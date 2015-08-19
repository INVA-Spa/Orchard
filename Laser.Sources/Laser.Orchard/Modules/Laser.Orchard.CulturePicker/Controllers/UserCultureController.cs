﻿using System;
using System.Web;
using System.Web.Mvc;
using Orchard.Autoroute.Models;
using Laser.Orchard.CulturePicker.Services;
using Orchard.Environment.Configuration;
using Orchard.Localization;
using Orchard.Mvc.Extensions;
using Orchard;

namespace Laser.Orchard.CulturePicker.Controllers {
    public class UserCultureController : Controller {
        private readonly ILocalizableContentService _localizableContentService;
        private readonly ICulturePickerServices _cpServices;

        public UserCultureController(IOrchardServices services, ILocalizableContentService localizableContentService, ICulturePickerServices cpServices) {
            Services = services;
            _localizableContentService = localizableContentService;
            _cpServices = cpServices;
        }

        public IOrchardServices Services { get; set; }
        public Localizer T { get; set; }

        public ActionResult ChangeCulture(string cultureName) {
            if (string.IsNullOrEmpty(cultureName)) {
                throw new ArgumentNullException(cultureName);
            }
            var urlPrefix = Services.WorkContext.Resolve<ShellSettings>().RequestUrlPrefix;

            string returnUrl = Utils.GetReturnUrl(Services.WorkContext.HttpContext.Request, urlPrefix);

            AutoroutePart currentRoutePart;
            //returnUrl may not correspond to any content and we use "Try" approach
            if (_localizableContentService.TryGetRouteForUrl(returnUrl, out currentRoutePart)) {
                AutoroutePart localizedRoutePart;
                //content may not have localized version and we use "Try" approach
                if (_localizableContentService.TryFindLocalizedRoute(currentRoutePart.ContentItem, cultureName, out localizedRoutePart)) {
                    returnUrl = String.IsNullOrWhiteSpace(urlPrefix) ? localizedRoutePart.Path : urlPrefix + "/" + localizedRoutePart.Path;
                }
            }

            if (!String.IsNullOrWhiteSpace(urlPrefix) && !returnUrl.StartsWith(urlPrefix)) {
                returnUrl = urlPrefix + "/" + returnUrl;
            }
            _cpServices.SaveCultureCookie(cultureName, Services.WorkContext.HttpContext);

            //support for Orchard < 1.6
            //TODO: discontinue in 2013 Q2
            Version orchardVersion = Utils.GetOrchardVersion();
            if (orchardVersion < new Version(1, 6)) {
                returnUrl = Url.Encode(returnUrl);
            } else {
                if (!returnUrl.StartsWith("~/")) {
                    returnUrl = "~/" + returnUrl;
                }
            }

            return this.RedirectLocal(returnUrl);
        }

        #region Helpers

        #endregion
    }
}