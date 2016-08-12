﻿using Laser.Orchard.TaskScheduler.Models;
using Laser.Orchard.TaskScheduler.ViewModels;
using Orchard;
using Orchard.ContentManagement;
using Orchard.Core.Scheduling.Models;
using Orchard.Data;
using Orchard.Tasks.Scheduling;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Linq;
using System.Web;

namespace Laser.Orchard.TaskScheduler.Services {
    public class ScheduledTaskService : IScheduledTaskService {

        private readonly IRepository<LaserTaskSchedulerRecord> _repoLaserTaskScheduler;
        private readonly IOrchardServices _orchardServices;
        private readonly IScheduledTaskManager _taskManager;
        private readonly IRepository<ScheduledTaskRecord> _repoTasks;


        public ScheduledTaskService(IRepository<LaserTaskSchedulerRecord> repoLaserTaskScheduler,
            IOrchardServices orchardServices,
            IScheduledTaskManager taskManager,
            IRepository<ScheduledTaskRecord> repoTasks) {
            _repoLaserTaskScheduler = repoLaserTaskScheduler;
            _orchardServices = orchardServices;
            _taskManager = taskManager;
            _repoTasks = repoTasks;
        }
        /// <summary>
        /// Get all the scheulers from the db
        /// </summary>
        /// <returns>A list of task schedulers found in the db</returns>
        public List<ScheduledTaskPart> GetAllTasks() {
            List<ScheduledTaskPart> parts = _orchardServices.ContentManager.Query().ForPart<ScheduledTaskPart>().List().ToList();
            foreach (ScheduledTaskPart pa in parts.Where(p => p.RunningTaskId != 0)) {
                //check whether the task is still running. It might have been stopped by someone or something
                if (_repoTasks.Get(pa.RunningTaskId) == null) {
                    pa.RunningTaskId = 0;
                }
            }
            return parts;
        }

        /// <summary>
        /// Converts the data from the form into a list of view models for the task schedulers
        /// NOTE: here we are using the name of the properties and fields in the strings:
        /// if those are changed for any reason, the strings in this method should reflect that
        /// </summary>
        /// <param name="formData">The collection representing the data from the form</param>
        /// <returns>The list of view models</returns>
        public List<ScheduledTaskViewModel> GetTaskViewModelsFromForm(NameValueCollection formData) {
            var keys = formData.AllKeys.Where(k => k.IndexOf("allTasks[") == 0).ToArray();

            List<ScheduledTaskViewModel> vmsForTasks = new List<ScheduledTaskViewModel>();
            int nVms = keys.Length / 9; //this is the number of fields we get from the views, and should be the number of fields in the ScheduledTaskViewModel

            //note: here we are using the name of the properties and fields in the strings:
            //if those are changed for any reason, the strings in this method should reflect that
            for (int i = 0; i < nVms; i++) {
                //get the number to use as index for the keys. It may not correspond to i in the case where
                //some schedulers have been deleted
                string kk = keys[i * 9];
                int index = int.Parse(kk.Split(new string[] { "[", "]" }, StringSplitOptions.RemoveEmptyEntries)[1]);
                string thisObject = String.Format("allTasks[{0}].", index);
                DateTime? inputDate;
                try {
                    inputDate = String.IsNullOrWhiteSpace(formData[thisObject + "ScheduledStartUTC"]) ? (DateTime?)null :
                        Convert.ToDateTime(formData[thisObject + "ScheduledStartUTC"]);
                } catch (Exception) {
                    inputDate = null;
                }
                vmsForTasks.Add(new ScheduledTaskViewModel {
                    Id = int.Parse(formData[thisObject + "Id"]),
                    SignalName = formData[thisObject + "SignalName"],
                    ScheduledStartUTC = inputDate,
                    PeriodicityTime = int.Parse(formData[thisObject + "PeriodicityTime"]),
                    PeriodicityUnit = EnumExtension.ParseEnum(formData[thisObject + "PeriodicityUnit"]),
                    ContentItemId = int.Parse(formData[thisObject + "ContentItemId"]),
                    Running = int.Parse(formData[thisObject + "Running"]),
                    Delete = Convert.ToBoolean(formData[thisObject + "Delete"]),
                    Scheduling = Convert.ToBoolean(formData[thisObject + "Scheduling"])
                });
            }

            return vmsForTasks;
        }

