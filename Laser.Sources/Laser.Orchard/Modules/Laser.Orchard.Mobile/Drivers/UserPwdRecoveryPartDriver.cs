﻿using AutoMapper;
using Laser.Orchard.Mobile.Models;
using Laser.Orchard.Mobile.ViewModels;
using Orchard;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Drivers;
using Orchard.ContentManagement.Handlers;
using Orchard.Environment.Extensions;
using Orchard.Localization;


namespace Laser.Orchard.Mobile.Drivers {

    [OrchardFeature("Laser.Orchard.Sms")]
    public class UserPwdRecoveryPartDriver : ContentPartDriver<UserPwdRecoveryPart> {

        private readonly IOrchardServices _orchardServices;
        public Localizer T { get; set; }

        protected override string Prefix {
            get { return "Laser.Mobile.UserPwdRecovery"; }
        }

        public UserPwdRecoveryPartDriver(IOrchardServices orchardServices) {
            _orchardServices = orchardServices;
        }

        protected override DriverResult Editor(UserPwdRecoveryPart part, dynamic shapeHelper) {
            return Editor(part, null, shapeHelper);

        }

        protected override DriverResult Editor(UserPwdRecoveryPart part, IUpdateModel updater, dynamic shapeHelper) {
            if (updater != null) {
                updater.TryUpdateModel(part, Prefix, null, null);
            }
            return ContentShape("Parts_UserPwdRecoveryPart_Edit", () => shapeHelper.EditorTemplate(TemplateName: "Parts/UserPwdRecoveryPart_Edit", Model: part, Prefix: Prefix));
        }

        protected override void Importing(UserPwdRecoveryPart part, ImportContentContext context) {
            var importedInternationalPrefix = context.Attribute(part.PartDefinition.Name, "InternationalPrefix");
            if (importedInternationalPrefix != null) {
                part.InternationalPrefix = importedInternationalPrefix;
            }
            var importedPhoneNumber = context.Attribute(part.PartDefinition.Name, "PhoneNumber");
            if (importedPhoneNumber != null) {
                part.PhoneNumber = importedPhoneNumber;
            }
        }
        protected override void Exporting(UserPwdRecoveryPart part, ExportContentContext context) {
            context.Element(part.PartDefinition.Name).SetAttributeValue("InternationalPrefix", part.InternationalPrefix);
            context.Element(part.PartDefinition.Name).SetAttributeValue("PhoneNumber", part.PhoneNumber);
        }
    }
}