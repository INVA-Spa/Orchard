﻿using Laser.Orchard.TaskScheduler.Models;
using Laser.Orchard.TaskScheduler.Services;
using Orchard;
using Orchard.ContentManagement;
using Orchard.Core.Scheduling.Models;
using Orchard.Data;
using Orchard.Logging;
using Orchard.Tasks.Scheduling;
using Orchard.Workflows.Activities;
using Orchard.Workflows.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Laser.Orchard.TaskScheduler.Handlers {
    public class ScheduledTaskTasksHandler : IScheduledTaskHandler {

        private readonly IOrchardServices _orchardServices;
        private readonly IScheduledTaskManager _taskManager;
        private readonly IScheduledTaskService _scheduledTaskService;
        private readonly IRepository<ScheduledTaskRecord> _repoTasks;
        private readonly IWorkflowManager _workflowManager;
        public ILogger Logger { get; set; }

        public ScheduledTaskTasksHandler(IOrchardServices orchardServices,
            IScheduledTaskManager taskManager,
            IScheduledTaskService scheduledTaskService,
            IRepository<ScheduledTaskRecord> repoTasks,
            IWorkflowManager workflowManager) {
            _orchardServices = orchardServices;
            _taskManager = taskManager;
            _scheduledTaskService = scheduledTaskService;
            _repoTasks = repoTasks;
            _workflowManager = workflowManager;
            Logger = NullLogger.Instance;
        }

        public void Process(ScheduledTaskContext context) {
            string taskTypeStr = context.Task.TaskType;
            if (taskTypeStr.IndexOf(Constants.TaskTypeBase) == 0) {
                try {
                    //For some reason, querying the db for the records related to the task returns null
                    //Hence, we placed the part id in the TaskType.
                    int pid = int.Parse(taskTypeStr.Split(new string[] { "_" }, StringSplitOptions.RemoveEmptyEntries).Last());
                    ScheduledTaskPart part = (ScheduledTaskPart)_orchardServices.ContentManager.Get<ScheduledTaskPart>(pid);
                    //Trigger the event
                    //get the signal name for from the part to track edits that may have been done.
                    _workflowManager.TriggerEvent(
                        SignalActivity.SignalEventName,
                        context.Task.ContentItem,
                        () => new Dictionary<string, object> {
                        { "Content", context.Task.ContentItem },
                        { SignalActivity.SignalEventName, part.SignalName }}
                        );
                    //if the part has periodicity and it was not unscheduled, we may reschedule the task
                    if (part.PeriodicityTime > 0 && part.RunningTaskId > 0) {
                        //define tasktype
                        string newTaskTypeStr = Constants.TaskTypeBase + "_" + part.SignalName + "_" + part.Id;
                        ContentItem ci = null;
                        if (part.ContentItemId > 0) {
                            ci = _orchardServices.ContentManager.Get(part.ContentItemId);
                        }
                        DateTime scheduleTime = _scheduledTaskService.ComputeNextScheduledTime(part);
                        _taskManager.CreateTask(newTaskTypeStr, scheduleTime, ci);
                        part.RunningTaskId = _repoTasks.Get(str => str.TaskType.Equals(newTaskTypeStr)).Id;
                    }
                    else {
                        part.RunningTaskId = 0;
                    }
                }
                catch (Exception ex) {
                    Logger.Error(ex, "ScheduledTaskTasksHandler -> Error on " + taskTypeStr + " id= " + context.Task.ContentItem + ex.Message);
                }
            }
        }
    }
}