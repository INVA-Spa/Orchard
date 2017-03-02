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

namespace Laser.Orchard.DevTools.Controllers {

    public class AdminController : Controller {
        private readonly ICsrfTokenHelper _csrfTokenHelper;
        private readonly IScheduledTaskManager _scheduledTaskManager;
        private readonly IRepository<ScheduledTaskRecord> _repositoryScheduledTask;
        private readonly ICacheStorageProvider _cacheStorageProvider;
        private readonly ShellSettings _shellSetting;
        public IOrchardServices _orchardServices { get; set; }
        private readonly INotifier _notifier;
        public Localizer T { get; set; }

        public AdminController(ICsrfTokenHelper csrfTokenHelper,
            IScheduledTaskManager scheduledTaskManager,
             IRepository<ScheduledTaskRecord> repositoryScheduledTask,
            IOrchardServices orchardServices,
             INotifier notifier,
             ICacheStorageProvider cacheStorageProvider,
            ShellSettings shellSetting) {
            _csrfTokenHelper = csrfTokenHelper;
            _orchardServices = orchardServices;
            _notifier = notifier;
            T = NullLocalizer.Instance;
            _scheduledTaskManager = scheduledTaskManager;
            _repositoryScheduledTask = repositoryScheduledTask;
            _cacheStorageProvider = cacheStorageProvider;
            _shellSetting = shellSetting;
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
            Segnalazione se = new Segnalazione { Testo = csrfToken };
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
    }
}