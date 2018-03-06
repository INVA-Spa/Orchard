﻿using Laser.Orchard.HID.Extensions;
using Laser.Orchard.HID.Services;
using Newtonsoft.Json.Linq;
using Orchard.Logging;
using Orchard.Security;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;

namespace Laser.Orchard.HID.Models {
    public class HIDUser {
        private string _location;
        public string Location { get {
                if (!LocationIsValid()) {
                    _location = _HIDService.UsersEndpoint + "/" + Id.ToString();
                }
                return _location;
            }
            set { _location = value; } }
        public int Id { get; set; } //id of user in HID systems
        public string ExternalId { get; set; }
        public string FamilyName { get; set; }
        public string GivenName { get; set; }
        public List<string> Emails { get; set; }
        public string Status { get; set; }
        public List<int> InvitationIds { get; set; }
        public List<HIDCredentialContainer> CredentialContainers { get; set; }
        public UserErrors Error { get; set; }

        private readonly IHIDAdminService _HIDService;

        public ILogger Logger { get; set; }

        public HIDUser() {
            Emails = new List<string>();
            InvitationIds = new List<int>();
            CredentialContainers = new List<HIDCredentialContainer>();
            Error = UserErrors.UnknownError;
            Logger = NullLogger.Instance;
        }

        private HIDUser(IHIDAdminService hidService)
            : this() {
            _HIDService = hidService;
        }

        public static string GenerateExternalId(int id) {
            return Constants.LocalArea + id.ToString();
        }

        private void PopulateFromJson(JObject json, bool onlyActiveContainers = true) {
            Id = int.Parse(json["id"].ToString()); //no null-checks for required properties
            ExternalId = json["externalId"].ToString();
            FamilyName = json["name"]["familyName"].ToString();
            GivenName = json["name"]["givenName"].ToString();
            Emails.AddRange(json["emails"].Children().Select(jt => jt["value"].ToString()));
            Emails = Emails.Distinct().ToList();
            Status = json["status"] != null ? json["status"].ToString() : "";
            Location = json["meta"]["location"].ToString();
            if (json["urn:hid:scim:api:ma:1.0:UserInvitation"] != null) {
                InvitationIds.AddRange(json["urn:hid:scim:api:ma:1.0:UserInvitation"].Children().Select(jt => int.Parse(jt["id"].ToString())));
                InvitationIds = InvitationIds.Distinct().ToList();
            }
            if (json["urn:hid:scim:api:ma:1.0:CredentialContainer"] != null) {
                CredentialContainers.Clear();
                var avStrings = _HIDService.GetSiteSettings().AppVersionStrings; // used to validate our apps
                CredentialContainers.AddRange(
                    json["urn:hid:scim:api:ma:1.0:CredentialContainer"]
                    .Children()
                    .Select(jt => new HIDCredentialContainer(jt, _HIDService))
                    // we can avoid trying to maange conatiners that have been deleted, or that have not yet been initialized
                    .Where(cc => onlyActiveContainers ? cc.Status == "ACTIVE" : true) 
                    .Where(cc => avStrings.Any(avs => cc.ApplicationVersion.Contains(avs))) //validate apps
                    );
            }
            Error = UserErrors.NoError;
        }

        private void ErrorFromStatusCode(HttpStatusCode sc) {
            switch (sc) {
                case HttpStatusCode.BadRequest:
                    Error = UserErrors.InvalidParameters;
                    break;
                case HttpStatusCode.Conflict:
                    Error = UserErrors.EmailNotUnique;
                    break;
                case HttpStatusCode.InternalServerError:
                    Error = UserErrors.InternalServerError;
                    break;
                case HttpStatusCode.PreconditionFailed:
                    Error = UserErrors.PreconditionFailed;
                    break;
                case HttpStatusCode.NotFound:
                    Error = UserErrors.DoesNotExist;
                    break;
                default:
                    Error = UserErrors.UnknownError;
                    break;
            }
        }

