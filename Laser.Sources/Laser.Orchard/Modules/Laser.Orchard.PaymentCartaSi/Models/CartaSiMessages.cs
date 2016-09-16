﻿using Laser.Orchard.PaymentCartaSi.Attributes;
using Laser.Orchard.PaymentGateway.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography;
using System.Web;

namespace Laser.Orchard.PaymentCartaSi.Models {
    /// 
    /// <remarks>
    /// The name of most properties here are in Italian, because in the CartaSì service (and corresponding documentation) the 
    /// parameter names are in Italian.
    /// </remarks>
    /// 

    public abstract class CartaSiMessageBase {
        [Required]
        public string secret { get; set; } //secret key given by CartaSì
        [Required]
        [StringLength(40, MinimumLength = 40)]
        public string mac { get; set; } //message code authentication field
        [Required]
        [StringLength(30)]
        public string alias { get; set; } //shop identifier (constant given by CartaSì)

        protected string MACFromSignature(string sig) {
            byte[] sigBytes = System.Text.Encoding.UTF8.GetBytes(sig);
            SHA1 sha = new SHA1CryptoServiceProvider();
            byte[] macBytes = sha.ComputeHash(sigBytes);
            return BitConverter.ToString(macBytes).Replace("-", string.Empty);
        }
    }

