﻿using Orchard.MediaLibrary.Models;
using System;
using System.Collections.Generic;
using System.Web.Configuration;

namespace Orchard.MediaLibrary.ViewModels {
    public class MediaManagerFolderCreateViewModel {
        public string Name { get; set; }
        public string FolderPath { get; set; }
        public IEnumerable<MediaFolder> Hierarchy { get; set; }
        public string InvalidCharactersPattern {
            get {
                string invalidChars = ((SystemWebSectionGroup)WebConfigurationManager.OpenWebConfiguration(null).GetSectionGroup("system.web")).HttpRuntime.RequestPathInvalidCharacters;
                List<string> invalidCharacters = new List<string>() { "/", @"\" };
                invalidCharacters.AddRange(invalidChars.Split(new string[] { "," }, StringSplitOptions.RemoveEmptyEntries));
                string pattern = @"[^/" + string.Join("", invalidCharacters) + @"]+";
                return pattern.Replace(@"\", @"\\");
            }
        }
    }
}
