﻿using Laser.Orchard.HID.Events;
using Laser.Orchard.HID.Extensions;
using Laser.Orchard.HID.Models;
using Orchard.ContentManagement;
using Orchard.Data;
using Orchard.Localization;
using Orchard.Logging;
using Orchard.Security;
using Orchard.Tasks.Scheduling;
using Orchard.Users.Models;
using System;
using System.Linq;

namespace Laser.Orchard.HID.Services {
    public class HIDCredentialsService : IHIDCredentialsService {

        private readonly IHIDSearchUserService _HIDSearchUserService;
        private readonly IContentManager _contentManager;
        private readonly IScheduledTaskManager _taskManager;
        private readonly IRepository<BulkCredentialsOperationsRecord> _credentialsOperationsRepository;
        private readonly IHIDEventHandler _HIDEventHandlers;

        public HIDCredentialsService(
            IHIDSearchUserService HIDSearchUserService,
            IContentManager contentManager,
            IScheduledTaskManager taskManager,
            IRepository<BulkCredentialsOperationsRecord> credentialsOperationsrepository,
            IHIDEventHandler HIDEventHandlers) {

            _HIDSearchUserService = HIDSearchUserService;
            _contentManager = contentManager;
            _taskManager = taskManager;
            _credentialsOperationsRepository = credentialsOperationsrepository;
            _HIDEventHandlers = HIDEventHandlers;

            Logger = NullLogger.Instance;
            T = NullLocalizer.Instance;
        }

        ILogger Logger { get; set; }
        public Localizer T { get; set; }

        public HIDUser IssueCredentials(HIDUser hidUser, string[] partNumbers) {
            var user = UserFromEmail(hidUser.Emails.FirstOrDefault());
            if (partNumbers.Length == 0) {
                hidUser = hidUser.IssueCredential(""); //this assigns the default part number for the customer
                if (hidUser.Error == UserErrors.NoError && hidUser.Error == UserErrors.PreconditionFailed) {
                    // trigger events on success
                    _HIDEventHandlers.HIDCredentialIssued(new HIDCredentialEventContext(hidUser, "") { User = user });
                }
            } else {
                foreach (var pn in partNumbers) {
                    hidUser = hidUser.IssueCredential(pn);
                    if (hidUser.Error != UserErrors.NoError && hidUser.Error != UserErrors.PreconditionFailed) {
                        break;  //break on error, but not on PreconditionFailed, because that may be caused by the credential having been
                                //assigned already, which is fine
                    }
                    // trigger events on success
                    _HIDEventHandlers.HIDCredentialIssued(new HIDCredentialEventContext(hidUser, "") { User = user });
                }
            }
            return hidUser;
        }

        public HIDUser IssueCredentials(IUser user, string[] partNumbers) {
            var searchResult = _HIDSearchUserService.SearchHIDUser(user.Email);
            if (searchResult.Error == SearchErrors.NoError) {
                return IssueCredentials(searchResult.User, partNumbers);
            } else {
                return new HIDUser();
            }
        }

        public HIDUser RevokeCredentials(HIDUser hidUser, string[] partNumbers) {
            var user = UserFromEmail(hidUser.Emails.FirstOrDefault());
            if (partNumbers.Length == 0) {
                hidUser = hidUser.RevokeCredential();
                if (hidUser.Error == UserErrors.NoError && hidUser.Error == UserErrors.PreconditionFailed) {
                    // trigger events on success
                    _HIDEventHandlers.HIDCredentialRevoked(new HIDCredentialEventContext(hidUser, "") { User = user });
                }
            } else {
                foreach (var pn in partNumbers) {
                    hidUser = hidUser.RevokeCredential(pn);
                    if (hidUser.Error != UserErrors.NoError && hidUser.Error != UserErrors.PreconditionFailed) {
                        break;  //break on error, but not on PreconditionFailed, because that may be caused by the credential being
                        //revoked right now
                    }
                    // trigger events on success
                    _HIDEventHandlers.HIDCredentialRevoked(new HIDCredentialEventContext(hidUser, "") { User = user });
                }
            }
            return hidUser;
        }

        public HIDUser RevokeCredentials(IUser user, string[] partNumbers) {
            var searchResult = _HIDSearchUserService.SearchHIDUser(user.Email);
            if (searchResult.Error == SearchErrors.NoError) {
                return RevokeCredentials(searchResult.User, partNumbers);
            } else {
                return new HIDUser();
            }
        }

        public void ProcessUserCredentialActions(BulkCredentialsOperationsContext context) {
            context.ConsolidateDictionary();
            foreach (var ua in context.UserActions) {
                var user = _contentManager.Get<UserPart>(ua.Key);
                if (user != null) {
                    // For each user, the lists should have been processed in such a way that
                    // 1. They contain no duplicate
                    // 2. There's no PartNumber that is in both lists
                    // 3. RevokeList and IssueList are not both empty
                    // Get the HIDUser only once, as we will need it for both issue and revoke
                    var searchResult = _HIDSearchUserService.SearchHIDUser(user.Email);
                    if (searchResult.Error == SearchErrors.NoError) {
                        var issueUser = ua.Value.IssueList.Any()
                            ? IssueCredentials(searchResult.User, ua.Value.IssueList.ToArray())
                            : null;
                        if (issueUser != null && (issueUser.Error != UserErrors.NoError && issueUser.Error != UserErrors.PreconditionFailed)) {
                            context.AddError(user, issueUser.Error);
                        }
                        var revokeUser = ua.Value.RevokeList.Any()
                            ? RevokeCredentials(searchResult.User, ua.Value.RevokeList.ToArray())
                            : null;
                        if (revokeUser != null && (revokeUser.Error != UserErrors.NoError && revokeUser.Error != UserErrors.PreconditionFailed)) {
                            context.AddError(user, revokeUser.Error);
                        }
                    } else {
                        context.AddError(user, searchResult.Error);
                    }
                }
            }
        }

        public void ScheduleCredentialActions(BulkCredentialsOperationsContext context) {
            if (context.UserActions.Any()) {
                // Create the record for the first user action in the db
                // We will use the Id of this first record as taskId, so after creating the first
                // we need to update its TaskId field (also in the db).
                var firstRecord = context.UserActions.First().Value.ToRecord(0);
                _credentialsOperationsRepository.Create(firstRecord);
                var taskId = firstRecord.Id;
                firstRecord.TaskId = taskId;
                _credentialsOperationsRepository.Update(firstRecord);

                // now create all other records
                foreach (var ua in context.UserActions.Where(act => act.Key != firstRecord.UserId)) {
                    var record = ua.Value.ToRecord(taskId);
                    _credentialsOperationsRepository.Create(record);
                }

                // Generate the name of the task as Constant_TaskId
                var taskTypeStr = Constants.HIDBulkCredentialsOperationsTaskName + "_" + taskId.ToString();
                // create and schedule the task
                _taskManager.CreateTask(taskTypeStr, DateTime.UtcNow, null);
            }
        }


        private IUser UserFromEmail(string email) {
            if (string.IsNullOrWhiteSpace(email)) {
                return null;
            }
            return _contentManager
                .Query<UserPart, UserPartRecord>()
                .Where(u => u.Email == email)
                .Slice(0, 1)
                .FirstOrDefault();
        }
    }
}