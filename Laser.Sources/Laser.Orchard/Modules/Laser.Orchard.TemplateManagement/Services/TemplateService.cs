﻿using System;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using Laser.Orchard.TemplateManagement.Models;
using Orchard;
using Orchard.ContentManagement;
using Orchard.Email.Models;
using Orchard.Environment.Extensions;
using Laser.Orchard.StartupConfig.Extensions;
using Laser.Orchard.Commons.Extensions;
using Orchard.Messaging.Services;
using Orchard.Email.Services;
using Orchard.JobsQueue.Services;
using RazorEngine.Templating;
using Newtonsoft.Json;
using Orchard.UI.Notify;
using Orchard.Environment.Configuration;
using Laser.Orchard.TemplateManagement.ViewModels;


namespace Laser.Orchard.TemplateManagement.Services {
    public interface ITemplateService : IDependency {
        IEnumerable<TemplatePart> GetLayouts();
        IEnumerable<TemplatePart> GetTemplates();
        IEnumerable<TemplatePart> GetTemplatesWithLayout(int LayoutIdSelected);
        TemplatePart GetTemplate(int id);
        string ParseTemplate(TemplatePart template, ParseTemplateContext context);
        IEnumerable<IParserEngine> GetParsers();
        IParserEngine GetParser(string id);
        IParserEngine SelectParser(TemplatePart template);
        bool SendTemplatedEmail(dynamic contentModel, int templateId, IEnumerable<string> sendTo, IEnumerable<string> bcc, object viewBag = null, bool queued = true, List<TemplatePlaceHolderViewModel> listaPH = null);
        string RitornaParsingTemplate(dynamic contentModel, int templateId, object viewBag = null);
   }

    [OrchardFeature("Laser.Orchard.TemplateManagement")]
    public class TemplateService : Component, ITemplateService {
        private readonly IContentManager _contentManager;
        private readonly IEnumerable<IParserEngine> _parsers;
        private readonly IOrchardServices _services;
        private readonly IMessageService _messageService;
        private readonly IJobsQueueService _jobsQueueService;
        private readonly INotifier _notifier;
        private readonly ShellSettings _shellSettings;

        public TemplateService(INotifier notifier, IEnumerable<IParserEngine> parsers, IOrchardServices services, IMessageService messageService, IJobsQueueService jobsQueueService, ShellSettings shellSettings) {
            _contentManager = services.ContentManager;
            _parsers = parsers;
            _services = services;
            _messageService = messageService;
            _jobsQueueService = jobsQueueService;
            _notifier = notifier;
            _shellSettings = shellSettings;
        }

        public IEnumerable<TemplatePart> GetLayouts() {
            return _contentManager.Query<TemplatePart, TemplatePartRecord>().Where(x => x.IsLayout).List();
        }

        public IEnumerable<TemplatePart> GetTemplates() {
            return _contentManager.Query<TemplatePart, TemplatePartRecord>().Where(x => !x.IsLayout).List();
        }

        public IEnumerable<TemplatePart> GetTemplatesWithLayout(int LayoutIdSelected) {
            return _contentManager.Query<TemplatePart, TemplatePartRecord>().Where(x => x.LayoutIdSelected == LayoutIdSelected).List();
        }

        public TemplatePart GetTemplate(int id) {
            return _contentManager.Get<TemplatePart>(id);
        }

        public string ParseTemplate(TemplatePart template, ParseTemplateContext context) {
            var parser = SelectParser(template);
            return parser.ParseTemplate(template, context);
        }

        public IParserEngine GetParser(string id) {
            return _parsers.SingleOrDefault(x => x.Id == id);
        }

        public IParserEngine SelectParser(TemplatePart template) {
            var parserId = template.DefaultParserIdSelected;
            IParserEngine parser = null;

            if (!string.IsNullOrWhiteSpace(parserId)) {
                parser = GetParser(parserId);
            }

            if (parser == null) {
                parserId = _services.WorkContext.CurrentSite.As<SiteSettingsPart>().DefaultParserIdSelected;
                parser = GetParser(parserId);
            }

            return parser ?? _parsers.First();
        }

        public IEnumerable<IParserEngine> GetParsers() {
            return _parsers;
        }

