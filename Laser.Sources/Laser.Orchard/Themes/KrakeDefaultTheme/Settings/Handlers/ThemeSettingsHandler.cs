﻿using KrakeDefaultTheme.Settings.Models;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Handlers;
using Orchard.Localization;
using Orchard.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace KrakeDefaultTheme.Settings.Handlers {
    public class ThemeSettingsHandler : ContentHandler {

        public ThemeSettingsHandler() {
            T = NullLocalizer.Instance;
            Logger = NullLogger.Instance;

            Filters.Add(new ActivatingFilter<ThemeSettingsPart>("Site"));
        }

        public Localizer T { get; set; }
        protected override void GetItemMetadata(GetContentItemMetadataContext context) {
            if (context.ContentItem.ContentType != "Site")
                return;
            base.GetItemMetadata(context);
            context.Metadata.EditorGroupInfo.Add(new GroupInfo(T("Krake")));
        }
    }
}