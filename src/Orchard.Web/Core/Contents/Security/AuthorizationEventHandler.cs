using System.Collections.Generic;
using System.Linq;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Aspects;
using Orchard.Core.Contents.Settings;
using Orchard.Localization;
using Orchard.Logging;
using Orchard.Security;
using Orchard.Security.Permissions;
using Orchard.UI.Notify;

namespace Orchard.Core.Contents.Security {
    public class AuthorizationEventHandler : IAuthorizationServiceEventHandler {
        private IEnumerable<IPermissionProvider> _permissions;
        private INotifier _notifier;

        public AuthorizationEventHandler(IEnumerable<IPermissionProvider> permissions, INotifier notifier) {
            _permissions = permissions;
            _notifier = notifier;
            T = NullLocalizer.Instance;
        }
        public ILogger Logger { get; set; }
        public Localizer T { get; set; }

        public void Checking(CheckAccessContext context) {
            var permissionList = _permissions.Invoke(x => x.GetPermissions(), Logger);
            foreach (var permission in permissionList) {
                var customPermission = permission.Where(w => w.Overrides != null && w.Overrides(context.Permission, context.Content));
                if (customPermission.Any()) {
                    if (customPermission.FirstOrDefault() != null) {
                        context.Permission = customPermission.First();
                        break;
                    }
                }
            }
        }
        public void Complete(CheckAccessContext context) { }

        public void Adjust(CheckAccessContext context) {

            if (!context.Granted &&
                context.Content.Is<ICommonPart>()) {

                if (OwnerVariationExists(context.Permission) &&
                    HasOwnership(context.User, context.Content)) {

                    context.Adjusted = true;
                    context.Permission = GetOwnerVariation(context.Permission);
                }

                var typeDefinition = context.Content.ContentItem.TypeDefinition;

                // replace permission if a content type specific version exists
                if (typeDefinition.Settings.GetModel<ContentTypeSettings>().Securable) {
                    var permission = GetContentTypeVariation(context.Permission);

                    if (permission != null) {
                        context.Adjusted = true;
                        context.Permission = DynamicPermissions.CreateDynamicPermission(permission, typeDefinition);
                    }
                }
            }
        }

        private static bool HasOwnership(IUser user, IContent content) {
            if (user == null || content == null)
                return false;

            var common = content.As<ICommonPart>();
            if (common == null || common.Owner == null)
                return false;

            return user.Id == common.Owner.Id;
        }

        private static bool OwnerVariationExists(Permission permission) {
            return GetOwnerVariation(permission) != null;
        }

        private static Permission GetOwnerVariation(Permission permission) {
            if (permission.Name == Permissions.PublishContent.Name)
                return Permissions.PublishOwnContent;
            if (permission.Name == Permissions.EditContent.Name)
                return Permissions.EditOwnContent;
            if (permission.Name == Permissions.DeleteContent.Name)
                return Permissions.DeleteOwnContent;
            if (permission.Name == Permissions.ViewContent.Name)
                return Permissions.ViewOwnContent;
            if (permission.Name == Permissions.PreviewContent.Name)
                return Permissions.PreviewOwnContent;

            return null;
        }

        private static Permission GetContentTypeVariation(Permission permission) {
            return DynamicPermissions.ConvertToDynamicPermission(permission);
        }
    }
}