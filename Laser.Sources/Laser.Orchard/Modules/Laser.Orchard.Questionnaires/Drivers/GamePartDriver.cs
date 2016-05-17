﻿using AutoMapper;
using Laser.Orchard.Questionnaires.Models;
using Laser.Orchard.Questionnaires.ViewModels;
using Laser.Orchard.StartupConfig.Localization;
using Orchard;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Drivers;
using Orchard.Localization;
using Orchard.ContentManagement.Handlers;
using Orchard.Tasks.Scheduling;
using System;
using System.Globalization;
using Orchard.Localization.Services;

namespace Laser.Orchard.Questionnaires.Drivers {

    public class GamePartDriver : ContentPartDriver<GamePart> {
        private readonly IOrchardServices _orchardServices;
        private readonly IDateLocalization _dateLocalization;
        private readonly IScheduledTaskManager _taskManager;
        private readonly IDateServices _dateServices;

        public Localizer T { get; set; }

        protected override string Prefix {
            get { return "Laser.Mobile.Questionnaires.Game"; }
        }

        public GamePartDriver(IOrchardServices orchardServices, IDateLocalization dateLocalization,
            IScheduledTaskManager taskManager, IDateServices dateServices) {
            _orchardServices = orchardServices;
            _dateLocalization = dateLocalization;
            _taskManager = taskManager;
            _dateServices = dateServices;
        }

        protected override DriverResult Editor(GamePart part, dynamic shapeHelper) {
            var viewModel = new GamePartVM();

            DateTime? tmpGameDate = _dateLocalization.ReadDateLocalized(part.GameDate);
            Mapper.CreateMap<GamePart, GamePartVM>()
                .ForMember(dest => dest.GameDate, opt => opt.Ignore());
            Mapper.Map(part, viewModel);
            viewModel.GameDate = _dateLocalization.WriteDateLocalized(tmpGameDate);
            return ContentShape("Parts_GamePart_Edit", () => shapeHelper.EditorTemplate(TemplateName: "Parts/GamePart_Edit", Model: viewModel, Prefix: Prefix));
        }

        protected override DriverResult Editor(GamePart part, IUpdateModel updater, dynamic shapeHelper) {
            var viewModel = new GamePartVM();
            if (updater.TryUpdateModel(viewModel, Prefix, null, null)) {
                Mapper.CreateMap<GamePartVM, GamePart>()
                    .ForMember(dest => dest.GameDate, opt => opt.Ignore());
                Mapper.Map(viewModel, part);
                if (!String.IsNullOrWhiteSpace(viewModel.GameDate)) {
                    part.GameDate = _dateLocalization.StringToDatetime(viewModel.GameDate, "") ?? DateTime.Now;
                }
            }
            //Check the button pressed: either Publish or Save
            if (_orchardServices.WorkContext.HttpContext.Request.Form["submit.Publish"] == "submit.Publish") {
                //Schedule a task to send an email at the end of the game. NOTE: the task should be scheduled only if the game is being published.
                ScheduleEmailTask(part);
            } else if (_orchardServices.WorkContext.HttpContext.Request.Form["submit.Save"] == "submit.Save") {
                //if the game has already been published, we need to reschedule its task
                if (part.IsPublished()) {
                    ScheduleEmailTask(part);
                }
            }
            return Editor(part, shapeHelper);
        }

        /// <summary>
        /// Create a task to schedule sending a summary email with the game results after the game has ended. the update of the task, in case the game has been modified,
        /// is done by deleting the existing task and creating a new one.
        /// </summary>
        /// <param name="part">The <type>GamePart</type> object containing the information about the game.</param>
        private void ScheduleEmailTask(GamePart part) {
            //Schedule a task to send an email at the end of the game.
            DateTime timeGameEnd = ((dynamic)part.ContentItem).ActivityPart.DateTimeEnd;
            //do we need to check whether timeGameEnd > DateTime.Now? NOTE: to make this check we should first convert timeGameEnd to UTC (see later)
            //as the code is now, if DateTime.Now > timeGameEnd, the email gets sent immediately
            Int32 thisGameID = part.Record.Id;
            //Check whether we already have a task for this game
            string taskTypeStr = Laser.Orchard.Questionnaires.Handlers.ScheduledTaskHandler.TaskType + " " + thisGameID.ToString();
            var tasks = _taskManager.GetTasks(taskTypeStr);
            foreach (var ta in tasks) {
                //if we are here, it means the task ta exists with the same game id as the current game
                //hence we should update the task. We fall in this condition when we are updating the information for a game.
                _taskManager.DeleteTasks(ta.ContentItem); //maybe
            }
            DateTime taskDate = timeGameEnd.AddMinutes(5);
            //Local time to UTC conversion
            //taskDate = (DateTime)( _dateServices.ConvertFromLocal(taskDate.ToLocalTime()));
            taskDate = (DateTime)(_dateServices.ConvertFromLocalString(_dateLocalization.WriteDateLocalized(taskDate), _dateLocalization.WriteTimeLocalized(taskDate)));
            //taskDate = taskDate.Subtract(new TimeSpan ( 2, 0, 0 )); //subtract two hours
            taskDate = taskDate.ToUniversalTime(); //this problay does nothing
            _taskManager.CreateTask(taskTypeStr, taskDate, null);
        }

        protected override void Importing(GamePart part, ImportContentContext context) {
            var root = context.Data.Element(part.PartDefinition.Name);
            part.AbstractText = root.Attribute("AbstractText").Value;
            part.AnswerPoint = Decimal.Parse(root.Attribute("AnswerPoint").Value, CultureInfo.InvariantCulture);
            part.AnswerTime = Decimal.Parse(root.Attribute("AnswerTime").Value, CultureInfo.InvariantCulture);
            part.GameDate = DateTime.Parse(root.Attribute("GameDate").Value, CultureInfo.InvariantCulture);
            part.GameType = ((GameType)Enum.Parse(typeof(GameType), root.Attribute("GameType").Value));
            part.MyOrder = Int32.Parse(root.Attribute("MyOrder").Value);
            part.QuestionsSortedRandomlyNumber = Int32.Parse(root.Attribute("QuestionsSortedRandomlyNumber").Value);
            part.RandomResponse = Boolean.Parse(root.Attribute("RandomResponse").Value);
            part.RankingAndroidIdentifier = root.Attribute("RankingAndroidIdentifier").Value;
            part.RankingIOSIdentifier = root.Attribute("RankingIOSIdentifier").Value;
        }

        protected override void Exporting(GamePart part, ExportContentContext context) {
            var root = context.Element(part.PartDefinition.Name);
            root.SetAttributeValue("AbstractText", part.AbstractText);
            root.SetAttributeValue("AnswerPoint", part.AnswerPoint.ToString(CultureInfo.InvariantCulture));
            root.SetAttributeValue("AnswerTime", part.AnswerTime.ToString(CultureInfo.InvariantCulture));
            root.SetAttributeValue("GameDate", part.GameDate.ToString(CultureInfo.InvariantCulture));
            root.SetAttributeValue("GameType", part.GameType);
            root.SetAttributeValue("MyOrder", part.MyOrder);
            root.SetAttributeValue("QuestionsSortedRandomlyNumber", part.QuestionsSortedRandomlyNumber);
            root.SetAttributeValue("RandomResponse", part.RandomResponse);
            root.SetAttributeValue("RankingAndroidIdentifier", part.RankingAndroidIdentifier);
            root.SetAttributeValue("RankingIOSIdentifier",      part.RankingIOSIdentifier);
        }
    }
}