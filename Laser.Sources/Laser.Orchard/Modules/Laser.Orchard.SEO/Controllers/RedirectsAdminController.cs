﻿using Laser.Orchard.SEO.Models;
using Laser.Orchard.SEO.Services;
using Orchard;
using Orchard.DisplayManagement;
using Orchard.Environment.Extensions;
using Orchard.Localization;
using Orchard.Settings;
using Orchard.UI.Admin;
using Orchard.UI.Navigation;
using Orchard.UI.Notify;
using System;
using System.Web.Mvc;

namespace Laser.Orchard.SEO.Controllers {
    [Admin]
    [OrchardFeature("Laser.Orchard.Redirects")]
    public class RedirectsAdminController : Controller {
        private readonly IRedirectService _redirectService;
        private readonly IOrchardServices _orchardServices;
        private readonly ISiteService _siteService;

        private readonly string[] _includeProperties = { "SourceUrl", "DestinationUrl", "IsPermanent" };

        private dynamic Shape { get; set; }

        public RedirectsAdminController(
            IRedirectService redirectService,
            IOrchardServices orchardServices,
            ISiteService siteService,
            IShapeFactory shapeFactory) {

            _redirectService = redirectService;
            _orchardServices = orchardServices;
            _siteService = siteService;
            Shape = shapeFactory;

            T = NullLocalizer.Instance;
        }

        public Localizer T { get; set; }

        [HttpGet]
        public ActionResult Index(PagerParameters pagerParameters) {
            var pager = new Pager(_siteService.GetSiteSettings(), pagerParameters);
            var pagerShape = _orchardServices.New.Pager(pager).TotalItemCount(_redirectService.GetRedirectsTotalCount());
            var items = _redirectService.GetRedirects(pager.GetStartIndex(), pager.PageSize);

            dynamic viewModel = Shape.ViewModel()
                .Redirects(items)
                .Pager(pagerShape);
            return View((object)viewModel);
        }

        [HttpGet]
        public ActionResult Add() {
            return View();
        }

        [HttpPost, ActionName("Add")]
        public ActionResult AddPost() {
            var redirect = new RedirectRule();

            //thanks to the _includeProperties, Update succeeds even if we are not adding the id from UI
            if (!TryUpdateModel(redirect, _includeProperties)) {
                _orchardServices.TransactionManager.Cancel();
                return View(redirect);
            }

            return RedirectIfUrlsAreSame(redirect, (red) => _redirectService.Add(red));
        }

        [HttpGet]
        public ActionResult Edit(int id) {
            var redirect = _redirectService.GetRedirect(id);
            if (redirect == null)
                return HttpNotFound();

            return View(redirect);
        }

        [HttpPost, ActionName("Edit")]
        public ActionResult EditPost(int id) {
            var redirect = _redirectService.GetRedirect(id);
            if (redirect == null)
                return HttpNotFound();

            if (!TryUpdateModel(redirect, _includeProperties)) {
                _orchardServices.TransactionManager.Cancel();
                return View(redirect);
            }

            return RedirectIfUrlsAreSame(redirect, (red) => _redirectService.Update(red));
        }
        
        private ActionResult RedirectIfUrlsAreSame(RedirectRule redirect, Action<RedirectRule> doOnSuccess) {
            if (redirect.SourceUrl == redirect.DestinationUrl) {
                ModelState.AddModelError("SourceUrl", T("Source url is equal to Destination url").Text);
                _orchardServices.TransactionManager.Cancel();
                return View(redirect);
            }

            doOnSuccess(redirect);

            return RedirectToAction("Index");
        }

        [HttpPost]
        public ActionResult Delete(int id) {
            var redirect = _redirectService.GetRedirect(id);
            if (redirect == null)
                return HttpNotFound();

            _redirectService.Delete(redirect);

            _orchardServices.Notifier.Information(T("Redirect record was deleted"));

            return RedirectToAction("Index");
        }
    }
}