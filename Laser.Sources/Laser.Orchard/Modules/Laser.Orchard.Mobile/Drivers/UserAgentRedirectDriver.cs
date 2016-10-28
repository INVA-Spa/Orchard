﻿using Laser.Orchard.Mobile.Models;
using Laser.Orchard.Mobile.Services;
using Laser.Orchard.Mobile.ViewModels;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Drivers;
using Orchard.ContentManagement.Handlers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Xml.Linq;

namespace Laser.Orchard.Mobile.Drivers {
    public class UserAgentRedirectDriver : ContentPartDriver<UserAgentRedirectPart> {
        private readonly IUserAgentRedirectServices _userAgentRedirectServices;
        public UserAgentRedirectDriver(IUserAgentRedirectServices userAgentRedirectServices) {
            _userAgentRedirectServices = userAgentRedirectServices;
        }

        protected override string Prefix {
            get {
                return "UserAgentRedirect";
            }
        }

        protected override DriverResult Display(UserAgentRedirectPart part, string displayType, dynamic shapeHelper) {
            MobileAppStores? mobile = null;
            mobile = _userAgentRedirectServices.GetStoreFromUserAgent();
            if (mobile == null) {
                return null;
            }
            if (part.AutoRedirect) {
                HttpContext.Current.Response.Redirect(part.Stores.Single(w => w.AppStoreKey.Equals(mobile)).RedirectUrl);
                return null;
            } else {
                var store = part.Stores.SingleOrDefault(w => w.AppStoreKey.Equals(mobile));
                if (store == null) {
                    return null;
                }
                return ContentShape("Parts_UserAgentRedirect",
                    () => shapeHelper.Parts_UserAgentRedirect(
                        AppName: part.AppName,
                        Store: store
                        ));
            }
        }
        protected override DriverResult Editor(UserAgentRedirectPart part, dynamic shapeHelper) {

            var editModel = _userAgentRedirectServices.BuildEditModelForUserAgentRedirectPart(part);
            return ContentShape("Parts_UserAgentRedirect_Edit",
                () => shapeHelper.EditorTemplate(
                    TemplateName: "Parts/UserAgentRedirect_Edit",
                    Model: editModel,
                    Prefix: Prefix
                    ));
        }
        protected override DriverResult Editor(UserAgentRedirectPart part, IUpdateModel updater, dynamic shapeHelper) {
            var editModel = _userAgentRedirectServices.BuildEditModelForUserAgentRedirectPart(part);

            if (updater.TryUpdateModel(editModel, Prefix, null, null)) {
                if (part.ContentItem.Id != 0) {
                    // se per caso part.Id è diversa dall'Id registrato nel record relazionato, arrivo da una traduzione, quindi devo trattare tutto come se fosse un nuovo inserimento
                    foreach (var q in editModel.Stores) {
                        if (part.Id != q.UserAgentRedirectPartRecord_Id) {
                            q.UserAgentRedirectPartRecord_Id = part.Id;
                            q.Id = 0;
                        }
                    }
                    _userAgentRedirectServices.Update(part.ContentItem, editModel);
                }
            }
            return Editor(part, shapeHelper);
        }

        #region [ Import/Export ]
        protected override void Exporting(UserAgentRedirectPart part, ExportContentContext context) {

            var root = context.Element(part.PartDefinition.Name);
            XElement partElement = new XElement("Part");
            root.SetAttributeValue("AppName", part.AppName);
            root.SetAttributeValue("AutoRedirect", part.AutoRedirect);
            root.SetAttributeValue("AutoRedirect", part.AutoRedirect);
            root.Add(partElement);
            foreach (var q in part.Stores) {
                XElement question = new XElement("Stores");
                question.SetAttributeValue("AppStoreKey", q.AppStoreKey);
                question.SetAttributeValue("RedirectUrl", q.RedirectUrl);
                root.Add(question);
            }
        }

        protected override void Importing(UserAgentRedirectPart part, ImportContentContext context) {
            var root = context.Data.Element(part.PartDefinition.Name);
            var stores = context.Data.Element(part.PartDefinition.Name).Elements("Stores");
            var editModel = _userAgentRedirectServices.BuildEditModelForUserAgentRedirectPart(part);
            editModel.AutoRedirect = bool.Parse(root.Attribute("AutoRedirect").Value);
            editModel.AppName = root.Attribute("AppName").Value;
            var storeList = new List<AppStoreEdit>();
            foreach (var q in stores) {
                var appStoreEditModel = new AppStoreEdit {
                    AppStoreKey = (MobileAppStores)Enum.Parse(typeof(MobileAppStores), q.Attribute("AppStoreKey").Value),
                    RedirectUrl = q.Attribute("RedirectUrl").Value,
                };
                storeList.Add(appStoreEditModel);
            }
            editModel.Stores = storeList; // metto tutto nel model 
            _userAgentRedirectServices.Update(
                    part.ContentItem, editModel); //aggiorno
        }
        #endregion
    }
}