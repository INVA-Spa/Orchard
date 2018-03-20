﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Hosting;
using System.Web.Mvc;
using Laser.Orchard.DevTools.ViewModels;
using Laser.Orchard.StartupConfig.Services;
using Newtonsoft.Json;
using Orchard;
using Orchard.Caching.Services;
using Orchard.Core.Scheduling.Models;
using Orchard.Data;
using Orchard.Environment.Configuration;
using Orchard.Localization;
using Orchard.Tasks.Scheduling;
using Orchard.UI.Admin;
using Orchard.UI.Notify;
using Laser.Orchard.StartupConfig.RazorCodeExecution.Services;
using System.Dynamic;
using NHibernate.Transform;
using Orchard.Tokens;
using Orchard.ContentManagement;
using System.Web.Security;
using Orchard.Utility.Extensions;
using Orchard.Services;
using Orchard.Security;
using Orchard.Mvc;

namespace Laser.Orchard.DevTools.Controllers {

    public class AdminController : Controller {
        private readonly ICsrfTokenHelper _csrfTokenHelper;
        private readonly IScheduledTaskManager _scheduledTaskManager;
        private readonly IRepository<ScheduledTaskRecord> _repositoryScheduledTask;
        private readonly ICacheStorageProvider _cacheStorageProvider;
        private readonly ShellSettings _shellSetting;
        private readonly IRazorTemplateManager _razorTemplateManager;
        private readonly ITokenizer _tokenizer;
        public IOrchardServices _orchardServices { get; set; }
        private readonly INotifier _notifier;
        public Localizer T { get; set; }
        private readonly IClock _clock;
        private readonly ISslSettingsProvider _sslSettingsProvider;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AdminController(ICsrfTokenHelper csrfTokenHelper,
            IScheduledTaskManager scheduledTaskManager,
             IRepository<ScheduledTaskRecord> repositoryScheduledTask,
            IOrchardServices orchardServices,
             INotifier notifier,
             ICacheStorageProvider cacheStorageProvider,
            ShellSettings shellSetting,
            IRazorTemplateManager razorTemplateManager,
            ITokenizer tokenizer,
            IClock clock,
            ISslSettingsProvider sslSettingsProvider,
            IHttpContextAccessor httpContextAccessor) {

            _tokenizer = tokenizer;
            _csrfTokenHelper = csrfTokenHelper;
            _orchardServices = orchardServices;
            _notifier = notifier;
            T = NullLocalizer.Instance;
            _scheduledTaskManager = scheduledTaskManager;
            _repositoryScheduledTask = repositoryScheduledTask;
            _cacheStorageProvider = cacheStorageProvider;
            _shellSetting = shellSetting;
            _razorTemplateManager = razorTemplateManager;
            _clock = clock;
            _sslSettingsProvider = sslSettingsProvider;
            _httpContextAccessor = httpContextAccessor;
        }


        [HttpGet]
        [Admin]
        public ActionResult Index(string testo = "") {
            Segnalazione se = new Segnalazione();
            se.Testo = testo;
            return View(se);
        }

        [HttpGet]
        [Admin]
        public ActionResult Getcsrf() {
            if (!_orchardServices.Authorizer.Authorize(Permissions.DevTools))
                return new HttpUnauthorizedResult();
            string csrfToken = "";
            var currentUser = _orchardServices.WorkContext.CurrentUser;
            if (currentUser != null) {
                var authCookie = System.Web.HttpContext.Current.Request.Cookies[".ASPXAUTH"];
                if (authCookie != null) {
                    var authToken = authCookie.Value;
                    csrfToken = _csrfTokenHelper.GenerateCsrfTokenFromAuthToken(authToken);
                    //  Segnalazione se = new Segnalazione();
                    //se.Testo = csrfToken;
                    // _notifier.Add(NotifyType.Information, T(csrfToken));
                }
            }
            //  return RedirectToAction("Index", "Admin", new { testo = csrfToken });
            Segnalazione se = new Segnalazione { Testo = "X-XSRF-TOKEN:" + csrfToken };
            return View("Index", se);
        }