    public class StartPaymentMessage : CartaSiMessageBase {
        [StringLength(7)]
        [ValidAmount]
        public string importo { get; set; } //amount expressed in euro-cents. (50€ become 5000). No decimal separator.
        [StringLength(3, MinimumLength = 3)]
        public string divisa { get; set; } //alpha3 code of the currency used for transaction. CartaSì only accepts "EUR"
        [Required]
        [StringLength(30, MinimumLength = 2)]
        [NoOctothorpe]
        public string codTrans { get; set; } //unique transaction identifier
        [Required]
        [StringLength(500)]
        [IsValidUrl]
        public string url { get; set; } //url where the client will be redirected after transaction completes
        [Required]
        [StringLength(200)]
        [IsValidUrl]
        public string url_back { get; set; } //url where the client will be redirect after an error, or after abandoning the payment
        [StringLength(150)]
        public string mail { get; set; } //buyer's email address where we want to send the payment's outcome
        [StringLength(7)]
        public string languageId { get; set; } //language identifier, from the corresponding table, for the POS
        [StringLength(500)]
        [IsValidUrl]
        public string urlpost { get; set; } //url for server-to-server transaction where the POS will send the transaction result
        [StringLength(30)]
        public string num_contratto { get; set; } //unique merchant-side identifier for the POS-side archive where credit card data is stored
        [StringLength(30)]
        public string tipo_servizio { get; set; } //for recurring payments or OneClickPay
        [StringLength(2, MinimumLength = 2)]
        public string tipo_richiesta { get; set; } //"PP" (primo pagamento), "PR" (pagamento ricorrente), "PA" (pagamento singolo)
        [StringLength(30, MinimumLength = 5)]
        public string gruppo { get; set; }
        [StringLength(2000)]
        public string descrizione { get; set; } //Description of service. It will be in the email sent to the cardholder. max 140 characters for MyBank
        [StringLength(100)]
        public string session_id { get; set; } //session identifier
        [StringLength(200)]
        public string Note1 { get; set; } //informations about the order. Reported in CartaSì back office
        [StringLength(200)]
        public string Note2 { get; set; } //informations about the order. Reported in CartaSì back office
        [StringLength(200)]
        public string Note3 { get; set; } //informations about the order. Reported in CartaSì back office
        [StringLength(4000)]
        public string AdditionalParameters { //additional custom parameters that will be reported in the outcome message. Some parameter names are reserved.
            get {
                List<string> intermediate = new List<string>();
                foreach (var item in AdditionalParametersDictionary) {
                    intermediate.Add(string.Format("{0}={1}", item.Key, item.Value));
                }
                return string.Join(@"&", intermediate);
            }
            set { //parse this into the dictionary below for validation
                //this string should be formatted as a portion of a query string
                AdditionalParametersDictionary = new Dictionary<string, string>();
                string[] elems = value.Split(new string[] { "&" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var item in elems) {
                    //each element should be in the form key=value
                    string[] pair = item.Split(new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries);
                    //pair should have two elements.
                    if (pair.Length == 1) {
                        //consider null value
                        AdditionalParametersDictionary.Add(HttpUtility.UrlEncode(pair[0]), null);
                    } else if (pair.Length == 2) {
                        //healthy case
                        AdditionalParametersDictionary.Add(HttpUtility.UrlEncode(pair[0]), HttpUtility.UrlEncode(pair[1]));
                    } else {
                        //too much stuff, so the string is probably messed up. We still add the first as key and the second as value.
                        AdditionalParametersDictionary.Add(HttpUtility.UrlEncode(pair[0]), HttpUtility.UrlEncode(pair[1]));
                    }
                }
            }
        }
        [IsValidParametersDictionary]
        public Dictionary<string, string> AdditionalParametersDictionary { get; set; }
        [StringLength(16, MinimumLength = 16)]
        public string OPTION_CF { get; set; } //fiscal code of user. Required if check between fiscal code and PAN number is active.
        [StringLength(25)]
        public string selectedcard { get; set; }
        [StringLength(20, MinimumLength = 20)]
        public string TCONTAB { get; set; } //identifies how the transaction has to be managed in terms of payments to the merchant
        [StringLength(35)]
        public string infoc { get; set; } //additional information related to the single payment
        [StringLength(20)]
        public string infob { get; set; } //additional information related to the single payment
        [StringLength(40)]
        public string modo_gestione_consegna { get; set; } //only for payments using MySi wallets

        private string TransactionStartSignature {
            get { return string.Format("codTrans={0}divisa={1}importo={2}{3}", codTrans, divisa, importo, secret); }
        }
        
        public string TransactionStartMAC {
            get {
                return MACFromSignature(TransactionStartSignature);
            }
        }

        public StartPaymentMessage() {
            AdditionalParametersDictionary = new Dictionary<string, string>();
        }
        public StartPaymentMessage(string al, string secret)
            : this() {
            alias = al;
        }
        public StartPaymentMessage(string al,  string secret, PaymentRecord pr)
            : this(al, secret) {
            importo = (pr.Amount * 100).ToString("0");
            divisa = pr.Currency;
            codTrans = pr.Id.ToString();
        }
        /// <summary>
        /// Uses the properties of the object to build a querystring for the CartaSì request
        /// </summary>
        /// <returns>The querystring. Use it as URL?querystring.</returns>
        public string MakeQueryString() {
            List<string> parameters = new List<string>();
            //Go through all the properties and append them.
            parameters.Add("alias=" + HttpUtility.UrlEncode(alias));
            if (!string.IsNullOrEmpty(importo)) {
                parameters.Add("importo=" + HttpUtility.UrlEncode(importo));
            }
            if (!string.IsNullOrEmpty(divisa)) {
                parameters.Add("divisa=" + HttpUtility.UrlEncode(divisa));
            }
            parameters.Add("codTrans=" + HttpUtility.UrlEncode(codTrans));
            parameters.Add("url=" + HttpUtility.UrlEncode(url));
            parameters.Add("url_back=" + HttpUtility.UrlEncode(url_back));
            parameters.Add("mac=" + HttpUtility.UrlEncode(mac));
            if (!string.IsNullOrEmpty(mail)) {
                parameters.Add("mail=" + HttpUtility.UrlEncode(mail));
            }
            if (!string.IsNullOrEmpty(languageId)) {
                parameters.Add("languageId=" + HttpUtility.UrlEncode(languageId));
            }
            if (!string.IsNullOrEmpty(urlpost)) {
                parameters.Add("urlpost=" + HttpUtility.UrlEncode(urlpost));
            }
            if (!string.IsNullOrEmpty(num_contratto)) {
                parameters.Add("num_contratto=" + HttpUtility.UrlEncode(num_contratto));
            }
            if (!string.IsNullOrEmpty(tipo_servizio)) {
                parameters.Add("tipo_servizio=" + HttpUtility.UrlEncode(tipo_servizio));
            }
            if (!string.IsNullOrEmpty(tipo_richiesta)) {
                parameters.Add("tipo_richiesta=" + HttpUtility.UrlEncode(tipo_richiesta));
            }
            if (!string.IsNullOrEmpty(gruppo)) {
                parameters.Add("gruppo=" + HttpUtility.UrlEncode(gruppo));
            }
            if (!string.IsNullOrEmpty(descrizione)) {
                parameters.Add("descrizione=" + HttpUtility.UrlEncode(descrizione));
            }
            if (!string.IsNullOrEmpty(session_id)) {
                parameters.Add("session_id=" + HttpUtility.UrlEncode(session_id));
            }
            if (!string.IsNullOrEmpty(Note1)) {
                parameters.Add("Note1=" + HttpUtility.UrlEncode(Note1));
            }
            if (!string.IsNullOrEmpty(Note2)) {
                parameters.Add("Note2=" + HttpUtility.UrlEncode(Note2));
            }
            if (!string.IsNullOrEmpty(Note3)) {
                parameters.Add("Note3=" + HttpUtility.UrlEncode(Note3));
            }
            if (!string.IsNullOrEmpty(AdditionalParameters)) {
                parameters.Add(AdditionalParameters);
            }
            if (!string.IsNullOrEmpty(OPTION_CF)) {
                parameters.Add("OPTION_CF=" + HttpUtility.UrlEncode(OPTION_CF));
            }
            if (!string.IsNullOrEmpty(selectedcard)) {
                parameters.Add("selectedcard=" + HttpUtility.UrlEncode(selectedcard));
            }
            if (!string.IsNullOrEmpty(TCONTAB)) {
                parameters.Add("TCONTAB=" + HttpUtility.UrlEncode(TCONTAB));
            }
            if (!string.IsNullOrEmpty(infoc)) {
                parameters.Add("infoc=" + HttpUtility.UrlEncode(infoc));
            }
            if (!string.IsNullOrEmpty(infob)) {
                parameters.Add("infob=" + HttpUtility.UrlEncode(infob));
            }
            if (!string.IsNullOrEmpty(modo_gestione_consegna)) {
                parameters.Add("modo_gestione_consegna=" + HttpUtility.UrlEncode(modo_gestione_consegna));
            }
            return string.Join(@"&", parameters);
        }
    }

