﻿using Laser.Orchard.HID.Extensions;
using Laser.Orchard.HID.Services;
using Newtonsoft.Json.Linq;
using Orchard.Logging;
using System;
using System.IO;
using System.Net;
using System.Text;

namespace Laser.Orchard.HID.Models {
    public class HIDAuthToken {
        public string TokenType { get; private set; }
        public string AccessToken { get; private set; }
        public int ExpiresIn { get; private set; } //these are seconds
        public AuthenticationErrors Error { get; private set; }

        private readonly IHIDAdminService _HIDAdminService;
        public ILogger Logger { get; set; }

        private HIDAuthToken(IHIDAdminService hidService) {
            _HIDAdminService = hidService;
            TokenType = "";
            AccessToken = "";
            Error = AuthenticationErrors.NotAuthenticated;
            Logger = NullLogger.Instance;
        }

        private const string AuthBodyFormat = @"grant_type=client_credentials&client_id={0}&client_secret={1}";

        public static HIDAuthToken Authenticate(IHIDAdminService hidService) {
            return new HIDAuthToken(hidService).Authenticate();
        }
        public HIDAuthToken Authenticate() {
            var settings = _HIDAdminService.GetSiteSettings();
            return Authenticate(settings.ClientID, settings.ClientSecret);
        }
        public HIDAuthToken Authenticate(string userName, string password) {
            var settings = _HIDAdminService.GetSiteSettings();
            var LoginEndpoint = String.Format(HIDAPIEndpoints.LoginEndpointFormat,
                settings.UseTestEnvironment ? HIDAPIEndpoints.IdentityProviderTest : HIDAPIEndpoints.IdentityProviderProd);
            HttpWebRequest wr = HttpWebRequest.CreateHttp(LoginEndpoint);
            wr.Method = WebRequestMethods.Http.Post;
            string bodyText = string.Format(AuthBodyFormat, userName, password);
            byte[] bodyData = Encoding.UTF8.GetBytes(bodyText);
            wr.ContentType = "application/x-www-form-urlencoded";
            wr.Accept = "application/json";
            //wr.ContentLength = bodyData.Length;
            try {
                using (Stream reqStream = wr.GetRequestStream()) {
                    // body stream is written in try-catch, because it needs to resolve destination url
                    reqStream.Write(bodyData, 0, bodyData.Length);
                }
                using (HttpWebResponse resp = wr.GetResponse() as HttpWebResponse) {
                    if (resp.StatusCode == HttpStatusCode.OK) {
                        //read the json response
                        using (var reader = new StreamReader(resp.GetResponseStream())) {
                            string respJson = reader.ReadToEnd();
                            var json = JObject.Parse(respJson);
                            TokenType = json["token_type"].ToString();
                            AccessToken = json["access_token"].ToString();
                            ExpiresIn = int.Parse(json["expires_in"].ToString());
                            Error = AuthenticationErrors.NoError;
                        }
                    }
                }
            } catch (WebException ex) {
                //response code 401 in case login info is invalid
                HttpWebResponse resp = (System.Net.HttpWebResponse)(ex.Response);
                if (resp != null && resp.StatusCode == HttpStatusCode.Unauthorized) {
                    Error = AuthenticationErrors.ClientInfoInvalid;
                } else {
                    Error = AuthenticationErrors.CommunicationError;
                }
            } catch (Exception ex) {
                Error = AuthenticationErrors.CommunicationError;
                Logger.Error(ex, "Fallback error management.");
            }
            return this;
        }

    }
}