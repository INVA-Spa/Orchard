﻿using Laser.Orchard.Mobile.ViewModels;
using Laser.Orchard.StartupConfig.IdentityProvider;
using Laser.Orchard.StartupConfig.Services;
using Laser.Orchard.UsersExtensions.Models;
using Laser.Orchard.UsersExtensions.Services;
using Orchard;
using Orchard.Security;
using Orchard.Users.Services;
using System.Collections.Generic;
using System.Web.Mvc;

namespace Laser.Orchard.UsersExtensions.Controllers {

    [OutputCache(NoStore = true, Duration = 0)]
    [RoutePrefix("UserActions")]
    public class UserActionsController : BaseUserActionsController {

        public UserActionsController(
            IOrchardServices orchardServices,
            IUsersExtensionsServices usersExtensionsServices,
            IUserService userService,
            IUtilsServices utilsServices,
            IEnumerable<IIdentityProvider> identityProviders) : base(
                 orchardServices,
                 utilsServices,
                 usersExtensionsServices,
                 identityProviders,
                 userService
                 ) { }


        #region [https calls]

        /// <summary>
        /// </summary>
        /// <param name="userRegistrationParams">
        /// </param>
        /// <returns></returns>
        [HttpPost]
        [AlwaysAccessible]
        public ContentResult RegisterSsl(UserRegistration userRegistrationParams) {
            return RegisterLogic(userRegistrationParams);
        }

        [HttpPost]
        [AlwaysAccessible]
        public ContentResult SignInSsl(UserLogin login) {
            return SignInLogic(login);
        }

        [HttpPost]
        public JsonResult SignOutSsl() {
            return SignOutLogic();
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="phoneNumber">
        ///
        /// {
        ///     "phoneNumber":{
        ///         "internationalPrefix":"39",
        ///         "phoneNumber":"3477543903"
        ///     }
        /// }
        /// </param>
        /// <returns></returns>
        [HttpPost]
        [AlwaysAccessible]
        public JsonResult RequestLostPasswordSmsSsl(PhoneNumberViewModel phoneNumber) {
            return RequestLostPasswordLogic(phoneNumber.PhoneNumber, LostPasswordUserOptions.Phone, phoneNumber.InternationalPrefix);
        }

        /// <summary>
        ///
        /// </summary>
        /// <param name="username">
        ///  {
        ///      "username":"h.sbicego"
        ///  }
        /// </param>
        /// <returns></returns>
        [HttpPost]
        [AlwaysAccessible]
        public JsonResult RequestLostPasswordAccountOrEmailSsl(string username) {
            return RequestLostPasswordLogic(username, LostPasswordUserOptions.Account);
        }

        [HttpGet]
        [AlwaysAccessible]
        public ContentResult GetCleanRegistrationPoliciesSsl(string lang = null) {
            return GetCleanRegistrationPoliciesLogic(lang);
        }

        [HttpGet]
        [AlwaysAccessible]
        public ContentResult GetRegistrationPoliciesSsl(string mfilter = "", int page = 1, int pageSize = 10, bool tinyResponse = true, bool minified = false, bool realformat = false, int deeplevel = 10, string lang = null) {
            return GetRegistrationPoliciesLogic(mfilter, page, pageSize, tinyResponse, minified, realformat, deeplevel, lang);
        }

        [HttpGet]
        [AlwaysAccessible]
        public JsonResult GetUserRegistrationModelSsl() {
            return GetUserRegistrationModelLogic();
        }

        #endregion [https calls]

        #region [http calls]

        [HttpPost]
        public ContentResult Register(UserRegistration userRegistrationParams) {
            return RegisterLogic(userRegistrationParams);
        }

        [HttpPost]
        public ContentResult SignIn(UserLogin login) {
            return SignInLogic(login);
        }

        [HttpPost]
        public JsonResult SignOut() {
            return SignOutLogic();
        }

        [HttpPost]
        public JsonResult RequestLostPasswordSms(PhoneNumberViewModel phoneNumber) {
            return RequestLostPasswordLogic(phoneNumber.PhoneNumber, LostPasswordUserOptions.Phone, phoneNumber.InternationalPrefix);
        }

        [HttpPost]
        public JsonResult RequestLostPasswordAccountOrEmail(string username) {
            return RequestLostPasswordLogic(username, LostPasswordUserOptions.Account);
        }

        [HttpGet]
        public ContentResult GetCleanRegistrationPolicies(string lang = null) {
            return GetCleanRegistrationPoliciesLogic(lang);
        }

        [HttpGet]
        public ContentResult GetRegistrationPolicies(string mfilter = "", int page = 1, int pageSize = 10, bool tinyResponse = true, bool minified = false, bool realformat = false, int deeplevel = 10, string lang = null) {
            return GetRegistrationPoliciesLogic(mfilter, page, pageSize, tinyResponse, minified, realformat, deeplevel, lang);
        }

        [HttpGet]
        public JsonResult GetUserRegistrationModel() {
            return GetUserRegistrationModelLogic();
        }

        #endregion [http calls]
        
    }
}