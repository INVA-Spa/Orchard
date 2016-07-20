using DotNetOpenAuth.AspNet;
using DotNetOpenAuth.AspNet.Clients;
using Laser.Orchard.OpenAuthentication.Models;
using Laser.Orchard.OpenAuthentication.Security;

namespace Laser.Orchard.OpenAuthentication.Services.Clients {
    public class YahooOpenIdAuthenticationClient : IExternalAuthenticationClient {
        public string ProviderName {
            get { return "Yahoo"; }
        }

        public IAuthenticationClient Build(ProviderConfigurationRecord providerConfigurationRecord) {
            return new YahooOpenIdClient();
        }

        public AuthenticationResult GetUserData(ProviderConfigurationRecord clientConfiguration, string userAccessToken, string userAccessSecret = "") {
            throw new System.NotImplementedException();
        }

        public OpenAuthCreateUserParams NormalizeData(OpenAuthCreateUserParams createUserParams) {
            throw new System.NotImplementedException();
        }
    }
}