﻿using Laser.Orchard.PaymentGateway.Controllers;
using Laser.Orchard.PaymentGestPay.Models;
using Laser.Orchard.PaymentGestPay.Services;
using Laser.Orchard.PaymentGestPay.ViewModels;
using Orchard;
using Orchard.ContentManagement;
using Orchard.Localization;
using Orchard.UI.Notify;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using OMvc = Orchard.Mvc;

namespace Laser.Orchard.PaymentGestPay.Controllers {
    //public class AdminController : Controller {

    //    private readonly IOrchardServices _orchardServices;
    //    private IGestPayAdminServices _gestPayAdminServices;
    //    public Localizer T { get; set; }

    //    public AdminController(IOrchardServices orchardServices, IGestPayAdminServices gestPayAdminServices) {
    //        _orchardServices = orchardServices;
    //        _gestPayAdminServices = gestPayAdminServices;

    //        T = NullLocalizer.Instance;
    //    }

    //    public ActionResult Index() {
    //        //TODO: add permission verification


    //        return View(_gestPayAdminServices.GetSettingsVM());
    //    }

    //    [HttpPost, ActionName("Index")]
    //    [OMvc.FormValueRequired("submit.SaveSettings")]
    //    public ActionResult IndexSaveSettings(GestPaySettingsViewModel vm) {
    //        _gestPayAdminServices.UpdateSettings(vm);
    //        return RedirectToAction("Index");
    //    }
    //}

    public class AdminController : PosAdminBaseController {
        private IGestPayAdminServices _gestPayAdminServices;

        public AdminController(IOrchardServices orchardServices, IGestPayAdminServices gestPayAdminServices) : 
            base(orchardServices) {

            _gestPayAdminServices = gestPayAdminServices;
        }

        protected override dynamic GetSettingsPart() {
            return _orchardServices
                .WorkContext
                .CurrentSite
                .As<PaymentGestPaySettingsPart>(); // _gestPayAdminServices.GetSettingsVM();
        }
    }
}