        /// <summary>
        /// Get a specific user from HID's systems.
        /// </summary>
        /// <param name="hidService">The IHIDAdminService implementation to use.</param>
        /// <param name="location">This is the complete endpoint corresponding to the user in HID's systems.</param>
        /// <returns>The HIDUser gotten from HID's systems.</returns>
        public static HIDUser GetUser(IHIDAdminService hidService, string location) {
            var id = IdFromLocation(location);
            return new HIDUser(hidService) { Id = id, Location = location }.GetUser();
        }

        public HIDUser GetUser() {
            if (!_HIDService.VerifyAuthentication()) {
                Error = UserErrors.AuthorizationFailed;
                return this;
            }
            
            HttpWebRequest wr = HttpWebRequest.CreateHttp(Location);
            wr.Method = WebRequestMethods.Http.Get;
            wr.ContentType = Constants.DefaultContentType;
            wr.Headers.Add(HttpRequestHeader.Authorization, _HIDService.AuthorizationToken);
            try {
                using (HttpWebResponse resp = wr.GetResponse() as HttpWebResponse) {
                    if (resp.StatusCode == HttpStatusCode.OK) {
                        //read the json response
                        using (var reader = new StreamReader(resp.GetResponseStream())) {
                            string respJson = reader.ReadToEnd();
                            PopulateFromJson(JObject.Parse(respJson));
                        }
                    }
                }
            } catch (WebException ex) {
                HttpWebResponse resp = (System.Net.HttpWebResponse)(ex.Response);
                if (resp != null) {
                    if (resp.StatusCode == HttpStatusCode.Unauthorized) {
                        // Authentication could have expired while this method was running
                        if (_HIDService.Authenticate() == AuthenticationErrors.NoError) {
                            return GetUser();
                        }
                        Error = UserErrors.AuthorizationFailed;
                    } else {
                        ErrorFromStatusCode(resp.StatusCode);
                    }
                } else {
                    Error = UserErrors.UnknownError;
                }
            } catch (Exception ex) {
                Error = UserErrors.UnknownError;
                Logger.Error(ex, "Fallback error management.");
            }
            return this;
        }

        private const string UserNameFormat = @"'name':{{ 'familyName': '{0}', 'givenName': '{1}'}}";

        private string UserNameBlock {
            get { return string.Format(UserNameFormat, 
                string.IsNullOrWhiteSpace(FamilyName) ? "FamilyName" : FamilyName, 
                string.IsNullOrWhiteSpace(GivenName) ? "GivenName" : GivenName); }
        }

        private const string UserCreateFormat = @"{{ 'schemas':[ 'urn:hid:scim:api:ma:1.0:UserAction', 'urn:ietf:params:scim:schemas:core:2.0:User' ], 'externalId': '{0}', {1}, 'emails':[ {{ {2} }} ], 'urn:hid:scim:api:ma:1.0:UserAction':{{ 'createInvitationCode':'N', 'sendInvitationEmail':'N', 'assignCredential':'N', 'partNumber':'', 'credential':'' }}, 'meta':{{ 'resourceType':'PACSUser' }} }}";

        private string CreateUserBody {
            get {
                return JObject.Parse(
                    string.Format(UserCreateFormat, 
                        ExternalId, 
                        UserNameBlock, 
                        string.Join(", ", Emails.Select(em => string.Format(@"'value':'{0}'", em.ToLowerInvariant())))))
                    .ToString();
            }
        }

        public static HIDUser CreateUser(IHIDAdminService hidService, IUser oUser, string familyName, string givenName) {
            return CreateUser(hidService, oUser.Id, familyName, givenName, oUser.Email);
        }

        public static HIDUser CreateUser(IHIDAdminService hidService, IUser oUser, string familyName, string givenName, string email) {
            return CreateUser(hidService, oUser.Id, familyName, givenName, email);
        }

        public static HIDUser CreateUser(IHIDAdminService hidService, int id, string familyName, string givenName, string email) {
            return CreateUser(hidService, string.Format(hidService.ExternalIdFormat, id.ToString()), familyName, givenName, email);
        }

        public static HIDUser CreateUser(IHIDAdminService hidService, string extId, string familyName, string givenName, string email) {
            HIDUser user = new HIDUser(hidService) { ExternalId = extId, FamilyName = familyName, GivenName = givenName };
            user.Emails.Add(email);
            return user.CreateUser();
        }

