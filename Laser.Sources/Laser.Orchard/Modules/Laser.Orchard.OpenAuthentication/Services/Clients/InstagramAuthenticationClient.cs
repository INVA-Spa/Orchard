﻿using DotNetOpenAuth.AspNet;
using Laser.Orchard.OpenAuthentication.Models;
using Laser.Orchard.OpenAuthentication.Security;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;

namespace Laser.Orchard.OpenAuthentication.Services.Clients {
    public class InstagramAuthenticationClient : IExternalAuthenticationClient {
        public string ProviderName {
            get { return "Instagram"; }
        }

        public IAuthenticationClient Build(ProviderConfigurationRecord providerConfigurationRecord) {
            string ClientId = providerConfigurationRecord.ProviderIdKey;
            string ClientSecret = providerConfigurationRecord.ProviderSecret;
            var client = new InstagramOAuth2Client(ClientId, ClientSecret);
            return client;
        }

        public AuthenticationResult GetUserData(ProviderConfigurationRecord clientConfiguration, AuthenticationResult previousAuthResult, string userAccessToken, string userAccessSecretKey = "") {
            var userData = (Build(clientConfiguration) as InstagramOAuth2Client).GetUserDataDictionary(userAccessToken);
            userData["accesstoken"] = userAccessToken;
            string id = userData["id"];
            string username = userData["username"];
            return new AuthenticationResult(true, this.ProviderName, id, username, userData);
        }

        public OpenAuthCreateUserParams NormalizeData(OpenAuthCreateUserParams clientData) {
            return clientData;
        }

        public bool RewriteRequest() {
            return new ServiceUtility().RewriteRequestByState();
        }
    }
}