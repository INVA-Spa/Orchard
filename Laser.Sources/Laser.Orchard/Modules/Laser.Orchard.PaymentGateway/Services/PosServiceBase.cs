﻿using Laser.Orchard.PaymentGateway.Models;
using Orchard;
using Orchard.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Laser.Orchard.PaymentGateway.Services {
    public abstract class PosServiceBase : IPosService {
        protected readonly IOrchardServices _orchardServices;
        private readonly IRepository<PaymentRecord> _repository;
        private readonly IPaymentEventHandler _paymentEventHandler;

        public abstract string GetPosName();
        /// <summary>
        /// Restituisce il nome del controller utilizzato per la gestione dei settings del POS che deve ereditare da PosAdminBaseController.
        /// Il nome non deve avere il suffisso "Controller" (es. "Admin", non "AdminController").
        /// Restiuisce null o stringa vuota se non è necessario un controller per i settings.
        /// </summary>
        /// <returns></returns>
        public abstract string GetSettingsControllerName();

        /// <summary>
        /// Get the return URL passed to the virtual POS.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public abstract string GetPosUrl(int paymentId);

        public PosServiceBase(IOrchardServices orchardServices, IRepository<PaymentRecord> repository, IPaymentEventHandler paymentEventHandler) {
            _orchardServices = orchardServices;
            _repository = repository;
            _paymentEventHandler = paymentEventHandler;
        }
        public PaymentRecord StartPayment(PaymentRecord values) {
            // verifica che siano presenti i valori necessari
            if ((values.Amount <= 0)
                || string.IsNullOrWhiteSpace(values.Currency)) {
                throw new Exception("Parameters missing. Required parameters: Amount, Currency.");
            }
            values.PosName = GetPosName();
            if (string.IsNullOrWhiteSpace(values.PosUrl)) {
                string posUrl = GetPosUrl(values.Id);
                values.PosUrl = posUrl;
            }
            int paymentId = SavePaymentInfo(values);
            values.Id = paymentId;
            return values;
        }
        public PaymentRecord GetPaymentInfo(int paymentId) {
            // verifica che siano presenti i parametri necessari
            if (paymentId <= 0) {
                throw new Exception("Invalid parameter 'Id'.");
            }
            PaymentRecord result = _repository.Get(paymentId);
            return result;
        }
        public void EndPayment(int paymentId, bool success, string error, string info, string transactionId = "") {
            PaymentRecord payment = GetPaymentInfo(paymentId);
            payment.Success = success;
            payment.Error = error;
            payment.Info = info;
            payment.TransactionId = transactionId;
            payment.PosName = GetPosName(); // forza la valorizzazione del PosName
            payment.PosUrl = GetPosUrl(paymentId);
            SavePaymentInfo(payment);
            // solleva l'evento di termine della transazione
            if (success) {
                _paymentEventHandler.OnSuccess(payment.Id, payment.ContentItemId);
            }
            else {
                _paymentEventHandler.OnError(payment.Id, payment.ContentItemId);
            }
        }
        /// <summary>
        /// Fornisce l'URL per consultare l'esito del pagamento
        /// </summary>
        /// <param name="paymentId"></param>
        /// <returns></returns>
        public string GetPaymentInfoUrl(int paymentId) {
            return new UrlHelper(HttpContext.Current.Request.RequestContext).Action("Info", "Payment", new { area = "Laser.Orchard.PaymentGateway", paymentId = paymentId });
        }
        /// <summary>
        /// Salva il pagamento e restituisce il PaymentId.
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        private int SavePaymentInfo(PaymentRecord values) {
            PaymentRecord record = null;
            DateTime now = DateTime.Now;
            if (values.Id > 0) {
                record = _repository.Get(values.Id);
            }
            values.PosName = GetValidString(values.PosName, 255);
            values.Reason = GetValidString(values.Reason, 255);
            values.Error = GetValidString(values.Error, 255);
            values.TransactionId = GetValidString(values.TransactionId, 255);
            // 4000 è la massima lunghezza di stringa che nhibernate riesce a gestire
            values.PosUrl = GetValidString(values.PosUrl, 4000);
            values.Info = GetValidString(values.Info, 4000);
            if (record == null) {
                values.CreationDate = now;
                values.UpdateDate = now;
                _repository.Create(values);
            }
            else {
                values.UpdateDate = now;
                _repository.Update(values);
            }
            return values.Id;
        }
        private string GetValidString(string text, int maxLength) {
            string result = text;
            if ((result != null) && (result.Length > maxLength)) {
                result = result.Substring(0, maxLength);
            }
            return result;
        }
    }
}