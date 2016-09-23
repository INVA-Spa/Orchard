﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Laser.Orchard.PaymentGateway.Models {
    public class PaymentRecord {
        public virtual int Id { get; set; }
        public virtual string Reason { get; set; }
        public virtual string PosName { get; set; }
        public virtual DateTime CreationDate { get; set; }
        public virtual DateTime UpdateDate { get; set; }
        public virtual string PosUrl { get; set; }
        public virtual decimal Amount { get; set; }
        public virtual string Currency { get; set; }
        public virtual bool Success { get; set; }
        public virtual string Error { get; set; }
        public virtual string TransactionId { get; set; }
        public virtual string Info { get; set; }
        public virtual int ContentItemId { get; set; }
        public virtual int UserId { get; set; }
        //update 2016/09/22: add new properties to store custom redirect information
        public virtual string CustomRedirectUrl { get; set; }
        public virtual string CustomRedirectSchema { get; set; }
        public virtual bool PaymentTransactionComplete { get; set; }
    }
}