        [HttpGet]
        [Admin]
        public ActionResult ShowLog() {
            if (!_orchardServices.Authorizer.Authorize(Permissions.DevTools))
                return new HttpUnauthorizedResult();
            Segnalazione se;
            if (System.IO.File.Exists(String.Format(HostingEnvironment.MapPath("~/") + "App_Data/Logs/orchard-error-" + DateTime.Now.Year.ToString() + "." + DateTime.Now.Month.ToString().PadLeft(2, '0') + "." + DateTime.Now.Day.ToString().PadLeft(2, '0') + ".log"))) {
                string textfile = System.IO.File.ReadAllText(String.Format(HostingEnvironment.MapPath("~/") + "App_Data/Logs/orchard-error-" + DateTime.Now.Year.ToString() + "." + DateTime.Now.Month.ToString().PadLeft(2, '0') + "." + DateTime.Now.Day.ToString().PadLeft(2, '0') + ".log"));
                var ultimora = textfile.Split(new string[] { DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString().PadLeft(2, '0') + "-" + DateTime.Now.Day.ToString().PadLeft(2, '0') + " " }, StringSplitOptions.RemoveEmptyEntries);
                StringBuilder sb = new StringBuilder();
                for (int a = ultimora.Length - 1; a >= 0; --a) {
                    sb.Append(ultimora[a] + Environment.NewLine + Environment.NewLine);
                }
                se = new Segnalazione { Testo = sb.ToString() };
            }
            else
                se = new Segnalazione { Testo = "Nessun file di log oggi" };
            return View("Index", se);
        }

        [HttpGet]
        [Admin]
        public ActionResult ShowLogInfo() {
            if (!_orchardServices.Authorizer.Authorize(Permissions.DevTools))
                return new HttpUnauthorizedResult();
            Segnalazione se;
            if (System.IO.File.Exists(String.Format(HostingEnvironment.MapPath("~/") + "App_Data/Logs/orchard-laser-" + DateTime.Now.Year.ToString() + "." + DateTime.Now.Month.ToString().PadLeft(2, '0') + "." + DateTime.Now.Day.ToString().PadLeft(2, '0') + ".log"))) {
                string textfile = System.IO.File.ReadAllText(String.Format(HostingEnvironment.MapPath("~/") + "App_Data/Logs/orchard-laser-" + DateTime.Now.Year.ToString() + "." + DateTime.Now.Month.ToString().PadLeft(2, '0') + "." + DateTime.Now.Day.ToString().PadLeft(2, '0') + ".log"));
                var ultimora = textfile.Split(new string[] { DateTime.Now.Year.ToString() + "-" + DateTime.Now.Month.ToString().PadLeft(2, '0') + "-" + DateTime.Now.Day.ToString().PadLeft(2, '0') + " " }, StringSplitOptions.RemoveEmptyEntries);
                StringBuilder sb = new StringBuilder();
                for (int a = ultimora.Length - 1; a >= 0; --a) {
                    sb.Append(ultimora[a] + Environment.NewLine + Environment.NewLine);
                }
                se = new Segnalazione { Testo = sb.ToString() };
            }
            else
                se = new Segnalazione { Testo = "Nessun file di log oggi" };
            return View("Index", se);
        }


        [HttpGet]
        [Admin]
        public ActionResult TestToken() {
            if (!_orchardServices.Authorizer.Authorize(Permissions.DevTools))
                return new HttpUnauthorizedResult();
            return View("TestToken");
        }

        [HttpGet]
        [Admin]
        public string TestTokenExecute(int contentItemId, string token, bool lastVersion) {
            if (!_orchardServices.Authorizer.Authorize(Permissions.DevTools))
                return "";
            try {
                ContentItem contentItem;
                if (lastVersion)
                    contentItem = _orchardServices.ContentManager.Get(contentItemId, VersionOptions.Latest);
                else
                    contentItem = _orchardServices.ContentManager.Get(contentItemId);
                var tokens = new Dictionary<string, object> { { "Content", contentItem } };
                return _tokenizer.Replace(token, tokens);
            }
            catch (Exception ex) {
                return "Error " + ex.Message;
            }
        }

        [HttpGet]
        [Admin]
        public ActionResult ShowScheduledTask() {
            if (!_orchardServices.Authorizer.Authorize(Permissions.DevTools))
                return new HttpUnauthorizedResult();
            IEnumerable<ScheduledTaskRecord> st = _repositoryScheduledTask.Fetch(t => true).OrderBy(x => x.ScheduledUtc);

            return View("ShowScheduledTask", (object)st);
        }

        [HttpGet]
        [Admin]
        public ActionResult DeleteScheduledTask(Int32 key) {
            if (!_orchardServices.Authorizer.Authorize(Permissions.DevTools))
                return new HttpUnauthorizedResult();
            ScheduledTaskRecord st = _repositoryScheduledTask.Fetch(t => true).Where(x => x.Id == key).FirstOrDefault();
            _repositoryScheduledTask.Delete(st);
            //  st.ContentItemVersionRecord.ContentItemRecord.Id;
            return RedirectToAction("ShowScheduledTask", "Admin");
        }

