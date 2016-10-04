﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Laser.Orchard.StartupConfig.Models;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Drivers;
using Orchard.ContentManagement.Handlers;
using Orchard;
using OrchardData = Orchard.Data;
using OrchardLocalization = Orchard.Localization;
using Orchard.UI.Admin;

namespace Laser.Orchard.StartupConfig.Drivers {
    public class FavoriteCulturePartDriver : ContentPartDriver<FavoriteCulturePart> {
        private readonly IOrchardServices _orchardServices;
        protected override string Prefix {
            get {
                return "FavoriteCulturePart";
            }
        }
        public FavoriteCulturePartDriver(IOrchardServices orchardServices) {
            _orchardServices = orchardServices;
        }
        protected override DriverResult Display(FavoriteCulturePart part, string displayType, dynamic shapeHelper) {
            bool isAdmin = AdminFilter.IsApplied(_orchardServices.WorkContext.HttpContext.Request.RequestContext);
            if (isAdmin && (displayType == "Detail")) {
                string culture = "";
                OrchardData.IRepository<OrchardLocalization.Records.CultureRecord> cultureRepository;
                if (_orchardServices.WorkContext.TryResolve<OrchardData.IRepository<OrchardLocalization.Records.CultureRecord>>(out cultureRepository)) {
                    var cultureRecord = cultureRepository.Get(part.Culture_Id);
                    if (cultureRecord != null) {
                        culture = cultureRecord.Culture;
                    }
                }
                return ContentShape("Parts_FavoriteCulturePart",
                    () => shapeHelper.Parts_FavoriteCulturePart(Culture: culture));
            }
            else {
                return null;
            }
        }
        protected override DriverResult Editor(FavoriteCulturePart part, dynamic shapeHelper) {
            return Editor(part, null, shapeHelper);
        }
        protected override DriverResult Editor(FavoriteCulturePart part, IUpdateModel updater, dynamic shapeHelper) {
            if (updater != null) {
                if (updater.TryUpdateModel(part, Prefix, null, null)) { 
                
                }
            }
            return ContentShape("Parts_FavoriteCulturePart_Edit",
                                () => shapeHelper.EditorTemplate(TemplateName: "Parts/FavoriteCulturePart_Edit",
                                    Model: part,
                                    Prefix: Prefix)
                                    .ListCulture(new List<string> { "ciao", "ciao2"})
                                    );

        }

        protected override void Importing(FavoriteCulturePart part, ImportContentContext context) {
            var importedCulture_Id = context.Attribute(part.PartDefinition.Name, "Culture_Id");
            if (importedCulture_Id != null) {
                part.Culture_Id = int.Parse(importedCulture_Id);
            }
        }

        protected override void Exporting(FavoriteCulturePart part, ExportContentContext context) {
            context.Element(part.PartDefinition.Name).SetAttributeValue("Culture_Id", part.Culture_Id);
        }
    }
}