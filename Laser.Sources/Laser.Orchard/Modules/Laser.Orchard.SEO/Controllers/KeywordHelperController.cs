﻿using Laser.Orchard.SEO.ViewModels;
using Orchard.Environment.Extensions;
using Orchard.UI.Admin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;

namespace Laser.Orchard.SEO.Controllers {
    [OrchardFeature("Laser.Orchard.KeywordHelper")]
    public class KeywordHelperController : Controller {

        [Admin]
        public ActionResult RefreshTrends(string _hl, string _q, string _geo, string _date) {
            var model = new GoogleTrendsViewModel {
                hl = _hl,
                q = _q,
                geo = _geo,
                date = _date
            };
            return PartialView((object)model);
        }

        [Admin]
        public ActionResult SummaryTrends(string _hl, string _q, string _geo, string _date) {
            var model = new GoogleTrendsViewModel {
                hl = _hl,
                q = _q,
                geo = _geo,
                date = _date
            };
            return PartialView((object)model);
        }
    }
}