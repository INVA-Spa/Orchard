﻿using Orchard.ContentManagement;
using Orchard.ContentManagement.Records;

namespace Laser.Orchard.ShareLink.Models {

    public class ShareLinkPart : ContentPart<ShareLinkPartRecord> {

        public string SharedLink {
            get { return this.Retrieve(r => r.SharedLink); }
            set { this.Store(r => r.SharedLink, value); }
        }

        public string SharedText {
            get { return this.Retrieve(r => r.SharedText); }
            set { this.Store(r => r.SharedText, value); }
        }

        public string SharedImage {
            get { return this.Retrieve(r => r.SharedImage); }
            set { this.Store(r => r.SharedImage, value); }
        }

        public string SharedIdImage {
            get { return this.Retrieve(r => r.SharedIdImage); }
            set { this.Store(r => r.SharedIdImage, value); }
        }
    }

    public class ShareLinkPartRecord : ContentPartRecord {
        public virtual string SharedLink { get; set; }
        public virtual string SharedText { get; set; }
        public virtual string SharedImage { get; set; }
        public virtual string SharedIdImage { get; set; }
    }
}