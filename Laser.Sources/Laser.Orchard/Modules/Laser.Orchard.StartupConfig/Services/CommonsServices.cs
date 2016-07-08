﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard;
using Orchard.ContentManagement;
using Orchard.Autoroute.Models;
using System.Xml.Linq;
using System.Globalization;
using Orchard.Services;
using System.Text;
using Orchard.Security;

namespace Laser.Orchard.StartupConfig.Services {
    public class CommonsServices : ICommonsServices {
        private readonly IOrchardServices _orchardServices;
        private readonly IClock _clock;
        private readonly IEncryptionService _encryptionService;

        public CommonsServices(IOrchardServices orchardServices, IClock clock, IEncryptionService encryptionService) {
            _orchardServices = orchardServices;
            _clock = clock;
            _encryptionService = encryptionService;
        }

        public DevicesBrands GetDeviceBrandByUserAgent() {
            var userAgent = _orchardServices.WorkContext.HttpContext.Request.UserAgent.ToLower().Trim();
            if (userAgent.Contains("iphone") || userAgent.Contains("ipod") || userAgent.Contains("ipad")) {
                return DevicesBrands.Apple;
            } else if (userAgent.Contains("windows")) {
                return  DevicesBrands.Windows;
            } else if (userAgent.Contains("android")) {
                return DevicesBrands.Google;
            } else {
                return DevicesBrands.Unknown;
            }

        }

        public IContent GetContentByAlias(string displayAlias) {
            IContent item = null;
            var autoroutePart = _orchardServices.ContentManager.Query<AutoroutePart, AutoroutePartRecord>()
                .ForVersion(VersionOptions.Published)
                .Where(w => w.DisplayAlias == displayAlias).List().SingleOrDefault();

            if (autoroutePart != null && autoroutePart.ContentItem != null) {
                item = autoroutePart.ContentItem;
            } else {
                new HttpException(404, ("Not found"));
                return null;
            }
            return item;
        }

        public string CreateNonce(string parametri, TimeSpan delay) {
            var challengeToken = new XElement("n", new XAttribute("par", parametri), new XAttribute("utc", _clock.UtcNow.ToUniversalTime().Add(delay).ToString(CultureInfo.InvariantCulture))).ToString();
            var data = Encoding.UTF8.GetBytes(challengeToken);
            return Convert.ToBase64String(_encryptionService.Encode(data));
        }

        public bool DecryptNonce(string nonce, out string parametri, out DateTime validateByUtc) {
            parametri = null;
            validateByUtc = _clock.UtcNow;

            try {
                var data = _encryptionService.Decode(Convert.FromBase64String(nonce));
                var xml = Encoding.UTF8.GetString(data);
                var element = XElement.Parse(xml);
                parametri = element.Attribute("par").Value;
                validateByUtc = DateTime.Parse(element.Attribute("utc").Value, CultureInfo.InvariantCulture);
                return _clock.UtcNow <= validateByUtc;
            } catch {
                return false;
            }
        }

    }
}