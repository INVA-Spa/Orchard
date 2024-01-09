using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;
using Orchard.PublishLater.Models;
using Orchard.ContentManagement;
using Orchard.DisplayManagement.Descriptors;
using Orchard.Localization;

namespace Orchard.PublishLater.Services {
    public class PublishLaterUIDescriptorForPicker : IAdditionalUIDescriptor {
        private readonly IOrchardServices _orchardServices;
        public int Priority { get { return 5; }}
        public string UIContext {
            get { return _UIContext; }
            set { _UIContext = value; }
        }

        private string _UIContext = "ContentPicker";

        public PublishLaterUIDescriptorForPicker(IOrchardServices orchardServices) {
            T = NullLocalizer.Instance;
            _orchardServices = orchardServices;
        }

        public Localizer T { get; set; }

        /// <summary>
        /// Builds a description if content is published and contains the archiveLater part
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public string Evaluate(IContent content) {
                var part = content.As<PublishLaterPart>();
                if (part != null) {
                    var publishDate = part.ScheduledPublishUtc;
                    if (publishDate != null && publishDate.Value != null) {
                        var currentCulture = _orchardServices.WorkContext.CurrentCulture;
                        var cultureInfo = CultureInfo.GetCultureInfo(currentCulture);
                        return T("Publishing on {0}", ((DateTime)publishDate.Value).ToString(cultureInfo.DateTimeFormat.ShortDatePattern, cultureInfo)).Text;
                    }
                }
            return string.Empty;
        }
    }
}