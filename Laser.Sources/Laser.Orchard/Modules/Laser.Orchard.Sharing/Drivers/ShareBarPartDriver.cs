﻿using System;

using Orchard;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Aspects;
using Orchard.ContentManagement.Drivers;
using Orchard.Mvc;
using Orchard.Utility.Extensions;
using Laser.Orchard.Sharing.Models;
using Laser.Orchard.Sharing.Settings;
using Laser.Orchard.Sharing.ViewModels;
using Orchard.Environment.Configuration;
using System.Web;
using Orchard.Logging;

namespace Laser.Orchard.Sharing.Drivers
{
    
    public class ShareBarPartDriver : ContentPartDriver<ShareBarPart>
    {
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly IOrchardServices _services;

        public ILogger Logger { get; set; }

        public ShareBarPartDriver(IHttpContextAccessor httpContextAccessor, IOrchardServices services)
        {
            _httpContextAccessor = httpContextAccessor;
            _services = services;
        }

        protected override DriverResult Display(ShareBarPart part, string displayType, dynamic shapeHelper)
        {
            return ContentShape("Parts_Share_ShareBar", () => {
                                                                var shareSettings = _services.WorkContext.CurrentSite.As<ShareBarSettingsPart>();
                                                                var httpContext = _httpContextAccessor.Current();

                                                                string path;
                                                                ShareBarTypePartSettings typeSettings;

                                                                // Prevent share bar from showing if account is not set
                                                                if (shareSettings == null || string.IsNullOrWhiteSpace(shareSettings.AddThisAccount))
                                                                {
                                                                    return null;
                                                                }

                                                                // Prevent share bar from showing when current item is not Routable and it's not possible to retrieve the url
                                                                if (!part.Is<IAliasAspect>()) {
                                                                    try {
                                                                        path = HttpContext.Current.Request.Url.AbsoluteUri;
                                                                        if (string.IsNullOrWhiteSpace(path)) {
                                                                            return null;
                                                                        }
                                                                    } catch (Exception e) {
                                                                        Logger.Error(e.Message);
                                                                        return null;
                                                                    }

                                                                } else {

                                                                    path = part.As<IAliasAspect>().Path;
                                                                    
                                                                    var baseUrl = httpContext.Request.ToApplicationRootUrlString();

                                                                    // remove any application path from the base url
                                                                    var applicationPath = httpContext.Request.ApplicationPath ?? String.Empty;

                                                                    var urlPrefix = _services.WorkContext.Resolve<ShellSettings>().RequestUrlPrefix;

                                                                    if (path.StartsWith(applicationPath, StringComparison.OrdinalIgnoreCase)) {
                                                                        path = path.Substring(applicationPath.Length);
                                                                    }

                                                                    if (!string.IsNullOrWhiteSpace(urlPrefix))
                                                                        urlPrefix = urlPrefix + "/";
                                                                    else
                                                                        urlPrefix = "";

                                                                    baseUrl = baseUrl.TrimEnd('/');
                                                                    path = path.TrimStart('/');

                                                                    path = baseUrl + "/" + urlPrefix + path;
                                                                }

                                                                typeSettings = part.Settings.GetModel<ShareBarTypePartSettings>();

                                                                var model = new ShareBarViewModel
                                                                {
                                                                    Link = path,
                                                                    Title = _services.ContentManager.GetItemMetadata(part).DisplayText,
                                                                    Account = shareSettings.AddThisAccount,
                                                                    Mode = typeSettings.Mode
                                                                };
                                                                return shapeHelper.Parts_Share_ShareBar(ViewModel: model);
                                                            });
        }
    }
}