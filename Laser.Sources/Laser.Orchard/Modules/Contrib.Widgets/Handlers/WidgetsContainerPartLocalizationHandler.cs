﻿using Contrib.Widgets.Models;
using Contrib.Widgets.Services;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Handlers;
using Orchard.Localization.Models;
using System.Linq;

namespace Contrib.Widgets.Handlers {
    public class WidgetsContainerPartLocalizationHandler : ContentHandler {

        private readonly IWidgetManager _widgetManager;

        public WidgetsContainerPartLocalizationHandler(
            IWidgetManager widgetManager) {

            _widgetManager = widgetManager;

            OnCloned<WidgetsContainerPart>(ResetWidgetsLocalization);
        }
        /// <summary>
        /// Reset the culture of all widgets in the cloned content
        /// </summary>
        /// <param name="context"></param>
        /// <param name="part"></param>
        private void ResetWidgetsLocalization(CloneContentContext context, WidgetsContainerPart part) {
            var baseLocPart = part.ContentItem.As<LocalizationPart>();
            if (baseLocPart != null) {
                var widgetsLocParts = _widgetManager
                    .GetWidgets(context.CloneContentItem.Id, context.CloneContentItem.IsPublished())
                    .Select(wi => wi.ContentItem.As<LocalizationPart>())
                    .Where(pa => pa != null);
                foreach (var wLocPart in widgetsLocParts) {
                    wLocPart.Culture = null;
                }
            }
        }
    }
}