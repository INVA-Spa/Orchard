﻿using AutoMapper;
using Laser.Orchard.Events.Models;
using Laser.Orchard.Questionnaires.Models;
using Laser.Orchard.Questionnaires.Settings;
using Laser.Orchard.Questionnaires.ViewModels;
using Laser.Orchard.StartupConfig.Services;
using Laser.Orchard.TemplateManagement.Models;
using Laser.Orchard.TemplateManagement.Services;
using NHibernate.Transform;
using Orchard;
using Orchard.ContentManagement;
using Orchard.Core.Title.Models;
using Orchard.Data;
using Orchard.Localization;
using Orchard.Messaging.Services;
using Orchard.Security;
using Orchard.UI.Notify;
using Orchard.Workflows.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Laser.Orchard.Questionnaires.Services {

    public class QuestionnairesServices : IQuestionnairesServices {
        private readonly IRepository<QuestionRecord> _repositoryQuestions;
        private readonly IRepository<AnswerRecord> _repositoryAnswer;
        private readonly IRepository<UserAnswersRecord> _repositoryUserAnswer;
        private readonly IRepository<TitlePartRecord> _repositoryTitle;
        private readonly IOrchardServices _orchardServices;
        private readonly IWorkflowManager _workflowManager;
        private readonly INotifier _notifier;
        private readonly IControllerContextAccessor _controllerContextAccessor;
        private readonly ITemplateService _templateService;
        private readonly IMessageManager _messageManager;
        private readonly ISessionLocator _sessionLocator;

        public Localizer T { get; set; }

        public QuestionnairesServices(IOrchardServices orchardServices,
            IRepository<QuestionRecord> repositoryQuestions,
            IRepository<AnswerRecord> repositoryAnswer,
            IRepository<TitlePartRecord> repositoryTitle,
            IRepository<UserAnswersRecord> repositoryUserAnswer,
            IWorkflowManager workflowManager,
            INotifier notifier,
            IControllerContextAccessor controllerContextAccessor,
            ITemplateService templateService,
           IMessageManager messageManager,
            ISessionLocator sessionLocator) {
            _orchardServices = orchardServices;
            _repositoryAnswer = repositoryAnswer;
            _repositoryQuestions = repositoryQuestions;
            _repositoryTitle = repositoryTitle;
            _repositoryUserAnswer = repositoryUserAnswer;
            _workflowManager = workflowManager;
            _notifier = notifier;
            T = NullLocalizer.Instance;
            _controllerContextAccessor = controllerContextAccessor;
            _templateService = templateService;
            _messageManager = messageManager;
            _sessionLocator = sessionLocator;
        }

        private string getusername(int id) {
            if (id > 0) {
                try {
                    return ((dynamic)_orchardServices.ContentManager.Get(id)).UserPart.UserName;
                } catch (Exception) {
                    return "No User";
                }
            } else
                return "No User";
        }

        //public bool SendTemplatedEmailRanking(bool multipleRankings) {
        //    if (multipleRankings)
        //        return SendTemplatedEmailRankingMultiple();
        //    else
        //        return SendTemplatedEmailRanking();
        //}

        //private bool SendTemplatedEmailRankingMultiple() {
        //    var query = _orchardServices.ContentManager.Query();
        //    var list = query.ForPart<GamePart>().Where<GamePartRecord>(x => x.workflowfired == false).List();

        //    return false;
        //}
        
        public bool SendTemplatedEmailRanking() {
            var query = _orchardServices.ContentManager.Query();
            var list = query.ForPart<GamePart>().Where<GamePartRecord>(x=>x.workflowfired==false).List();
            var listranking = _orchardServices.ContentManager.Query().ForPart<RankingPart>().List();
            foreach (GamePart gp in list) {
                ContentItem Ci = gp.ContentItem;
                if (((dynamic)Ci).ActivityPart != null && ((dynamic)Ci).ActivityPart.DateTimeEnd != null) {
                    if (((dynamic)Ci).ActivityPart.DateTimeEnd < DateTime.Now) {
                        //    gp.workflowfired = false; //todo remove this line
                        //      if (gp.workflowfired == false) {
                        if (gp.Settings.GetModel<GamePartSettingVM>().SendEmail) {
                            var listordered = listranking.Where(z => z.As<RankingPart>().ContentIdentifier == Ci.Id).OrderByDescending(y => y.Point);
                            List<RankingTemplateVM> rkt = new List<RankingTemplateVM>();
                            foreach (RankingPart cirkt in listordered) {
                                RankingTemplateVM tmp = new RankingTemplateVM();
                                tmp.Point = cirkt.Point;
                                tmp.ContentIdentifier = cirkt.ContentIdentifier;
                                tmp.Device = cirkt.Device;
                                tmp.Identifier = cirkt.Identifier;
                                tmp.name = getusername(cirkt.User_Id);
                                tmp.UsernameGameCenter = cirkt.UsernameGameCenter;
                                tmp.AccessSecured = cirkt.AccessSecured;
                                tmp.RegistrationDate = cirkt.RegistrationDate;
                                rkt.Add(tmp);
                            }

                            if (SendEmail(Ci, rkt)) {
                                // logger
                            }
                        }
                        _workflowManager.TriggerEvent("GameRankingSubmitted", Ci, () => new Dictionary<string, object> { { "Content", Ci } });
                        gp.workflowfired = true;
                    }
                }
            }
            return true;
        }

        public bool SendTemplatedEmailRanking(Int32 gameID) {
            var query = _orchardServices.ContentManager.Query();
            var list = query.ForPart<GamePart>().Where<GamePartRecord>(x => x.Id == gameID).List();
            GamePart gp = list.FirstOrDefault();
            if (gp == null) //check if there is actually a game with the given ID (proper usage should make it so this is never true)
                return false;
            //get all rankings for the given game
            var listranking = _orchardServices.ContentManager.Query().ForPart<RankingPart>()
                .Where<RankingPartRecord>(x => x.ContentIdentifier == gameID)
                .OrderByDescending(y => y.Point)
                .List();
            //we do not do checks on whether this email was scheduled
            ContentItem Ci = gp.ContentItem;

            List<RankingTemplateVM> generalRank = QueryForRanking(gameId: gameID, page: 1, pageSize: 10);
            List<RankingTemplateVM> appleRank = QueryForRanking(gameId: gameID, device: TipoDispositivo.Apple.ToString(), page: 1, pageSize: 10);
            List<RankingTemplateVM> androidRank = QueryForRanking(gameId: gameID, device: TipoDispositivo.Android.ToString(), page: 1, pageSize: 10);
            List<RankingTemplateVM> windowsRank = QueryForRanking(gameId: gameID, device: TipoDispositivo.WindowsMobile.ToString(), page: 1, pageSize: 10);

            return SendEmail(Ci, generalRank, appleRank, androidRank, windowsRank);
        }

        private bool SendEmail(ContentItem Ci, List<RankingTemplateVM> rkt) {
            string emailRecipe = Ci.As<GamePart>().Settings.GetModel<GamePartSettingVM>().EmailRecipe;
            if (emailRecipe != "") {
                var editModel = new Dictionary<string, object>();
                editModel.Add("Content", Ci);
                editModel.Add("ListRanking", rkt);
                ParseTemplateContext ptc = new ParseTemplateContext();
                ptc.Model = editModel;
                int templateid = Ci.As<GamePart>().Settings.GetModel<GamePartSettingVM>().Template;
                TemplatePart TemplateToUse = _orchardServices.ContentManager.Get(templateid).As<TemplatePart>();
                string testohtml;
                if (TemplateToUse != null) {
                    testohtml = _templateService.ParseTemplate(TemplateToUse, ptc);
                    var datiCI = Ci.Record;
                    var data = new Dictionary<string, string>();
                    data.Add("Subject", "Game Ranking");
                    data.Add("Body", testohtml);
                    _messageManager.Send(new string[] { emailRecipe }, "ModuleRankingEmail", "email", data);
                    return true;
                } else { // Nessun template selezionato non mando una mail e ritorno false, mail non inviata
                    return false;
                }
            } else
                return false;
        }

        //method to send a single email with separate rankings for the different platforms
        private bool SendEmail(ContentItem Ci, List<RankingTemplateVM> generalRank, List<RankingTemplateVM> appleRank,
            List<RankingTemplateVM> androidRank, List<RankingTemplateVM> windowsRank) {
            string emailRecipe = Ci.As<GamePart>().Settings.GetModel<GamePartSettingVM>().EmailRecipe;
            if (emailRecipe != "") {
                var editModel = new Dictionary<string, object>();
                editModel.Add("Content", Ci);
                editModel.Add("GeneralRanking", generalRank);
                editModel.Add("AppleRanking", appleRank);
                editModel.Add("AndroidRanking", androidRank);
                editModel.Add("WindowsRanking", windowsRank);
                ParseTemplateContext ptc = new ParseTemplateContext();
                ptc.Model = editModel;
                int templateid = Ci.As<GamePart>().Settings.GetModel<GamePartSettingVM>().Template;
                TemplatePart TemplateToUse = _orchardServices.ContentManager.Get(templateid).As<TemplatePart>();
                string testohtml;
                if (TemplateToUse != null) {
                    testohtml = _templateService.ParseTemplate(TemplateToUse, ptc);
                    var datiCI = Ci.Record;
                    var data = new Dictionary<string, string>();
                    data.Add("Subject", "Game Ranking");
                    data.Add("Body", testohtml);
                    _messageManager.Send(new string[] { emailRecipe }, "ModuleRankingEmail", "email", data);
                    return true;
                } else { // Nessun template selezionato non mando una mail e ritorno false, mail non inviata
                    return false;
                }
            } else
                return false;
        }

        
        /// <summary>
        /// Method used to query the db for a specific ranking (by game, device) and a specific range of results (paging)
        /// </summary>
        /// <param name="gameId">The Id of the game for which we want the ranking table.</param>
        /// <param name="device">A string representing the tipe of device for which we want the ranking. This is obtained as a 
        /// TipoDispositivo.value.ToString(). Any other string causes this method to default to returning the general ranking.</param>
        /// <param name="page">The page of the results in the ranking. This is 1-based (the top-most results are on page 1).</param>
        /// <param name="pageSize">The size of a page of results, corresponding to the maximum number of socres to return.</param>
        /// <param name="Ascending">A flag to determine the sorting order for the scores: <value>true</value> order by ascending score; 
        /// <value>false</value> order by descending score.</param>
        /// <returns>A <type>List &lt; RankingTemplateVM &gt;</type> containingn the objects representing the desired ranking.</returns>
        /// <example> QueryForRanking (3, TipoDispositivo.Apple.ToString()) would return the top10 scores for game number 3
        /// scored by iOs users, sorted from the highest score down.</example>
        public List<RankingTemplateVM> QueryForRanking(
            Int32 gameId, string device = "General", int page = 1, int pageSize = 10, bool Ascending = false) {
            
            List<RankingTemplateVM> lRank = new List<RankingTemplateVM>();
            string queryDeviceCondition = "";
            if (device == TipoDispositivo.Apple.ToString())
                queryDeviceCondition += "AND Device = '" + TipoDispositivo.Apple + "' ";
            else if (device == TipoDispositivo.Android.ToString())
                queryDeviceCondition += "AND Device = '" + TipoDispositivo.Android + "' ";
            else if (device == TipoDispositivo.WindowsMobile.ToString())
                queryDeviceCondition += "AND Device = '" + TipoDispositivo.WindowsMobile + "' ";
            string subQueryPoints = "SELECT MAX(Point) "
                    + "FROM [Orchard_FestivalTV].[dbo].[Laser_Orchard_Questionnaires_RankingPartRecord] " //Laser.Orchard.Questionnaires.Models.RankingPartRecord "
                    + "WHERE ContentIdentifier=" + gameId + " " + queryDeviceCondition
                    + "AND Identifier = rpr.Identifier ";
            string subQueryDate = "SELECT MIN(RegistrationDate) "
                    + "FROM [Orchard_FestivalTV].[dbo].[Laser_Orchard_Questionnaires_RankingPartRecord] " //Laser.Orchard.Questionnaires.Models.RankingPartRecord "
                    + "WHERE ContentIdentifier=" + gameId + " " + queryDeviceCondition
                    + "GROUP BY Identifier, Point ";
            string queryTable = "SELECT rpr.[Point], rpr.[Identifier], rpr.[UsernameGameCenter], rpr.[Device], rpr.[ContentIdentifier], rpr.[User_Id], rpr.[AccessSecured], rpr.[RegistrationDate] "
                    + "FROM [Orchard_FestivalTV].[dbo].[Laser_Orchard_Questionnaires_RankingPartRecord] as rpr " //Laser.Orchard.Questionnaires.Models.RankingPartRecord as rpr "
                    + "WHERE ContentIdentifier=" + gameId + " " + queryDeviceCondition
                    + "AND Point = ( " + subQueryPoints + " ) "
                    + "AND RegistrationDate IN ( " + subQueryDate + " ) ";
            if (Ascending)
                queryTable += "ORDER BY Point ";
            else
                queryTable += "ORDER BY Point DESC ";
            //paging
            queryTable += "OFFSET " + (pageSize * (page - 1)).ToString() + " ROWS "
                    + "FETCH NEXT " + pageSize.ToString() + " ROWS ONLY ";
            var tableQuery = _sessionLocator.For(typeof(RankingPartRecord)).CreateSQLQuery(queryTable); //since we create a SQL query, we use [table names] instead of Domain.Names
            //var ranking = tableQuery.List();
            var ranking = tableQuery.SetResultTransformer(Transformers.AliasToBean<RankingPartRecordIntermediate>()).List();

            //get the query results into the list
            foreach (RankingPartRecordIntermediate obj in ranking) {
                lRank.Add(ConvertFromDBData(obj));
            }

            return lRank;
        }

        /// <summary>
        /// Method filling up a <type>RanKingTemplateVM</type> object from a <type>RankingPartRecordIntermediate</type> object. The <type>RanKingTemplateVM</type>
        /// object has a filed of type <type>TipoDispositivo</type>. The corresponding record is read as a <type>string</type> from the DB. For this reason we use
        /// an intermediate class to store what we read from the db, and then convert it into the proper type.
        /// </summary>
        /// <param name="rpri">A <type>RankingPartRecordIntermediate</type> object, corresponding to a record as read from the DB.</param>
        /// <returns>A <type>RanKingTemplateVM</type> object, corresponding to the record we read from the DB.</returns>
        private RankingTemplateVM ConvertFromDBData(RankingPartRecordIntermediate rpri) {
            RankingTemplateVM ret = new RankingTemplateVM();
            ret.Point = rpri.Point;
            ret.Identifier = rpri.Identifier;
            ret.UsernameGameCenter = rpri.UsernameGameCenter;
            if (rpri.Device == TipoDispositivo.Android.ToString())
                ret.Device = TipoDispositivo.Android;
            else if (rpri.Device == TipoDispositivo.Apple.ToString())
                ret.Device = TipoDispositivo.Apple;
            else if (rpri.Device == TipoDispositivo.WindowsMobile.ToString())
                ret.Device = TipoDispositivo.WindowsMobile;
            ret.ContentIdentifier = rpri.ContentIdentifier;
            ret.name = getusername(rpri.User_Id);
            ret.AccessSecured = rpri.AccessSecured;
            ret.RegistrationDate = rpri.RegistrationDate;
            return ret;
        }


        public void Save(QuestionnaireWithResultsViewModel editModel, IUser currentUser, string SessionID) {
            var questionnaireModuleSettings = _orchardServices.WorkContext.CurrentSite.As<QuestionnaireModuleSettingsPart>();
            bool exit = false;
            if (currentUser != null && questionnaireModuleSettings.Disposable) {
                if (_repositoryUserAnswer.Fetch(x => x.User_Id == currentUser.Id && x.QuestionnairePartRecord_Id == editModel.Id).Count() > 0) {
                    _notifier.Add(NotifyType.Information, T("Operation Fail: Questionnaire already filled"));
                    // throw new Exception();
                    exit = true;
                }
            }
            if (!exit) {
                foreach (var q in editModel.QuestionsWithResults) {
                    if (q.QuestionType == QuestionType.OpenAnswer) {
                        if (!String.IsNullOrWhiteSpace(q.OpenAnswerAnswerText)) {
                            var userAnswer = new UserAnswersRecord();
                            userAnswer.AnswerText = q.OpenAnswerAnswerText;
                            userAnswer.QuestionText = q.Question;
                            userAnswer.QuestionRecord_Id = q.Id;
                            userAnswer.User_Id = currentUser.Id;
                            userAnswer.QuestionnairePartRecord_Id = editModel.Id;
                            userAnswer.SessionID = SessionID;
                            CreateUserAnswers(userAnswer);
                        }
                    } else if (q.QuestionType == QuestionType.SingleChoice) {
                        if (q.SingleChoiceAnswer > 0) {
                            var userAnswer = new UserAnswersRecord();
                            userAnswer.AnswerRecord_Id = q.SingleChoiceAnswer;
                            userAnswer.AnswerText = GetAnswer(q.SingleChoiceAnswer).Answer;
                            userAnswer.QuestionRecord_Id = q.Id;
                            userAnswer.User_Id = currentUser.Id;
                            userAnswer.QuestionText = q.Question;
                            userAnswer.QuestionnairePartRecord_Id = editModel.Id;
                            userAnswer.SessionID = SessionID;
                            CreateUserAnswers(userAnswer);
                        }
                    } else if (q.QuestionType == QuestionType.MultiChoice) {
                        var answerList = q.AnswersWithResult.Where(w => w.Answered);
                        foreach (var a in answerList) {
                            var userAnswer = new UserAnswersRecord();
                            userAnswer.AnswerRecord_Id = a.Id;
                            userAnswer.AnswerText = GetAnswer(a.Id).Answer;
                            userAnswer.QuestionRecord_Id = q.Id;
                            userAnswer.User_Id = currentUser.Id;
                            userAnswer.QuestionText = q.Question;
                            userAnswer.QuestionnairePartRecord_Id = editModel.Id;
                            userAnswer.SessionID = SessionID;
                            CreateUserAnswers(userAnswer);
                        }
                    }
                }

                var content = _orchardServices.ContentManager.Get(editModel.Id);
                _workflowManager.TriggerEvent("QuestionnaireSubmitted", content, () => new Dictionary<string, object> { { "Content", content } });
            }
        }

        public void UpdateForContentItem(ContentItem item, QuestionnaireEditModel partEditModel) {
            try {
                Mapper.CreateMap<QuestionnaireEditModel, QuestionnairePart>()
                    .ForMember(dest => dest.Questions, opt => opt.Ignore());
                Mapper.CreateMap<AnswerEditModel, AnswerRecord>();
                Mapper.CreateMap<QuestionEditModel, QuestionRecord>()
                    .ForMember(dest => dest.Answers, opt => opt.Ignore());
                var partRecord = item.As<QuestionnairePart>().Record;
                var part = item.As<QuestionnairePart>();
                var PartID = partRecord.Id;
                Mapper.Map(partEditModel, part);
                var mappingA = new Dictionary<string, string>();

                // Update and Delete
                foreach (var quest in partEditModel.Questions.Where(w => w.Id > 0)) {
                    QuestionRecord questionRecord = _repositoryQuestions.Get(quest.Id);
                    Mapper.Map(quest, questionRecord);
                    var recordQuestionID = questionRecord.Id;
                    questionRecord.QuestionnairePartRecord_Id = PartID;
                    if (quest.Delete) {
                        try {
                            foreach (var answer in questionRecord.Answers) {
                                _repositoryAnswer.Delete(_repositoryAnswer.Get(answer.Id));
                            }
                            _repositoryQuestions.Delete(_repositoryQuestions.Get(quest.Id));
                        } catch (Exception) {
                            throw new Exception("quest.Delete");
                        }
                    } else {
                        try {
                            foreach (var answer in quest.Answers.Where(w => w.Id != 0)) { ///Update and delete Answer
                                AnswerRecord answerRecord = new AnswerRecord();
                                Mapper.Map(answer, answerRecord);
                                if (answer.Delete) {
                                    _repositoryAnswer.Delete(_repositoryAnswer.Get(answer.Id));
                                } else if (answer.Id > 0) {
                                    _repositoryAnswer.Update(answerRecord);
                                }
                            }
                            _repositoryQuestions.Update(questionRecord);
                        } catch (Exception) {
                            throw new Exception("quest.Update");
                        }
                        try {
                            foreach (var answer in quest.Answers.Where(w => w.Id == 0)) { ///Insert Answer
                                AnswerRecord answerRecord = new AnswerRecord();
                                Mapper.Map(answer, answerRecord);
                                if (answer.Id == 0 && !answer.Delete) {
                                    answerRecord.QuestionRecord_Id = recordQuestionID;
                                    _repositoryAnswer.Create(answerRecord);
                                }
                            }
                        } catch (Exception) {
                            throw new Exception("answer.Insert");
                        }
                    }
                }
                // Create
                foreach (var quest in partEditModel.Questions.Where(w => w.Id == 0 && w.Delete == false)) {
                    QuestionRecord questionRecord = new QuestionRecord {
                        Position = quest.Position,
                        Published = quest.Published,
                        Question = quest.Question,
                        QuestionType = quest.QuestionType,
                        AnswerType = quest.AnswerType,
                        Section = quest.Section,
                        ConditionType = quest.ConditionType,
                        AllFiles = quest.AllFiles,
                        Condition = quest.Condition,
                        QuestionnairePartRecord_Id = PartID,
                    };
                    try {
                        _repositoryQuestions.Create(questionRecord);
                        var createdQuestionId = questionRecord.Id;
                        _repositoryQuestions.Flush();
                        foreach (var answer in quest.Answers) {
                            AnswerRecord answerRecord = new AnswerRecord();
                            Mapper.Map(answer, answerRecord);
                            if (answer.Id == 0) {
                                answerRecord.QuestionRecord_Id = createdQuestionId;
                                _repositoryAnswer.Create(answerRecord);
                                if (answer.OriginalId > 0) {
                                    mappingA[answer.OriginalId.ToString()] = answerRecord.Id.ToString();
                                }
                                _repositoryAnswer.Flush();
                            }
                        }
                    } catch (Exception) {
                        throw new Exception("quest.Create");
                    }

                    try {
                        // correggo gli id delle condizioni se necessario
                        if (mappingA.Count() > 0 && questionRecord.Condition != null) {
                            Regex re = new Regex(@"[0-9]+", RegexOptions.Compiled);
                            questionRecord.Condition = re.Replace(questionRecord.Condition, match => mappingA[match.Value] != null ? mappingA[match.Value].ToString() : match.Value);
                            _repositoryQuestions.Update(questionRecord);
                            re = null;
                        }
                    } catch (Exception) {
                        throw new Exception("quest.CorrezzioneCondizioni");
                    }
                }
            } catch (Exception) {
                throw new Exception("quest.UpdateTotale");
            }
        }

        public QuestionnaireEditModel BuildEditModelForQuestionnairePart(QuestionnairePart part) {
            Mapper.CreateMap<AnswerRecord, AnswerEditModel>();
            Mapper.CreateMap<QuestionRecord, QuestionEditModel>()
                .ForMember(dest => dest.Answers, opt => opt.MapFrom(src => src.Answers.OrderBy(o => o.Position)));
            Mapper.CreateMap<QuestionnairePart, QuestionnaireEditModel>()
                .ForMember(dest => dest.Questions, opt => opt.MapFrom(src => src.Questions));
            var editModel = Mapper.Map<QuestionnaireEditModel>(part);
            return (editModel);
        }

        public QuestionnaireViewModel BuildViewModelForQuestionnairePart(QuestionnairePart part) {
            Mapper.CreateMap<AnswerRecord, AnswerViewModel>();
            Mapper.CreateMap<QuestionRecord, QuestionViewModel>()
                .ForMember(dest => dest.Answers, opt => opt.MapFrom(src => src.Answers.OrderBy(o => o.Position)));
            Mapper.CreateMap<QuestionnairePart, QuestionnaireViewModel>()
                .ForMember(dest => dest.Questions, opt => opt.MapFrom(src => src.Questions.OrderBy(o => o.Position)));
            var viewModel = Mapper.Map<QuestionnaireViewModel>(part);
            return (viewModel);
        }

        #region [ FrontEnd Model ]

        public QuestionnaireWithResultsViewModel BuildViewModelWithResultsForQuestionnairePart(QuestionnairePart part) {
            Mapper.CreateMap<AnswerRecord, AnswerWithResultViewModel>();
            if (part.Settings.GetModel<QuestionnairesPartSettingVM>().QuestionsSortedRandomlyNumber > 0)
                Mapper.CreateMap<QuestionRecord, QuestionWithResultsViewModel>()
               .ForMember(dest => dest.AnswersWithResult, opt => opt.MapFrom(src => src.Answers.Where(w => w.Published)));
            else
                Mapper.CreateMap<QuestionRecord, QuestionWithResultsViewModel>()
                .ForMember(dest => dest.AnswersWithResult, opt => opt.MapFrom(src => src.Answers.Where(w => w.Published).OrderBy(o => o.Position)));
            if (part.Settings.GetModel<QuestionnairesPartSettingVM>().RandomResponse)
                Mapper.CreateMap<QuestionnairePart, QuestionnaireWithResultsViewModel>()
               .ForMember(dest => dest.QuestionsWithResults, opt => opt.MapFrom(src => src.Questions.Where(w => w.Published)));
            else
                Mapper.CreateMap<QuestionnairePart, QuestionnaireWithResultsViewModel>()
                .ForMember(dest => dest.QuestionsWithResults, opt => opt.MapFrom(src => src.Questions.Where(w => w.Published).OrderBy(o => o.Position)));
            var viewModel = Mapper.Map<QuestionnaireWithResultsViewModel>(part);
            return (viewModel);
        }

        #endregion [ FrontEnd Model ]

        public void CreateUserAnswers(UserAnswersRecord userAnswerRecord) {
            _repositoryUserAnswer.Create(userAnswerRecord);
        }

        public AnswerRecord GetAnswer(int id) {
            return (_repositoryAnswer.Get(id));
        }

        public List<QuestionnaireStatsViewModel> GetStats(int questionnaireId) {
            var questionnaireData = _orchardServices.ContentManager.Query<QuestionnairePart, QuestionnairePartRecord>(VersionOptions.Published)
                                                       .Where(q => q.Id == questionnaireId)
                                                       .List().FirstOrDefault();

            var questionnaireStats = _repositoryUserAnswer.Table.Join(_repositoryQuestions.Table,
                        l => l.QuestionRecord_Id, r => r.Id, (l, r) => new { UserAnswers = l, Questions = r })
                        .Where(w => w.Questions.QuestionnairePartRecord_Id == questionnaireId)
                        .ToList();

            if (questionnaireStats.Count == 0) {
                QuestionnaireStatsViewModel empty = new QuestionnaireStatsViewModel();
                empty.QuestionnairePart_Id = questionnaireData.Id;
                empty.QuestionnaireTitle = questionnaireData.As<TitlePart>().Title;
                empty.Answers = null;

                return new List<QuestionnaireStatsViewModel>() { empty };
            }
            else {
                var aggregatedStats = questionnaireStats.Select(s => new QuestionnaireStatsViewModel {
                                                                    QuestionnairePart_Id = s.Questions.QuestionnairePartRecord_Id,
                                                                    QuestionnaireTitle = questionnaireData.As<TitlePart>().Title,
                                                                    QuestionId = s.Questions.Id,
                                                                    Question = s.Questions.Question,
                                                                    QuestionType = s.Questions.QuestionType,
                                                                    Answers = new List<AnswerStatsViewModel>()
                                                                })
                                                         .GroupBy(g => g.QuestionId)
                                                         .Select(s => s.First()).ToList();

                for (int i = 0; i < aggregatedStats.Count(); i++) {
                    var question = aggregatedStats.ElementAt(i);
                    var answers = questionnaireStats.Where(w => w.Questions.Id == question.QuestionId)
                                                    .GroupBy(g => g.UserAnswers.AnswerText, StringComparer.InvariantCultureIgnoreCase)
                                                    .Select(s => new AnswerStatsViewModel {
                                                        Answer = s.Key,
                                                        Count = s.Count()
                                                    });

                    question.Answers.AddRange(answers.OrderBy(o => o.Answer));
                }

                return aggregatedStats.OrderBy(o => o.Question).ToList();
            }
        }

        public List<QuestStatViewModel> GetStats(QuestionType type) {
            var listaQuest = _orchardServices.ContentManager.Query<QuestionnairePart, QuestionnairePartRecord>(VersionOptions.Published)
                .List();
            var fullStat = new List<QuestStatViewModel>();
            foreach (var quest in listaQuest) {
                var title = quest.As<TitlePart>();
                var stats = _repositoryUserAnswer.Table.Join(_repositoryQuestions.Table,
                        l => l.QuestionRecord_Id, r => r.Id, (l, r) => new { UserAnswers = l, Questions = r })
                        .Where(w => w.Questions.QuestionType == type && w.Questions.QuestionnairePartRecord_Id == quest.Id)
                        .GroupBy(g => new { g.Questions.QuestionnairePartRecord_Id, g.UserAnswers.QuestionText, g.UserAnswers.AnswerText })
                        .Select(s => new QuestStatViewModel {
                            Question = s.Key.QuestionText,
                            Answer = s.Key.AnswerText,
                            QuestionnairePart_Id = s.Key.QuestionnairePartRecord_Id,
                            QuestionnaireTitle = title.Title,
                            Count = s.Count(),
                        });
                fullStat.AddRange(stats);
            }
            return fullStat.OrderBy(o => o.QuestionnaireTitle).ThenBy(o => o.Question).ThenBy(o => o.Answer).ToList();
        }
    }
}