        public bool SendTemplatedEmail(dynamic contentModel, int templateId, IEnumerable<string> sendTo, IEnumerable<string> bcc, object viewBag=null, bool queued = true, List<TemplatePlaceHolderViewModel> listaPH = null) {
            var template = GetTemplate(templateId);
            string body = RitornaParsingTemplate(contentModel, templateId, viewBag);

            if (body.StartsWith("Error On Template")) {
                _notifier.Add(NotifyType.Error,T("Error on template, mail not sent"));
                return false;
            }

            // Place Holder - es. [UNSUBSCRIBE]
            if (listaPH != null) {
                foreach (TemplatePlaceHolderViewModel ph in listaPH) {

                    if (body.Contains(ph.Name)) {
                        body = body.Replace(ph.Name, ph.Value);
                    } else {
                        if (ph.ShowForce)
                            body += "<br /><br />" + ph.Value;
                    }
                }
            }

            var data = new Dictionary<string, object>();
            var smtp = _services.WorkContext.CurrentSite.As<SmtpSettingsPart>();
            var recipient = sendTo != null ? sendTo : new List<string> { smtp.Address };
            data.Add("Subject", template.Subject);
            data.Add("Body", body);
            data.Add("Recipients", String.Join(",", recipient));
            if (bcc != null) {
                data.Add("Bcc", String.Join(",", bcc));
            }
            //var watch = Stopwatch.StartNew();
            //int msgsent = 0;

            //for(int i=0;i<20;i++) {
            //    msgsent++;
            //    data["Subject"] = msgsent.ToString();
            //    data["Bcc"] = "lorenzo.frediani@laser-group.com";
            //    _messageService.Send(SmtpMessageChannel.MessageType, data);
            //}
            //watch.Stop();
            //_notifier.Add(NotifyType.Information, T("Sent " + msgsent.ToString()+" email in Milliseconds:" + watch.ElapsedMilliseconds.ToString()));            
            if (!queued) {
                _messageService.Send(SmtpMessageChannel.MessageType, data);
            } else {
                var priority = 0;//normal 50 to hight -50 to low

                _jobsQueueService.Enqueue("IMessageService.Send", new { type = SmtpMessageChannel.MessageType, parameters = data }, priority);
            }

            return true;
        }

        public string RitornaParsingTemplate(dynamic contentModel, int templateId, object viewBag = null) {

            ParseTemplateContext templatectx = new ParseTemplateContext();
            var template = GetTemplate(templateId);

            var baseUri = new Uri(_services.WorkContext.CurrentSite.BaseUrl);
            string host = "";
            string mediaUrl = "";

            if (_services.WorkContext.HttpContext != null) {
                var urlHelper = new UrlHelper(_services.WorkContext.HttpContext.Request.RequestContext);

                // Creo un model che ha Content (il contentModel), Urls con alcuni oggetti utili per il template
                // Nel template pertanto Model, diventa Model.Content
                host = string.Format("{0}://{1}{2}",
                                     _services.WorkContext.HttpContext.Request.Url.Scheme,
                                     _services.WorkContext.HttpContext.Request.Url.Host,
                                     _services.WorkContext.HttpContext.Request.Url.Port == 80
                                     ? string.Empty
                                     : ":" + _services.WorkContext.HttpContext.Request.Url.Port);

                mediaUrl = urlHelper.MediaExtensionsImageUrl();
            } else {
                host = string.Format("{0}://{1}{2}",
                                     baseUri.Scheme,
                                     baseUri.Host,
                                     baseUri.Port == 80 ? string.Empty : ":" + baseUri.Port);

                var tenantPrefix = GetTenantUrlPrexix(_shellSettings);
                mediaUrl = string.Format("/{0}/{1}{2}", baseUri.GetComponents(UriComponents.Path, UriFormat.Unescaped), tenantPrefix, @"Laser.Orchard.StartupConfig/MediaTransform/Image");
            }

            var dynamicModel = new {
                WorkContext = _services.WorkContext,
                Content = contentModel,
                Urls = new {
                    //SubscriptionSubscribe = urlHelper.SubscriptionSubscribe(),
                    //SubscriptionUnsubscribe = urlHelper.SubscriptionUnsubscribe(),
                    //SubscriptionConfirmSubscribe = urlHelper.SubscriptionConfirmSubscribe(),
                    //SubscriptionConfirmUnsubscribe = urlHelper.SubscriptionConfirmUnsubscribe(),
                    BaseUrl = baseUri,
                    MediaUrl = mediaUrl,
                    Domain = host,

                }.ToExpando()
            };
            templatectx.Model = dynamicModel;

            // TODO: Passare link Campagna come per il test
            var razorviewBag = viewBag;
            RazorEngine.Templating.DynamicViewBag vb = new DynamicViewBag();
            try {
                foreach (string key in ((Dictionary<string, object>)viewBag).Keys) {
                    vb.AddValue(key, ((IDictionary<string, object>)viewBag)[key]);
                }
            } catch { }
            templatectx.ViewBag = vb;

            return ParseTemplate(template, templatectx);
        }

        private string GetTenantUrlPrexix(ShellSettings shellSettings) {
            // calcola il prefix del tenant corrente
            string tenantPath = shellSettings.RequestUrlPrefix ?? "";

            if (tenantPath != "") {
                tenantPath = tenantPath + "/";
            }
            return tenantPath;
        }


    }
}