        /// <summary>
        /// This method goes and creates the user information in HID's systems.
        /// </summary>
        /// <returns>This very user.</returns>
        public HIDUser CreateUser() {
            if (!_HIDService.VerifyAuthentication()) {
                Error = UserErrors.AuthorizationFailed;
                return this;
            }

            HttpWebRequest wr = HttpWebRequest.CreateHttp(_HIDService.UsersEndpoint);
            wr.Method = WebRequestMethods.Http.Post;
            wr.ContentType = Constants.DefaultContentType;
            wr.Headers.Add(HttpRequestHeader.Authorization, _HIDService.AuthorizationToken);
            byte[] bodyData = Encoding.UTF8.GetBytes(CreateUserBody);
            
            try {
                using (Stream reqStream = wr.GetRequestStream()) {
                    // body stream is written in try-catch, because it needs to resolve destination url
                    reqStream.Write(bodyData, 0, bodyData.Length);
                }
                using (HttpWebResponse resp = wr.GetResponse() as HttpWebResponse) {
                    if (resp.StatusCode == HttpStatusCode.Created) {
                        using (var reader = new StreamReader(resp.GetResponseStream())) {
                            string respJson = reader.ReadToEnd();
                            // populate the properties of the current HIDUser object with the values coming in the
                            // response from HID's systems.
                            PopulateFromJson(JObject.Parse(respJson)); 
                        }
                    }
                }
            } catch (WebException ex) {
                HttpWebResponse resp = (System.Net.HttpWebResponse)(ex.Response);
                if (resp != null) {
                    if (resp.StatusCode == HttpStatusCode.Unauthorized) {
                        // Authentication could have expired while this method was running
                        if (_HIDService.Authenticate() == AuthenticationErrors.NoError) {
                            return CreateUser();
                        }
                        Error = UserErrors.AuthorizationFailed;
                    } else {
                        ErrorFromStatusCode(resp.StatusCode);
                    }
                } else {
                    Error = UserErrors.UnknownError;
                }
            } catch (Exception ex) {
                Error = UserErrors.UnknownError;
                Logger.Error(ex, "Fallback error management.");
            }
            return this;
        }

        private const string InvitationCreateFormat = @"{ 'schemas':[ 'urn:hid:scim:api:ma:1.0:UserAction' ], 'urn:hid:scim:api:ma:1.0:UserAction':{ 'createInvitationCode':'Y', 'sendInvitationEmail':'N', 'assignCredential':'N', 'partNumber':'', 'credential':'' }, 'meta':{ 'resourceType':'PACSUser' } }";

        private string CreateInvitationBody {
            get { return JObject.Parse(InvitationCreateFormat).ToString(); }
        }

        public string CreateInvitation() {
            if (!_HIDService.VerifyAuthentication()) {
                Error = UserErrors.AuthorizationFailed;
                return "";
            }
            
            string invitationCode = "";

            HttpWebRequest wr = HttpWebRequest.CreateHttp(Location + "/invitation");
            wr.Method = WebRequestMethods.Http.Post;
            wr.ContentType = Constants.DefaultContentType;
            wr.Headers.Add(HttpRequestHeader.Authorization, _HIDService.AuthorizationToken);
            byte[] bodyData = Encoding.UTF8.GetBytes(CreateInvitationBody);
            
            try {
                using (Stream reqStream = wr.GetRequestStream()) {
                    // body stream is written in try-catch, because it needs to resolve destination url
                    reqStream.Write(bodyData, 0, bodyData.Length);
                }
                using (HttpWebResponse resp = wr.GetResponse() as HttpWebResponse) {
                    if (resp.StatusCode == HttpStatusCode.Created) {
                        using (var reader = new StreamReader(resp.GetResponseStream())) {
                            string respJson = reader.ReadToEnd();
                            JObject json = JObject.Parse(respJson);
                            var invitation = json["urn:hid:scim:api:ma:1.0:UserInvitation"].Children().First();
                            InvitationIds.Add(int.Parse(invitation["id"].ToString()));
                            invitationCode = invitation["invitationCode"].ToString();
                            Error = UserErrors.NoError;
                        }
                    }
                }
            } catch (WebException ex) {
                HttpWebResponse resp = (System.Net.HttpWebResponse)(ex.Response);
                if (resp != null) {
                    if (resp.StatusCode == HttpStatusCode.Unauthorized) {
                        // Authentication could have expired while this method was running
                        if (_HIDService.Authenticate() == AuthenticationErrors.NoError) {
                            return CreateInvitation();
                        }
                        Error = UserErrors.AuthorizationFailed;
                    } else {
                        ErrorFromStatusCode(resp.StatusCode);
                    }
                } else {
                    Error = UserErrors.UnknownError;
                }
            } catch (Exception ex) {
                Error = UserErrors.UnknownError;
                Logger.Error(ex, "Fallback error management.");
            }
            //a valid invitation code is in the form ABCD-EFGH-ILMN-OPQR
            //16 useful characters with an hyphen separator
            const string pattern = @"(\w){4}-(\w){4}-(\w){4}-(\w){4}";
            if (new Regex(pattern).Match(invitationCode).Success) {
                return invitationCode;
            }
            Error = UserErrors.UnknownError;
            return "";
        }
        
