﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Orchard;
using Orchard.ContentManagement;

using Orchard.UI.Navigation;
using Orchard.Environment.Configuration;
using Orchard.Autoroute.Services;
using Laser.Orchard.ShortLinks.Models;
using System.Web;
using System.Web.Mvc;
using Orchard.Autoroute.Models;
using Orchard.Mvc.Html;
using Orchard.Mvc.Extensions;
using System.Net;
using System.Text;
using System.IO;
using Orchard.UI.Notify;
using Orchard.Localization;
namespace Laser.Orchard.ShortLinks.Services {
    public class ShortLinksService : IShortLinksService {
        public Localizer T {get;set;}
        private readonly IAutorouteService _autorouteService;
        private readonly INotifier _notifier;
        private readonly ShellSettings _shellSettings;
        private readonly IOrchardServices _orchardServices;
        public ShortLinksService(IAutorouteService autorouteService, ShellSettings shellSettings, IOrchardServices orchardServices, INotifier notifier) {
            _autorouteService = autorouteService;
            _shellSettings = shellSettings;
            _orchardServices = orchardServices;
            _notifier = notifier;
            T = NullLocalizer.Instance;
        }

        public string GetShortLink(ContentPart part) {
            //string shorturl = "";
            string longuri = GetFullAbsoluteUrl(part);
            return GetShortLink(longuri);
            //var apiKey = _orchardServices.WorkContext.CurrentSite.As<Laser.Orchard.ShortLinks.Models.ShortLinksSettingsPart>().GoogleApiKey;
            //string apiurl = "https://www.googleapis.com/urlshortener/v1/url?key=" + apiKey;
            //var request = (HttpWebRequest)WebRequest.Create(apiurl);
            //var postData = "{'longUrl':'" + longuri + "'}";
            //request.Method = "POST";
            //request.ContentType = "application/json";
            //using (var stream = new StreamWriter(request.GetRequestStream())) {
            //    stream.Write(postData);
            //    stream.Flush();
            //    stream.Close();
            //}
            //var response = (HttpWebResponse)request.GetResponse();
            //using (var streamReader = new StreamReader(response.GetResponseStream())) {
            //    var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
            //    var jsondict = serializer.Deserialize<Dictionary<string, string>>(streamReader.ReadToEnd());
            //    shorturl = jsondict["id"];
            //}
            //return shorturl;
        }

        public string GetShortLink(string myurl) {
            if (String.IsNullOrWhiteSpace(myurl)) return "";
            string shorturl = "";
            string longuri = myurl;// GetFullAbsoluteUrl(part);
            string apiKey = _orchardServices.WorkContext.CurrentSite.As<Laser.Orchard.ShortLinks.Models.ShortLinksSettingsPart>().GoogleApiKey;
            if (string.IsNullOrEmpty(apiKey)) {
                _notifier.Add(NotifyType.Error, T("No Shorturl Setting Found"));
                //https://console.developers.google.com/apis/credentials
            }
            else {
                string apiurl = "https://www.googleapis.com/urlshortener/v1/url?key=" + apiKey;
                var request = (HttpWebRequest)WebRequest.Create(apiurl);
                var postData = "{'longUrl':'" + longuri + "'}";
                request.Method = "POST";
                request.ContentType = "application/json";
                using (var stream = new StreamWriter(request.GetRequestStream())) {
                    stream.Write(postData);
                    stream.Flush();
                    stream.Close();
                }
                var response = (HttpWebResponse)request.GetResponse();
                using (var streamReader = new StreamReader(response.GetResponseStream())) {
                    var serializer = new System.Web.Script.Serialization.JavaScriptSerializer();
                    var jsondict = serializer.Deserialize<Dictionary<string, string>>(streamReader.ReadToEnd());
                    shorturl = jsondict["id"];
                }
            }
            return shorturl;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="part"></param>
        /// <returns></returns>
        public string GetFullAbsoluteUrl(ContentPart part) {
            var urlHelper = new UrlHelper(_orchardServices.WorkContext.HttpContext.Request.RequestContext);
            return urlHelper.MakeAbsolute(urlHelper.ItemDisplayUrl(part));

            //UrlHelper url = new UrlHelper(HttpContext.Current.Request.RequestContext);     
            //var tenantName = _shellSettings.Name;
            //var alias = ((dynamic)part.ContentItem).AutoroutePart.DisplayAlias;
            //var fullUrl = String.Format("{0}/{1}/{2}", HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority), tenantName, alias);
            //var fullAbsoluteUrl = "";
            //if (String.IsNullOrWhiteSpace(tenantName) || tenantName == "Default") {
            //    fullUrl = String.Format("{0}/{1}", HttpContext.Current.Request.Url.GetLeftPart(UriPartial.Authority), alias);
            //}
            //fullAbsoluteUrl = url.Content(fullUrl);
            //return fullAbsoluteUrl;
        }
    }
}