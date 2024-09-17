using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;
using Orchard.ArchiveLater.Models;
using Orchard.ContentManagement;
using Orchard.DisplayManagement.Descriptors;
using Orchard.Localization;

namespace Orchard.ArchiveLater.Services {
    public class ArchiveLaterUIDescriptorForPicker : IAdditionalUIDescriptor {
        private readonly IOrchardServices _orchardServices;
        public int Priority { get { return 5; }}
        public string UIContext {
            get { return _UIContext; }
            set { _UIContext = value; }
        }

        private string _UIContext = "ContentPicker";

        public ArchiveLaterUIDescriptorForPicker(IOrchardServices orchardServices) {
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
                var part = content.As<ArchiveLaterPart>();
                if (part != null) {
                    var archiveDate = part.ScheduledArchiveUtc;
                    if (archiveDate != null && archiveDate.Value != null) {
                        var currentCulture = _orchardServices.WorkContext.CurrentCulture;
                        var cultureInfo = CultureInfo.GetCultureInfo(currentCulture);
                        return T("Archiving on {0}", ((DateTime)archiveDate.Value).ToString(cultureInfo.DateTimeFormat.ShortDatePattern, cultureInfo)).Text;
                    }
                }
            return string.Empty;
        }
    }
}