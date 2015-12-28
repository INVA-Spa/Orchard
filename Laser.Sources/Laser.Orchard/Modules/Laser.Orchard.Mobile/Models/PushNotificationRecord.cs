﻿using System;

namespace Laser.Orchard.Mobile.Models {
    public class PushNotificationRecord {
        public virtual int Id { get; set; }
        public virtual TipoDispositivo Device { get; set; }
        public virtual string UUIdentifier { get; set; }
        public virtual string Token { get; set; }
        public virtual bool Validated { get; set; }
        public virtual DateTime DataInserimento { get; set; }
        public virtual DateTime DataModifica { get; set; }
        public virtual bool Produzione { get; set; }
        public virtual string Language { get; set; }
        public virtual int CommunicationContactPartRecord_Id { get; set; }
    }
    public enum TipoDispositivo { Android, Apple, WindowsMobile }
}