    public class PaymentOutcomeMessage : CartaSiMessageBase {
        [Required]
        [StringLength(30)]
        public string alias { get; set; } //shop identifier (constant given by CartaSì)
        [Required]
        [StringLength(7)]
        [ValidAmount]
        public string importo { get; set; } //amount expressed in euro-cents. (50€ become 5000). No decimal separator.
        [Required]
        [StringLength(3, MinimumLength = 3)]
        public string divisa { get; set; } //alpha3 code of the currency used for transaction. CartaSì only accepts "EUR"
        [Required]
        [StringLength(30, MinimumLength = 2)]
        [NoOctothorpe]
        public string codTrans { get; set; } //unique transaction identifier
        [StringLength(200)]
        public string session_id { get; set; } //session identifier, taken from start message
        [Required]
        [StringLength(100)]
        public string brand { get; set; } //card used during payment. Possible values are in codetables
        [StringLength(150)]
        public string nome { get; set; } //name of buyer
        [StringLength(150)]
        public string cognome { get; set; } //surname of buyer
        [StringLength(150)]
        public string mail { get; set; } //email address of buyer
        [StringLength(30, MinimumLength = 5)]
        public string num_contratto { get; set; } //contract number from start message
        [Required]
        [StringLength(2, MinimumLength = 2)]
        public string esito { get; set; } //outcome: either "KO" or "OK"
        [Required]
        [StringLength(8, MinimumLength = 8)]
        public string data { get; set; } //transaction date "aaaammgg"
        [StringLength(3)]
        public string codiceEsito { get; set; } //number code describing transaction outcome (see codetable)
        [Required]
        [StringLength(6, MinimumLength = 6)]
        public string orario { get; set; } //time of transaction hhmmss
        [StringLength(6, MinimumLength = 2)]
        public string codAut { get; set; } //authorization code assigned by card provider
        [StringLength(100)]
        public string pan { get; set; } //masked credit card number
        [StringLength(6, MinimumLength = 6)]
        public string scadenza_pan { get; set; } //card expiration date aaaamm
        [StringLength(30)]
        public string regione { get; set; } //macro-region of used card (e.g. Europe)
        [StringLength(3, MinimumLength = 3)]
        public string nazionalita { get; set; } //ISO 3166-1 alpha-3 code of crd's country
        [StringLength(300)]
        public string messaggio { get; set; } //brief description of payment outcome
        [StringLength(28, MinimumLength = 28)]
        public string hash { get; set; } //hash of card's PAN
        [StringLength(3, MinimumLength = 3)]
        public string check { get; set; } //has values when some security check was not successful
        [StringLength(15)]
        public string codiceConvenzione { get; set; } //merchant's code assigned by the acquirer
        [StringLength(2000)]
        public string descrizione { get; set; } //description of service, copied form start message
        [StringLength(4000)]
        public string AdditionalParameters { //additional custom parameters from the start message. Some parameter names are reserved.
            get {
                List<string> intermediate = new List<string>();
                foreach (var item in AdditionalParametersDictionary) {
                    intermediate.Add(string.Format("{0}={1}", item.Key, item.Value));
                }
                return string.Join(@"&", intermediate);
            }
            set { //parse this into the dictionary below for validation
                //this string should be formatted as a portion of a query string
                AdditionalParametersDictionary = new Dictionary<string, string>();
                string[] elems = value.Split(new string[] { "&" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var item in elems) {
                    //each element should be in the form key=value
                    string[] pair = item.Split(new string[] { "=" }, StringSplitOptions.RemoveEmptyEntries);
                    //pair should have two elements.
                    if (pair.Length == 1) {
                        //consider null value
                        AdditionalParametersDictionary.Add(HttpUtility.UrlEncode(pair[0]), null);
                    } else if (pair.Length == 2) {
                        //healthy case
                        AdditionalParametersDictionary.Add(HttpUtility.UrlEncode(pair[0]), HttpUtility.UrlEncode(pair[1]));
                    } else {
                        //too much stuff, so the string is probably messed up. We still add the first as key and the second as value.
                        AdditionalParametersDictionary.Add(HttpUtility.UrlEncode(pair[0]), HttpUtility.UrlEncode(pair[1]));
                    }
                }
            } 
        }
        [IsValidParametersDictionary]
        public Dictionary<string, string> AdditionalParametersDictionary { get; set; }
        [StringLength(7)]
        public string languageId { get; set; } //language identifier, from the corresponding table, for the POS
        [StringLength(20)]
        public string TipoTransazione { get; set; } //transaction type, from the codetable
        [StringLength(30)]
        public string tipoProdotto { get; set; } //description of card type
        [StringLength(15)]
        public string dccRate { get; set; } //currency exchange rate for dcc service
        [StringLength(20, MinimumLength = 20)]
        public string dccAMount { get; set; } //amount in the exchanged dcc currency
        [StringLength(3, MinimumLength = 3)]
        public string dccCurrency { get; set; } //currency the payment was exchanged into, in numeric code from the tables
        [StringLength(2, MinimumLength = 2)]
        public string dccState { get; set; } //tells whether the transaction happend with dcc
        [StringLength(35)]
        public string infoc { get; set; } //additional information related to the single payment
        [StringLength(20)]
        public string infob { get; set; } //additional information related to the single payment
        [StringLength(8)]
        public string modo_gestione_consegna { get; set; } //only for payments using MySi wallets


        private string PaymentOutcomeSignature {
            get { return string.Format("codTrans={0}esito={1}importo={2}divisa{3}data{4}orario{%}codAut{6}{7}",
                codTrans, esito, importo, divisa, data, orario, codAut, secret); }
        }
        public string PaymentOutcomeMAC {
            get {
                return MACFromSignature(PaymentOutcomeSignature);
            }
        }


    }
}