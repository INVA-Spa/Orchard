﻿using Orchard.ContentManagement;
using Orchard.ContentManagement.Records;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Laser.Orchard.PaymentGateway.Models {
    public class PayButtonRecord : ContentPartRecord {
    }
    public class PayButtonPart : ContentPart<PayButtonRecord> {
    }
}