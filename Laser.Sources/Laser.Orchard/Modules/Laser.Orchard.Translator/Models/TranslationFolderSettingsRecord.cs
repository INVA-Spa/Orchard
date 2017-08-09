﻿namespace Laser.Orchard.Translator.Models {
    public class TranslationFolderSettingsRecord {
        public virtual int Id { get; set; }
        public virtual string ContainerName { get; set; }
        public virtual string ContainerType { get; set; }
        public virtual string Language { get; set; }
        public virtual bool Deprecated { get; set; }
    }
}