        [HttpGet]
        [Admin]
        public ActionResult ShowCachedData() {
            if (!_orchardServices.Authorizer.Authorize(Permissions.DevTools))
                return new HttpUnauthorizedResult();
            List<string> all = HttpRuntime.Cache
            .AsParallel()
            .Cast<DictionaryEntry>()
            .Select(x => x.Key.ToString())
            .Where(x => x.StartsWith(_shellSetting.Name + "_"))
            .ToList();
            return View((object)all);
        }

        [HttpGet]
        [Admin]
        public ActionResult ShowCachedDataClear(string key) {
            if (!_orchardServices.Authorizer.Authorize(Permissions.DevTools))
                return new HttpUnauthorizedResult();
            if (string.IsNullOrEmpty(key)) {
                List<string> all = HttpRuntime.Cache
                   .AsParallel()
                   .Cast<DictionaryEntry>()
                   .Select(x => x.Key.ToString())
                   .Where(x => x.StartsWith(_shellSetting.Name + "_"))
                   .ToList();
                foreach (string thekey in all) {
                    _cacheStorageProvider.Remove(thekey);
                }
            }
            else
                _cacheStorageProvider.Remove(key);
            return RedirectToAction("ShowCachedData", "Admin");
        }

        [HttpGet]
        [Admin]
        public ActionResult ShowCachedDataClearFileSystem(string key) {
            if (!_orchardServices.Authorizer.Authorize(Permissions.DevTools))
                return new HttpUnauthorizedResult();
            if (string.IsNullOrEmpty(key)) {
                string[] all = Directory.GetFiles(String.Format(HostingEnvironment.MapPath("~/") + "App_Data/Cache/", _shellSetting.Name + "_*"));
                foreach (string thekey in all) {
                    System.IO.File.Delete(thekey);
                }
            }
            else
                System.IO.File.Delete(String.Format(HostingEnvironment.MapPath("~/") + "App_Data/Cache/" + key));
            return RedirectToAction("ShowCachedData", "Admin");
        }

        [HttpGet]
        [Admin]
        public ActionResult ShowCachedDataEdit(string key) {
            if (!_orchardServices.Authorizer.Authorize(Permissions.DevTools))
                return new HttpUnauthorizedResult();
            //  _notifier.Add(NotifyType.Information, T());
            //  return RedirectToAction("index", "Admin", new { testo = JsonConvert.SerializeObject(_cacheStorageProvider.Get(key)) });
            Segnalazione se = new Segnalazione { Testo = JsonConvert.SerializeObject(_cacheStorageProvider.Get<object>(key)) };
            return View("index", se);
        }

        [HttpGet]
        [Admin]
        public ActionResult CleanRazor() {
            if (!_orchardServices.Authorizer.Authorize(Permissions.DevTools))
                return new HttpUnauthorizedResult();
            _razorTemplateManager.StartNewRazorEngine();
            //  _notifier.Add(NotifyType.Information, T());
            //  return RedirectToAction("index", "Admin", new { testo = JsonConvert.SerializeObject(_cacheStorageProvider.Get(key)) });
            Segnalazione se = new Segnalazione { Testo = "Razor resettati" };
            return View("index", se);
        }

        [HttpGet]
        [Admin]
        public ActionResult ShowRazor() {
            if (!_orchardServices.Authorizer.Authorize(Permissions.DevTools))
                return new HttpUnauthorizedResult();
            dynamic module = new ExpandoObject();
            module.RazorList = _razorTemplateManager.GetListCached();
            module.RazorOldList = _razorTemplateManager.GetListOldCached();
            return View(module);
        }

        [HttpGet]
        [Admin]
        public ActionResult GetValidApiKey() {
            Segnalazione se = null;
            IApiKeyService apiKeyService = null;
            if (_orchardServices.WorkContext.TryResolve<IApiKeyService>(out apiKeyService)) {
                var iv = GetRandomIV();
                var token = apiKeyService.GetValidApiKey(iv);
                var tokenTS = apiKeyService.GetValidApiKey(iv, true);
                se = new Segnalazione { Testo = string.Format("ApiKey: {0} \r\nApiKey: {1} \r\nAKIV: {2}", token, tokenTS, iv) };
            }
            else {
                se = new Segnalazione { Testo = "Feature Laser.Orchard.StartupConfig.WebApiProtection not active." };
            }
            return View("index", se);
        }

