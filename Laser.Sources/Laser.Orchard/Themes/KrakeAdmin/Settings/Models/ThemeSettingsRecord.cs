﻿using Orchard.ContentManagement;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace KrakeAdmin.Settings.Models {
    public class ThemeSettingsRecord {
        public ThemeSettingsRecord() {
            HeaderLogoUrl = "";
            PlaceholderLogoUrl = "";
            BaseLineText = "";
        }
        public virtual int Id { get; set; }
        public virtual string HeaderLogoUrl { get; set; }
        public virtual string PlaceholderLogoUrl { get; set; }
        public virtual string BaseLineText { get; set; }
        public virtual string PlaceholderSmallLogoUrl { get; internal set; }
    }
}