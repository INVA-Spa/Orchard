﻿using Laser.Orchard.HID.Extensions;
using Laser.Orchard.HID.Services;
using Orchard.ContentManagement;
using Orchard.Core.Common.Models;
using Orchard.Localization;
using Orchard.Security;
using Orchard.Users.Models;
using Orchard.Workflows.Models;
using Orchard.Workflows.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Laser.Orchard.HID.Activities {
    public class RevokeCredentialsTask : Task {

        private readonly IHIDAPIService _HIDAPIService;
        private readonly IContentManager _contentManager;

        public RevokeCredentialsTask(
            IHIDAPIService hidAPIService,
            IContentManager contentManager) {

            _HIDAPIService = hidAPIService;
            _contentManager = contentManager;
        }

        public Localizer T { get; set; }

        public override string Form {
            get { return Constants.ActivityRevokeCredentialsFormName; }
        }

        public override string Name {
            get { return "RevokeCredentials"; }
        }

        public override LocalizedString Description {
            get { return T("Revoke credentials from the user."); }
        }

        public override LocalizedString Category {
            get { return T("Security"); }
        }

        public override IEnumerable<LocalizedString> GetPossibleOutcomes(WorkflowContext workflowContext, ActivityContext activityContext) {
            return new[] {
                T("OK"),
                T("NoIUser"),
                T("NoPartNumber"),
                T("UnknownError"),
                T("AuthorizationFailed")
            };
        }

        public override IEnumerable<LocalizedString> Execute(WorkflowContext workflowContext, ActivityContext activityContext) {

            // Get the user from the form
            var userString = activityContext.GetState<string>("IUser");
            IUser user = null;
            if (string.IsNullOrWhiteSpace(userString)) {
                if (workflowContext.Content.Has<CommonPart>()) {
                    int uId = (int)(((dynamic)workflowContext.Content.As<CommonPart>()).Creator.Value);
                    user = _contentManager.Get<UserPart>(uId);
                }
            } else {
                //use the string to get the user
                int userId = 0;
                if (int.TryParse(userString, out userId)) {
                    user = _contentManager.Get<UserPart>(userId);
                } else {
                    user = _contentManager.Query<UserPart, UserPartRecord>().Where(u => u.UserName == userString).List().FirstOrDefault();
                }
            }
            if (user == null) {
                yield return T("NoIUser");
            }

            // Get the part numbers from the form
            var pnString = activityContext.GetState<string>("PartNumbers");
            var partNumbers = string.IsNullOrWhiteSpace(pnString)
                ? new string[] { }
                : pnString.Split(new string[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);

            if ((partNumbers == null || partNumbers.Length == 0)
                && (_HIDAPIService.GetSiteSettings().PartNumbers.Length == 0)) {
                // No part number found configured, either in this activity's form or in the site settings
                yield return T("NoPartNumber");
            }

            // Revoke and handle response
            var hidUser = _HIDAPIService.RevokeCredentials(user, partNumbers);
            switch (hidUser.Error) {
                case UserErrors.NoError:
                    // In this case, we need to ensure that no CredentialContainer has errors
                    var badContainer = hidUser
                        .CredentialContainers
                        .FirstOrDefault(cc => cc.Error != CredentialErrors.NoError);
                    if (badContainer == null) { // no error on any container
                        yield return T("OK");
                    }
                    switch (badContainer.Error) {
                        case CredentialErrors.UnknownError:
                            yield return T("UnknownError");
                            break;
                        case CredentialErrors.AuthorizationFailed:
                            yield return T("AuthorizationFailed");
                            break;
                        default:
                            yield return T("UnknownError");
                            break;
                    }
                    break;
                case UserErrors.UnknownError:
                    yield return T("UnknownError");
                    break;
                case UserErrors.AuthorizationFailed:
                    yield return T("AuthorizationFailed");
                    break;
                default:
                    yield return T("UnknownError");
                    break;
            }

        }
    }
}