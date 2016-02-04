﻿using Orchard.ContentManagement;
using Orchard.ContentManagement.Records;
using Orchard.Environment.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Laser.Orchard.Mobile.Models {
    [OrchardFeature("Laser.Orchard.SmsGateway")]
    public class SmsGatewayPartRecord : ContentPartRecord {

        public virtual string Message { get; set; }
        public virtual bool HaveAlias { get; set; }
        public virtual string Alias { get; set; }
        public virtual bool SmsMessageSent { get; set; }
        public virtual bool SendToTestNumber { get; set; }
        public virtual string NumberForTest { get; set; }
        public virtual bool SendOnNextPublish { get; set; }
    }

    [OrchardFeature("Laser.Orchard.SmsGateway")]
    public class SmsGatewayPart : ContentPart<SmsGatewayPartRecord> {

        public string Message {
            get { return this.Retrieve(x => x.Message); }
            set { this.Store(x => x.Message, value); }
        }

        public bool HaveAlias {
            get { return this.Retrieve(x => x.HaveAlias); }
            set { this.Store(x => x.HaveAlias, value); }
        }

        public string Alias {
            get { return this.Retrieve(x => x.Alias); }
            set { this.Store(x => x.Alias, value); }
        }

        public bool SmsMessageSent {
            get { return this.Retrieve(x => x.SmsMessageSent); }
            set { this.Store(x => x.SmsMessageSent, value); }
        }

        public bool SendToTestNumber {
            get { return this.Retrieve(x => x.SendToTestNumber); }
            set { this.Store(x => x.SendToTestNumber, value); }
        }

        public string NumberForTest {
            get { return this.Retrieve(x => x.NumberForTest); }
            set { this.Store(x => x.NumberForTest, value); }
        }

        public bool SendOnNextPublish {
            get { return this.Retrieve(x => x.SendOnNextPublish); }
            set { this.Store(x => x.SendOnNextPublish, value); }
        }

    }
}