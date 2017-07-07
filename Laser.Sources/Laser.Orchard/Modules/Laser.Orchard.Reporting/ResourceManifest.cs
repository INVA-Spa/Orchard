﻿using Orchard.UI.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Laser.Orchard.Reporting
{
    public class ResourceManifest : IResourceManifestProvider
    {
        public void BuildManifests(ResourceManifestBuilder builder)
        {
            builder.Add().DefineScript("mustache").SetUrl("mustache.js").SetDependencies("jQuery");
            builder.Add().DefineScript("jqplot").SetUrl("jquery.jqplot.js").SetDependencies("jQueryUI");
            builder.Add().DefineScript("piejqplot").SetUrl("plugins/jqplot.pieRenderer.js").SetDependencies("jqplot");
            builder.Add().DefineScript("barjqplot").SetUrl("plugins/jqplot.barRenderer.js").SetDependencies("jqplot");
            builder.Add().DefineScript("categoryAxisjqplot").SetUrl("plugins/jqplot.categoryAxisRenderer.js").SetDependencies("jqplot");
            builder.Add().DefineScript("donutRendererjqplot").SetUrl("plugins/jqplot.donutRenderer.js").SetDependencies("jqplot");
            builder.Add().DefineScript("Reporting").SetUrl("Reporting.js").SetDependencies("jqplot").SetDependencies("JQuery");
            builder.Add().DefineScript("Chosen").SetUrl("chosen/chosen.jquery.js").SetDependencies("jQuery");
            builder.Add().DefineScript("chart.settings").SetUrl("chart.settings.js");
        }
    }
}