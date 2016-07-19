﻿using Laser.Orchard.Vimeo.Models;
using Orchard;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Handlers;
using Orchard.Data;
using Orchard.MediaLibrary.Models;
using Orchard.MediaLibrary.Fields;
using System;
using System.Collections.Generic;
using System.Linq;
using Laser.Orchard.Vimeo.Services;
using Laser.Orchard.StartupConfig.WebApiProtection.Models;

namespace Laser.Orchard.Vimeo.Handlers {

    public class VimeoSettingsPartHandler : ContentHandler {
        private readonly IOrchardServices _orchardServices;
        private readonly IVimeoContentServices _vimeoContentServices;

        public VimeoSettingsPartHandler(IRepository<VimeoSettingsPartRecord> repository, IOrchardServices orchardServices, IVimeoContentServices vimeoContentServices) {
            Filters.Add(new ActivatingFilter<VimeoSettingsPart>("Site"));
            Filters.Add(StorageFilter.For(repository));

            _orchardServices = orchardServices;
            _vimeoContentServices = vimeoContentServices;

            //try {
            //    var protectionSettings = _orchardServices.WorkContext.CurrentSite.As<ProtectionSettingsPart>();
            //    if (protectionSettings != null) {
            //        if (!protectionSettings.ProtectedEntries.Contains("Laser.Orchard.Vimeo")) {
            //            protectionSettings.ProtectedEntries +=
            //                ",Laser.Orchard.Vimeo.VimeoUpload.TryStartUpload,Laser.Orchard.Vimeo.VimeoUpload.FinishUpload,Laser.Orchard.Vimeo.VimeoUpload.ErrorHandler";
            //        }
            //    }
            //} catch (Exception ex) {
                
            //}
            

            ////ondisplaying of a mediapart containing a vimeo video, check controller: if the request is coming from a mobile
            ////app we need to send a crypted URL, rather than the oEmbed, because we cannot whitelist apps
            //OnGetDisplayShape<OEmbedPart>((context, part) => {
            //    if (!string.IsNullOrWhiteSpace(part["provider_name"]) && part["provider_name"] == "Vimeo") {
            //        if (context.DisplayType == "Detail") {
            //            //part["html"] = "<iframe src=\"https://player.vimeo.com/video/480452\" width=\"640\" height=\"361\" frameborder=\"0\" webkitallowfullscreen mozallowfullscreen allowfullscreen></iframe>";
            //        }
            //    }
            //});
        }

        protected override void BuildDisplayShape(BuildDisplayContext context) {
            var request = _orchardServices.WorkContext.HttpContext.Request.RequestContext.RouteData;
            var area = request.Values["area"];
            var controller = request.Values["controller"];
            var action = request.Values["action"];
            string entry = "";
            if (action == null) {
                //ApiController for web apis
                entry = String.Format("{0}.{1}", area, controller);
            } else {
                //other controllers
                entry = String.Format("{0}.{1}.{2}", area, controller, action);
            }
            //the methods called from the apps serialize the content items in such a way that the only fields that is being
            //returned for an OEmbedPart is the Source string. So here we update it with the stream's url.
            if (entry.Equals("Laser.Orchard.WebServices.Json.GetByAlias", StringComparison.InvariantCultureIgnoreCase) ||
                entry.Equals("Laser.Orchard.WebServices.WebApiController", StringComparison.InvariantCultureIgnoreCase)) {
                    if (context.DisplayType == "Detail") {
                        var partsWithMediaFields = context
                            .ContentItem
                            .Parts
                            .Where(p => p.Fields.Count() > 0)
                            .Where(p => p.Fields.Any(f => f.FieldDefinition.Name == "MediaLibraryPickerField"))
                            .ToList();
                        foreach (var part in partsWithMediaFields) {
                            foreach (var field in part.Fields.Where(f => f.FieldDefinition.Name == "MediaLibraryPickerField")) {
                                foreach (var mPart in ((MediaLibraryPickerField)field).MediaParts) {
                                    var oePart = mPart.As<OEmbedPart>();
                                    if (oePart != null &&
                                        !string.IsNullOrWhiteSpace(oePart["provider_name"]) &&
                                        oePart["provider_name"] == "Vimeo") {
                                        //we recompute the video stream's url, because it may have expired
                                        oePart.Source = "Vimeo|" + _vimeoContentServices.EncryptedVideoUrl(_vimeoContentServices.ExtractVimeoStreamURL(oePart));
                                        //mPart.FileName = "";// oePart.Source;
                                    }
                                }
                            }
                        }
                    }
            }
            
            base.BuildDisplayShape(context);
        }
    }

}