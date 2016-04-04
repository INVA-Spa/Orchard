﻿using Laser.Orchard.CommunicationGateway.Services;
using Laser.Orchard.Mobile.Models;
using Laser.Orchard.Mobile.Services;
using Laser.Orchard.Queries.Services;
using NHibernate.Transform;
using Orchard;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Handlers;
using Orchard.Data;
using Orchard.Environment.Extensions;
using Orchard.Localization;
using Orchard.Tasks.Scheduling;
using Orchard.UI.Notify;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Laser.Orchard.Mobile.Handlers {

    #region Support Classes

    // Classe queryForSms
    public class SmsHQL {
        public int Id { get; set; }
        public string Title { get; set; }
        public string SmsPrefix { get; set; }
        public string SmsNumber { get; set; }
    }

    #endregion

    [OrchardFeature("Laser.Orchard.SmsGateway")]
    public class SmsGatewayPartHandler : ContentHandler {

        private readonly INotifier _notifier;
        private readonly IOrchardServices _orchardServices;
        private readonly ISmsServices _smsServices;
        private readonly ISmsCommunicationService _smsCommunicationService;
        private readonly IScheduledTaskManager _taskManager;

        public Localizer T { get; set; }


        public SmsGatewayPartHandler(IRepository<SmsGatewayPartRecord> repository, INotifier notifier, IOrchardServices orchardServices, ISmsServices smsServices,
                                     ISmsCommunicationService smsCommunicationService, IScheduledTaskManager taskManager) {
            _orchardServices = orchardServices;
            _smsServices = smsServices;
            _smsCommunicationService = smsCommunicationService;
            _notifier = notifier;
            _taskManager = taskManager;

            Filters.Add(StorageFilter.For(repository));

            OnUpdated<SmsGatewayPart>((context, part) => {
                if (_orchardServices.WorkContext.HttpContext.Request.Form["submit.Test"] == "submit.SmsTest") {
                    if (part.SendToTestNumber && part.NumberForTest != string.Empty) {
                        if (part.ContentItem.ContentType == "CommunicationAdvertising") {

                            string linktosend = "";
                            ICommunicationService _communicationService;

                            bool tryed = _orchardServices.WorkContext.TryResolve<ICommunicationService>(out _communicationService);
                            if (tryed) {
                                if (_communicationService.CampaignLinkExist(part)) {
                                    linktosend = _communicationService.GetCampaignLink("Sms", part);
                                }
                            }
                            string messageToSms = part.Message + " " + linktosend;

                            // Id deve essere univoco - Utilizzo part.Id per il Publish e lo modifico per SendToTestNumber
                            string IdSendToTest = "OrchardTest_" + part.Id.ToString() + "_" + DateTime.Now.ToString("yyyyMMddHHmmss");

                            // Invio SMS a NumberForTest
                            _smsServices.SendSms(
                                part.NumberForTest.Split(';').Select(x => new SmsHQL { SmsPrefix = "", SmsNumber = x, Id = 0, Title = "Test" }).ToArray(),
                                messageToSms, part.Alias, IdSendToTest, part.HaveAlias);
                        }
                    }
                }
            });

            OnPublished<SmsGatewayPart>((context, part) => {
                if (part.SendOnNextPublish && !part.SmsMessageSent) {
                    ContentItem ci = _orchardServices.ContentManager.Get(part.ContentItem.Id);
                    _taskManager.CreateTask("Laser.Orchard.SmsGateway.Task", DateTime.UtcNow.AddMinutes(1), ci);
                }
            });
        }
    }
}