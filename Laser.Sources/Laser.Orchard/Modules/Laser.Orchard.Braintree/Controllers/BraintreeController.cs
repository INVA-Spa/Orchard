﻿using Laser.Orchard.Braintree.Models;
using Laser.Orchard.Braintree.Services;
using Laser.Orchard.Braintree.ViewModels;
using Laser.Orchard.PaymentGateway;
using Laser.Orchard.PaymentGateway.Models;
using Newtonsoft;
using Newtonsoft.Json;
using Orchard;
using Orchard.ContentManagement;
using Orchard.Data;
using Orchard.Themes;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace Laser.Orchard.Braintree.Controllers {
    public class BraintreeController : Controller {
        private readonly IOrchardServices _orchardServices;
        private readonly BraintreePosService _posService;
        private readonly IBraintreeService _braintreeService;

        public BraintreeController(IOrchardServices orchardServices, IRepository<PaymentRecord> repository, IPaymentEventHandler paymentEventHandler, IBraintreeService braintreeService) {
            _orchardServices = orchardServices;
            _posService = new BraintreePosService(orchardServices, repository, paymentEventHandler);
            _braintreeService = braintreeService;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="pid">Payment ID</param>
        /// <returns></returns>
        [Themed]
        public ActionResult Index(int pid = 0, string guid = "") {
            PaymentRecord payment;
            if (pid > 0) {
                payment = _posService.GetPaymentInfo(pid);
            } else {
                payment = _posService.GetPaymentInfo(guid);
            }
            pid = payment.Id;
            var settings = _orchardServices.WorkContext.CurrentSite.As<BraintreeSiteSettingsPart>();
            if (settings.CurrencyCode != payment.Currency) {
                //throw new Exception(string.Format("Invalid currency code. Valid currency is {0}.", settings.CurrencyCode));
                string error = string.Format("Invalid currency code. Valid currency is {0}.", settings.CurrencyCode);
                _posService.EndPayment(payment.Id, false, error, error);
                return Redirect(_posService.GetPaymentInfoUrl(payment.Id));
            }
            PaymentVM model = new PaymentVM();
            model.Record = payment;
            model.TenantBaseUrl = Url.Action("Index").Replace("/Laser.Orchard.Braintree/Braintree", "");
            return View("Index", model);
        }

        [HttpGet]
        public ActionResult GetToken() {
            var clientToken = _braintreeService.GetClientToken();
            return Content(clientToken, "text/plain", Encoding.UTF8);
        }

        [Themed]
        [HttpPost]
        public ActionResult Pay() {
            string nonce = Request["payment_method_nonce"];
            string sPid = Request["pid"];
            int pid = int.Parse(sPid);
            decimal amount = _posService.GetPaymentInfo(pid).Amount;
            var payResult = _braintreeService.Pay(nonce, amount, null);
            string error = "";
            string transactionId = "";
            if (payResult.Success == false) {
                error = payResult.ResponseText;
            } else {
                // pagamento ok
                transactionId = payResult.TransactionId;
            }
            string info = JsonConvert.SerializeObject(payResult);
            _posService.EndPayment(pid, payResult.Success, error, info, transactionId);
            return Redirect(_posService.GetPaymentInfoUrl(pid));
        }
    }
}