        /// <summary>
        /// Task HID's systems with issueing a credential for the given part number
        /// </summary>
        /// <param name="partNumber">The Part Number for which we are going to issue the credential.</param>
        /// <param name="onlyLatestContainer">Tells wether to attept issueing credentials only for the most recent 
        /// container for each of the user's devices.</param>
        /// <returns></returns>
        /// <remarks>Passing an empty string for partNumber means we will try to have HID issue a credential for
        /// the default PartNumber. Contrary to the fact that is called a "default" Part Nunmber, it is something
        /// that HID's customers have to configure explicitly. If they did not, the Issue will fail.</remarks>
        public HIDUser IssueCredential(string partNumber, bool onlyLatestContainer = true) {
            if (!_HIDService.VerifyAuthentication()) {
                Error = UserErrors.AuthorizationFailed;
                return this;
            }

            if (CredentialContainers.Count == 0) {
                Error = UserErrors.DoesNotHaveDevices;
            }
            if (onlyLatestContainer && CredentialContainers.Count > 1) {
                //IEnumerable<T>.Distinct should preserve the ordering, but it is not actually guaranteed to
                CredentialContainers = CredentialContainers
                    .GroupBy(cc => cc.Manufacturer)
                    .SelectMany(group => {
                        return group
                            .GroupBy(cc => cc.Model)
                            .Select(sub => sub.OrderByDescending(cc => cc.Id).First());
                    }).ToList();
            }
            foreach (var credentialContainer in CredentialContainers) {
                credentialContainer.IssueCredential(partNumber, _HIDService);
                //error handling:
                switch (credentialContainer.Error) {
                    case CredentialErrors.NoError:
                        Error = UserErrors.NoError;
                        break;
                    case CredentialErrors.UnknownError:
                        Error = UserErrors.UnknownError;
                        break;
                    case CredentialErrors.CredentialDeliveredAlready:
                        Error = UserErrors.NoError;
                        break;
                    case CredentialErrors.AuthorizationFailed:
                        // Authentication could have expired while this method was running
                        if (_HIDService.Authenticate() == AuthenticationErrors.NoError) {
                            return IssueCredential(partNumber);
                        }
                        Error = UserErrors.AuthorizationFailed;
                        break;
                    default:
                        break;
                }
            }

            return this;
        }

