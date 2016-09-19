﻿using Laser.Orchard.PaymentCartaSi.Extensions;
using Laser.Orchard.PaymentCartaSi.Models;
using Laser.Orchard.PaymentGateway;
using Laser.Orchard.PaymentGateway.Models;
using Laser.Orchard.PaymentGateway.Services;
using Orchard;
using Orchard.ContentManagement;
using Orchard.Data;
using Orchard.Localization;
using Orchard.Logging;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace Laser.Orchard.PaymentCartaSi.Services {
    public class CartaSiPosService : PosServiceBase, ICartaSiTransactionService {

        public ILogger Logger { get; set; }
        public Localizer T { get; set; }

        public CartaSiPosService(IOrchardServices orchardServices, IRepository<PaymentRecord> repository, IPaymentEventHandler paymentEventHandler) :
            base(orchardServices, repository, paymentEventHandler) {

            T = NullLocalizer.Instance;
            Logger = NullLogger.Instance;
        }

        public override string GetPosName() {
            return Constants.PosName;
        }
        public override string GetSettingsControllerName() {
            return "Admin";
        }
        /// <summary>
        /// This gets called by the "general" payment services.
        /// </summary>
        /// <param name="paymentId">The id corresponding to a <type>PaymentRecord</type> for the transaction we want to start.</param>
        /// <returns>The url corresponding to an action that will start the CartaSì transaction </returns>
        public override string GetPosUrl(int paymentId) {
            //create the url for the controller action that takes care of the redirect, passing the id as parameter
            //Controller: Transactions
            //Action; RedirectToCartaSìPage
            //Area: Laser.Orchard.PaymentCartaSi
            var hp = new UrlHelper(_orchardServices.WorkContext.HttpContext.Request.RequestContext);
            var ub = new UriBuilder(_orchardServices.WorkContext.HttpContext.Request.Url.AbsoluteUri) {
                Path = hp.Action("RedirectToCartaSìPage", "Transactions", new { Area = Constants.LocalArea, Id = paymentId })
            };
            return ub.Uri.ToString();
        }

        /// <summary>
        /// Compute the full url for an Action in a Controller in the current site.
        /// </summary>
        /// <param name="aName">The name of the action.</param>
        /// <param name="cName">The name of the controller. Defaults at "Transactions".</param>
        /// <param name="areaName">The area of the controller. Defaults at the local area for this module.</param>
        /// <returns>The full Url of the action.</returns>
        private string ActionUrl(string aName, string cName = "Transactions", string areaName = Constants.LocalArea) {
            string sName = _orchardServices.WorkContext.CurrentSite.SiteName;
            string bUrl = _orchardServices.WorkContext.CurrentSite.BaseUrl;
            var hp = new UrlHelper(_orchardServices.WorkContext.HttpContext.Request.RequestContext);
            string aPath = hp.Action(aName, cName, new { Area = areaName });
            int cut = aPath.IndexOf(sName) - 1;
                return bUrl + aPath.Substring(cut);
        }
        /// <summary>
        /// Computes the url of CartaSì's web service to which the buyer has to be redirected.
        /// </summary>
        /// <param name="paymentId">The id of the PaymentRecord for the transaction we are trying to complete.</param>
        /// <returns>The url where we should redirct the buyer.</returns>
        public string StartCartaSiTransaction(int paymentId) {
            var settings = _orchardServices.WorkContext.CurrentSite.As<PaymentCartaSiSettingsPart>();

            string pURL = settings.UseTestEnvironment ? EndPoints.TestPaymentURL : EndPoints.PaymentURL;

            StartPaymentMessage spMsg = new StartPaymentMessage(settings.CartaSiShopAlias, settings.CartaSiSecretKey, GetPaymentInfo(paymentId));
            spMsg.url = ActionUrl("CartaSiOutcome");
            spMsg.url_back = ActionUrl("CartaSiUndo");
            spMsg.urlpost = ActionUrl("CartaSiS2S");
            spMsg.mac = spMsg.TransactionStartMAC;


            try {
                Validator.ValidateObject(spMsg, new ValidationContext(spMsg), true);
            } catch (Exception ex) {
                //Log the error
                Logger.Error(T("Transaction information not valid: {0}", ex.Message).Text);
                //update the PaymentRecord for this transaction
                EndPayment(paymentId, false, null, T("Transaction information not valid: {0}", ex.Message).Text);
                //return the URL of a suitable error page (call this.GetPaymentInfoUrl after inserting the error in the PaymentRecord)
                return GetPaymentInfoUrl(paymentId);
            }

            //from the parameters, make the query string for the payment request
            string qString = "";
            try {
                qString = spMsg.MakeQueryString();
                if (string.IsNullOrWhiteSpace(qString)) {
                    throw new Exception(T("Errors while creating the query string. The query string cannot be empty.").Text);
                }
            } catch (Exception ex) {
                //Log the error
                Logger.Error(ex.Message);
                //update the PaymentRecord for this transaction
                EndPayment(paymentId, false, null, ex.Message);
                //return the URL of a suitable error page (call this.GetPaymentInfoUrl after inserting the error in the PaymentRecord)
                return GetPaymentInfoUrl(paymentId);
            }

            pURL = string.Format("{0}?{1}", pURL, qString);
            return pURL; // return null;
        }
        /// <summary>
        /// Handles errors happening on cartasì's side, including the buyers canceling the transaction.
        /// </summary>
        /// <param name="importo"></param>
        /// <param name="divisa"></param>
        /// <param name="codTrans"></param>
        /// <param name="esito"></param>
        /// <returns>An url where the buyer should be redirected.</returns>
        public string ReceiveUndo(string importo, string divisa, string codTrans, string esito) {
            int id;
            if (int.TryParse(codTrans.Replace("LASER", ""), out id)) {
                LocalizedString error;
                if (esito.ToUpperInvariant() == "ANNULLO") {
                    error = T("Transaction canceled.");
                } else if (esito.ToUpperInvariant() == "ERRORE") {
                    error = T("Formal error in the call.");
                } else {
                    error = T("Unknown error.");
                }
                EndPayment(id, false, error.Text, error.Text);
                return GetPaymentInfoUrl(id);
            } else {
                //Log the error
                LocalizedString error = T("Receved wrong information while coming back from payment: wrong Id format.");
                Logger.Error(error.Text);
                throw new Exception(error.Text);
            }
        }

        public string HandleS2STransaction(NameValueCollection qs) {
            var settings = _orchardServices.WorkContext.CurrentSite.As<PaymentCartaSiSettingsPart>();
            //this is the method where the transaction information is trustworthy
            StringBuilder sr = new StringBuilder();
            sr.AppendLine("HandleS2STransaction: START");
            foreach (var item in qs) {
                sr.AppendLine(string.Format("{0}: {1}", item.ToString(), qs[item.ToString()]));
            }
            Logger.Error(sr.ToString());
            int paymentId = 0; //assign here because compiler does not understand that we won't use this without assigning it first
            bool validMessage = !string.IsNullOrWhiteSpace(qs["codTrans"]) && int.TryParse(qs["codTrans"].Replace("LASER", ""), out paymentId); //has an id
            validMessage = validMessage && !string.IsNullOrWhiteSpace(qs["esito"]); //has a result
            validMessage = validMessage && !string.IsNullOrWhiteSpace(qs["alias"]) && qs["alias"] == settings.CartaSiShopAlias; //has right shop alias
            if (validMessage) {
                PaymentOutcomeMessage pom = new PaymentOutcomeMessage(qs);
                pom.secret = settings.CartaSiSecretKey;
                try {
                    Validator.ValidateObject(pom, new ValidationContext(pom), true);
                } catch (Exception ex) {
                    LocalizedString error = T("Transaction information not valid for transaction {0}: {1}", paymentId, ex.Message);
                    //Log the error
                    Logger.Error(error.Text);
                    throw new Exception(error.Text);
                    //We do not update the PaymentRecord here, because we have been unable to verify the hash that we received
                    
                }
                //verify the hash
                if (pom.PaymentOutcomeMAC == qs["mac"]) {
                    //transaction valid
                    //update the PaymentRecord for this transaction
                    //TODO: add to info the decoding of the pom.codiceEsito based off the codetables
                    EndPayment(paymentId, pom.esito == "OK", pom.codiceEsito, pom.messaggio);
                    Logger.Error(string.Format("Payment {0} S2S outcome {1}", paymentId.ToString(), pom.esito));
                    //return the URL of a suitable error page (call this.GetPaymentInfoUrl after inserting the error in the PaymentRecord)
                    return pom.esito;
                }
            }
            Logger.Error("HandleS2STransaction: MESSAGE NOT VALID");
            throw new Exception(string.Format("Transaction message not valid: codTrans: {0}, esito: {1}, alias: {2}", qs["codTrans"] ?? "null", qs["esito"] ?? "null", qs["alias"] ?? "null"));
        }
        /// <summary>
        /// Gets the inforation about the transaction result back from CartaSì and returns an URL showing the transaction's result
        /// </summary>
        /// <param name="qs">The query string received in the attempt by CartaSì to redirect the browser.</param>
        /// <returns>The Url for the transaction results.</returns>
        public string HandleOutcomeTransaction(NameValueCollection qs) {
            //transaction information here may not be trustworthy
            int paymentId;
            if (!string.IsNullOrWhiteSpace(qs["codTrans"]) && int.TryParse(qs["codTrans"].Replace("LASER", ""), out paymentId)) {
                PaymentOutcomeMessage pom = new PaymentOutcomeMessage(qs);
                PaymentRecord pRecord = GetPaymentInfo(paymentId);
                if (pRecord != null) {
                    return GetPaymentInfoUrl(paymentId);
                }
            }

            LocalizedString error = T("Impossible to identify transaction. There was a communication error between CartaSì and our servers");
            Logger.Error(error.Text);
            throw new Exception(error.Text);
        }
    }
}