        private string GetRandomIV() {
            string iv = string.Format("{0}{0}", DateTime.UtcNow.ToString("ddMMyyyy").Substring(0, 8));
            byte[] arr = Encoding.UTF8.GetBytes(iv);
            return Convert.ToBase64String(arr);
        }
        [HttpGet]
        [Admin]
        public ActionResult ShowCustomHqlQuery() {
            if (!_orchardServices.Authorizer.Authorize(Permissions.DevTools))
                return new HttpUnauthorizedResult();
            return View("CustomHqlQuery", new CustomHqlQuery());
    }
        [HttpPost]
        [Admin]
        public ActionResult ExecHqlQuery(CustomHqlQuery model) {
            if (!_orchardServices.Authorizer.Authorize(Permissions.DevTools))
                return new HttpUnauthorizedResult();

            string query = model.HqlQuery.Trim();
            var hql = _orchardServices.TransactionManager.GetSession().CreateQuery(query);
            if (query.StartsWith("select ", StringComparison.InvariantCultureIgnoreCase)) {
                model.Results = hql.SetResultTransformer(Transformers.AliasToEntityMap).Enumerable();
            } else {
                var result = new List<Hashtable>();
                var aux1 = hql.List();
                foreach(var obj in aux1) {
                    var h = new Hashtable();
                    foreach(var field in obj.GetType().GetProperties()) {
                        h.Add(field.Name, field.GetValue(obj));
                    }
                    result.Add(h);
                }
                model.Results = result;
            }
            model.Aliases = hql.ReturnAliases;
            //model.Results = hql.SetResultTransformer(Transformers.AliasToEntityMap)
            //    .List() as IList<object>;
            return View("CustomHqlQuery", model);
        }

        [HttpGet]
        [Admin]
        public ActionResult GetV3AuthCookieInfo() {
            if (!_orchardServices.Authorizer.Authorize(Permissions.DevTools))
                return new HttpUnauthorizedResult();

            var now = _clock.UtcNow.ToLocalTime();
            var currentUser = _orchardServices.WorkContext.CurrentUser;
            var userData = string.Concat(currentUser.UserName.ToBase64(), ";", _shellSetting.Name);

            var ticket = new FormsAuthenticationTicket(
                3,
                currentUser.UserName,
                now,
                now.Add(TimeSpan.FromDays(1)),
                false,
                userData,
                FormsAuthentication.FormsCookiePath);

            var encryptedTicket = FormsAuthentication.Encrypt(ticket);

            var cookie = new HttpCookie(FormsAuthentication.FormsCookieName, encryptedTicket) {
                HttpOnly = true,
                Secure = _sslSettingsProvider.GetRequiresSSL(),
                Path = FormsAuthentication.FormsCookiePath
            };

            var httpContext = _httpContextAccessor.Current();

            if (!String.IsNullOrEmpty(_shellSetting.RequestUrlPrefix)) {
                cookie.Path = GetCookiePath(httpContext);
            }

            if (FormsAuthentication.CookieDomain != null) {
                cookie.Domain = FormsAuthentication.CookieDomain;
            }

            var sb = new StringBuilder();
            sb.AppendLine("New V3 authentication cookie (valid for 24 hours)");
            sb.AppendLine("Cookie Name: " + cookie.Name);
            sb.AppendLine("Cookie Path: " + cookie.Path);
            sb.AppendLine("Cookie Value: " + cookie.Value);

            // We also add the csrf token that we are going to use to validate calls from mobile
            var csrfToken = _csrfTokenHelper.GenerateCsrfTokenFromAuthToken(cookie.Value);
            sb.AppendLine("CSRF Token: " + csrfToken);

            // prepare a string to paste in a proxy for testing (e.g. in postman)
            sb.AppendLine(""); //empty line separator
            sb.AppendLine("Example of headers for a proxy (e.g. just paste the next few lines as headers in Postman):");
            sb.AppendLine("Cookie: " + cookie.Name + "=" + cookie.Value + "; path=" + cookie.Path + ";");
            sb.AppendLine("X-XSRF-TOKEN:" + csrfToken);

            var se = new Segnalazione { Testo = sb.ToString() };
            return View("Index", se);
        }
        private string GetCookiePath(HttpContextBase httpContext) {
            var cookiePath = httpContext.Request.ApplicationPath;
            if (cookiePath != null && cookiePath.Length > 1) {
                cookiePath += '/';
            }

            cookiePath += _shellSetting.RequestUrlPrefix;

            return cookiePath;
        }
    }
}