        /// <summary>
        /// Task HID's systems with issueing a credential for the given part number to the given credential container
        /// </summary>
        /// <param name="partNumber">The Part Number for which we are going to issue the credential</param>
        /// <param name="endpointId">The Id in HID's systems of the credential container we will be trying to issue
        /// credentials to.</param>
        /// <returns></returns>
        /// <remarks>Passing an empty string for partNumber means we will try to have HID issue a credential for
        /// the default PartNumber. Contrary to the fact that is called a "default" Part Nunmber, it is something
        /// that HID's customers have to configure explicitly. If they did not, the Issue will fail.</remarks>
        public HIDUser IssueCredential(string partNumber, int endpointId) {
            if (!_HIDService.VerifyAuthentication()) {
                Error = UserErrors.AuthorizationFailed;
                return this;
            }

            if (CredentialContainers == null || CredentialContainers.Count == 0) {
                Error = UserErrors.DoesNotHaveDevices;
                return this;
            }

            var specificContainer = CredentialContainers.FirstOrDefault(cc => cc.Id == endpointId);
            if (specificContainer == null) {
                Error = UserErrors.InvalidParameters; // User does not have that Credential Container
                return this;
            }

            specificContainer.IssueCredential(partNumber, _HIDService);
            //error handling:
            switch (specificContainer.Error) {
                case CredentialErrors.NoError:
                    Error = UserErrors.NoError;
                    break;
                case CredentialErrors.UnknownError:
                    Error = UserErrors.UnknownError;
                    break;
                case CredentialErrors.CredentialDeliveredAlready:
                    Error = UserErrors.NoError;
                    break;
                case CredentialErrors.AuthorizationFailed:
                    // Authentication could have expired while this method was running
                    if (_HIDService.Authenticate() == AuthenticationErrors.NoError) {
                        return IssueCredential(partNumber, endpointId);
                    }
                    Error = UserErrors.AuthorizationFailed;
                    break;
                default:
                    break;
            }

            return this;
        }

        private string GetCredentialContainerEndpointFormat {
            get { return String.Format(HIDAPIEndpoints.GetCredentialContainerEndpointFormat, _HIDService.BaseEndpoint, @"{0}"); }
        }

        private string RevokeCredentialEndpointFormat {
            get { return string.Format(HIDAPIEndpoints.RevokeCredentialEndpointFormat, _HIDService.BaseEndpoint, @"{0}"); }
        }

        /// <summary>
        /// Task HID's systems with revoking the credentials for the given part number.
        /// </summary>
        /// <param name="partNumber"></param>
        /// <returns></returns>
        /// <remarks>Passing an empty string for partNumber means we will try to have HID revoke tge credential for
        /// the default PartNumber. Contrary to the fact that is called a "default" Part Nunmber, it is something
        /// that HID's customers have to configure explicitly. If they did not, the Revoke will fail.</remarks>
        public HIDUser RevokeCredential(string partNumber = "") {
            if (!_HIDService.VerifyAuthentication()) {
                Error = UserErrors.AuthorizationFailed;
                return this;
            }

            foreach (var credentialContainer in CredentialContainers) {
                try {
                    credentialContainer.RevokeCredentials(partNumber, _HIDService);
                } catch (WebException ex) {
                    HttpWebResponse resp = (System.Net.HttpWebResponse)(ex.Response);
                    if (resp != null) {
                        if (resp.StatusCode == HttpStatusCode.Unauthorized) {
                            // Authentication could have expired while this method was running
                            if (_HIDService.Authenticate() == AuthenticationErrors.NoError) {
                                return RevokeCredential(partNumber);
                            }
                            Error = UserErrors.AuthorizationFailed;
                        } else {
                            ErrorFromStatusCode(resp.StatusCode);
                        }
                    } else {
                        Error = UserErrors.UnknownError;
                    }
                } catch (Exception ex) {
                    Error = UserErrors.UnknownError;
                    Logger.Error(ex, "Fallback error management.");
                }
            }
            return this;
        }

        private bool LocationIsValid() {
            int id;
            return !string.IsNullOrWhiteSpace(_location)
                && _location.StartsWith(_HIDService.UsersEndpoint, StringComparison.InvariantCultureIgnoreCase)
                && int.TryParse(_location.Substring(_HIDService.UsersEndpoint.Length + 1), out id)
                && id == Id;
        }

        private int IdFromLocation() {
            int id;
            if (int.TryParse(_location.Substring(_HIDService.UsersEndpoint.Length + 1), out id)) {
                return id;
            }
            return 0;
        }

        private static int IdFromLocation(string location) {
            int id;
            if (int.TryParse(location.Substring(location.LastIndexOf('/') + 1), out id)) {
                return id;
            }
            return 0;
        }
    }
}