﻿using Laser.Orchard.Pdf.Models;
using Orchard.ContentManagement;
using Orchard.ContentManagement.MetaData;
using Orchard.ContentManagement.MetaData.Builders;
using Orchard.ContentManagement.MetaData.Models;
using Orchard.ContentManagement.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Globalization;

namespace Laser.Orchard.Pdf.Settings {
    public class PrintButtonPartSettingsHooks : ContentDefinitionEditorEventsBase {
        public override IEnumerable<TemplateViewModel> TypePartEditor(ContentTypePartDefinition definition) {
            if (definition.PartDefinition.Name != "PrintButtonPart") yield break;
            var model = definition.Settings.GetModel<PrintButtonPartSettings>();
            yield return DefinitionTemplate(model);
        }

        public override IEnumerable<TemplateViewModel> TypePartEditorUpdate(ContentTypePartDefinitionBuilder builder, IUpdateModel updateModel) {
            if (builder.Name != "PrintButtonPart") yield break;
            var model = new PrintButtonPartSettings();
            updateModel.TryUpdateModel(model, "PrintButtonPartSettings", null, null);

            // carica ogni campo dei settings
            builder.WithSetting("PrintButtonPartSettings.TemplateId", model.TemplateId.ToString());
            builder.WithSetting("PrintButtonPartSettings.FileNameWithoutExtension", model.FileNameWithoutExtension);
            builder.WithSetting("PrintButtonPartSettings.Header", model.Header);
            builder.WithSetting("PrintButtonPartSettings.Footer", model.Footer);
            builder.WithSetting("PrintButtonPartSettings.HeaderHeight", model.HeaderHeight.ToString(CultureInfo.InvariantCulture));
            builder.WithSetting("PrintButtonPartSettings.FooterHeight", model.FooterHeight.ToString(CultureInfo.InvariantCulture));

            yield return DefinitionTemplate(model);
        }
    }
}