        /// <summary>
        /// Updates the reords for the schedulers based on the changes from the UI
        /// </summary>
        /// <param name="vms">A list of view models that hold the updated information</param>
        public void UpdateRecords(List<ScheduledTaskViewModel> vms) {
            foreach (ScheduledTaskViewModel vm in vms) {
                //if Id != 0 the task was already in the db
                if (vm.Id != 0) {
                    //Should we try to delete?
                    if (vm.Delete) {
                        //if there is a corresponding task that is running, we should stop it first
                        if (vm.Running > 0) {
                            //stop the task with id == vm.Running
                            UnscheduleTask(vm);
                        }
                        //the task is definitely not running, so we may safely remove the scheduler
                        _orchardServices.ContentManager.Remove(_orchardServices.ContentManager.Get(vm.Id));
                        //(note that a handler is invoked to clean up the repositor)
                    } else {
                        //update the part
                        ScheduledTaskPart part = (ScheduledTaskPart)_orchardServices.ContentManager.Get<ScheduledTaskPart>(vm.Id);
                        vm.UpdatePart(part);
                    }
                } else {
                    //we have to create a new record
                    if (!vm.Delete) {
                        //we only create it if it was not also deleted already
                        ScheduledTaskPart part = (ScheduledTaskPart)_orchardServices.ContentManager.New<ScheduledTaskPart>("ScheduledTaskPart");
                        vm.UpdatePart(part);
                        _orchardServices.ContentManager.Create(part);
                    }
                }
            }
        }

        /// <summary>
        /// Schedule a new task based on the information in the view model
        /// </summary>
        /// <param name="vm">The view model we are basing the new task on</param>
        public void ScheduleTask(ScheduledTaskViewModel vm) {
            //get the part
            ScheduledTaskPart part = (ScheduledTaskPart)_orchardServices.ContentManager.Get<ScheduledTaskPart>(vm.Id);
            //define tasktype: BASE_SIGNALNAME_ID
            string taskTypeStr = Constants.TaskTypeBase + "_" + part.SignalName + "_" + part.Id;
            ContentItem ci = null;
            if (part.ContentItemId > 0) {
                ci = _orchardServices.ContentManager.Get(part.ContentItemId);
            }
            _taskManager.CreateTask(taskTypeStr, part.ScheduledStartUTC ?? DateTime.UtcNow, ci);
            part.RunningTaskId = _repoTasks.Get(str => str.TaskType.Equals(taskTypeStr)).Id;
            
        }
        /// <summary>
        /// Unschedule an existing task based on the view model
        /// </summary>
        /// <param name="vm">The view model corresponding to the task we want to unschedule</param>
        public void UnscheduleTask(ScheduledTaskViewModel vm) {
            //get the part
            ScheduledTaskPart part = (ScheduledTaskPart)_orchardServices.ContentManager.Get<ScheduledTaskPart>(vm.Id);
            int tId = part.RunningTaskId;
            if (tId > 0) {
                var str = _repoTasks.Get(tId);
                if (str != null) {
                    _repoTasks.Delete(str);
                } else {
                    //tId might have changed since the moment we got the information into the view models
                    //e.g. if the task is periodic, it will generate a new Id and update it.
                    //let's check here if there are tasks with the part id in the TaskType
                    //(see the ScheduleTask method for the format we are using)
                    var records = _repoTasks.Table.ToList().Where(rec =>
                        //rec.TaskType.Split(new string[]{"_"}, StringSplitOptions.RemoveEmptyEntries).Last().Equals(part.Id.ToString())
                        rec.TaskType.IndexOf(Constants.TaskTypeBase) == 0
                        ).ToList().Where(rec =>
                        rec.TaskType.Split(new string[] { "_" }, StringSplitOptions.RemoveEmptyEntries).Last().Equals(part.Id.ToString())
                        ).ToList();
                    foreach (var item in records) {
                        _repoTasks.Delete(item);
                    }
                }
            }
            
            part.RunningTaskId = 0;

        }

        /// <summary>
        /// computes the next DateTime for scheduling based off the information in the part
        /// </summary>
        /// <param name="part">The part containing the scheduling information</param>
        /// <returns>A <type>DateTime</type> object containing the moment when the task whoudl be scheduled next.</returns>
        public DateTime ComputeNextScheduledTime(ScheduledTaskPart part) {
            DateTime result = DateTime.UtcNow;
            switch (part.PeriodicityUnit) {
                case TimeUnits.Seconds:
                    result = result.AddSeconds(part.PeriodicityTime);
                    break;
                case TimeUnits.Minutes:
                    result = result.AddMinutes(part.PeriodicityTime);
                    break;
                case TimeUnits.Hours:
                    result = result.AddHours(part.PeriodicityTime);
                    break;
                case TimeUnits.Days:
                    result = result.AddDays(part.PeriodicityTime);
                    break;
                case TimeUnits.Weeks:
                    result = result.AddDays(7 * part.PeriodicityTime);
                    break;
                case TimeUnits.Months:
                    result = result.AddMonths(part.PeriodicityTime);
                    break;
                case TimeUnits.Years:
                    result = result.AddYears(part.PeriodicityTime);
                    break;
                default:
                    break;
            }
            return result;
        }
    }
}