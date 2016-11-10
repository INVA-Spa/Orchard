﻿using AutoMapper;
using Laser.Orchard.CommunicationGateway.Models;
using Laser.Orchard.CommunicationGateway.ViewModels;
using Orchard;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Drivers;
using Orchard.Data;
using Orchard.Localization;
using Orchard.UI.Admin;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Laser.Orchard.CommunicationGateway.Drivers {
    public class SmsContactPartDriver : ContentPartDriver<SmsContactPart> {
        public Localizer T { get; set; }
        protected override string Prefix {
            get { return "Laser.Mobile.SmsContact"; }
        }
        private readonly IRepository<CommunicationSmsRecord> _repoSms;
        private readonly ITransactionManager _transaction;
        private readonly IOrchardServices _orchardServices;
        private IMapper _mapper;

        public SmsContactPartDriver(IRepository<CommunicationSmsRecord> repoSms, ITransactionManager transaction, IOrchardServices orchardServices) {
            _repoSms = repoSms;
            T = NullLocalizer.Instance;
            _transaction = transaction;
            _orchardServices = orchardServices;
            
            var mapperConfiguration = new MapperConfiguration(cfg => {
                cfg.CreateMap<CommunicationSmsRecord, View_SmsVM_element>();
                cfg.CreateMap<View_SmsVM_element, CommunicationSmsRecord>();
            });
            _mapper = mapperConfiguration.CreateMapper();
        }

        protected override DriverResult Display(SmsContactPart part, string displayType, dynamic shapeHelper) {
            //Determine if we're on an admin page
            bool isAdmin = AdminFilter.IsApplied(_orchardServices.WorkContext.HttpContext.Request.RequestContext);
            if (isAdmin) {
                if (displayType == "Detail") {
                    View_SmsVM viewModel = new View_SmsVM();
                    View_SmsVM_element vm = new View_SmsVM_element();
                    if (part.SmsEntries.Value != null) {
                        List<CommunicationSmsRecord> oldviewModel = part.SmsEntries.Value.ToList();
                        foreach (CommunicationSmsRecord cm in oldviewModel) {
                            vm = new View_SmsVM_element();
                            _mapper.Map<CommunicationSmsRecord, View_SmsVM_element>(cm, vm);
                            viewModel.Elenco.Add(vm);
                        }
                    }
                    return ContentShape("Parts_SmsContact",
                        () => shapeHelper.Parts_SmsContact(Elenco: viewModel.Elenco));
                } else {
                    return null;
                }
            } else {
                return null;
            }
        }

        protected override DriverResult Editor(SmsContactPart part, dynamic shapeHelper) {
            View_SmsVM viewModel = new View_SmsVM();
            View_SmsVM_element vm = null;
            if (part.SmsEntries.Value != null) {
                List<CommunicationSmsRecord> oldviewModel = part.SmsEntries.Value.ToList();
                foreach (CommunicationSmsRecord cm in oldviewModel) {
                    vm = new View_SmsVM_element();
                    _mapper.Map<CommunicationSmsRecord, View_SmsVM_element>(cm, vm);
                    viewModel.Elenco.Add(vm);
                }
            }
            return ContentShape("Parts_SmsContact_Edit", () => shapeHelper.EditorTemplate(TemplateName: "Parts/SmsContact_Edit", Model: viewModel, Prefix: Prefix));
        }



        protected override DriverResult Editor(SmsContactPart part, IUpdateModel updater, dynamic shapeHelper) {
            View_SmsVM oldviewModel = new View_SmsVM();

            updater.TryUpdateModel(oldviewModel, Prefix, null, null);
            bool error = false;
            _transaction.Demand();
            foreach (View_SmsVM_element vmel in oldviewModel.Elenco) {
                if ((vmel.Delete || string.IsNullOrEmpty(vmel.Sms)) && vmel.Id > 0) {
                    CommunicationSmsRecord cmr = _repoSms.Fetch(x => x.Id == vmel.Id).FirstOrDefault();
                    _repoSms.Delete(cmr);
                }
                else
                    if (!vmel.Delete) {
                        if (!string.IsNullOrEmpty(vmel.Sms))
                            if (_repoSms.Fetch(x => x.Sms == vmel.Sms && x.Prefix == vmel.Prefix && x.Id != vmel.Id).Count() > 0) {
                                error = true;
                                updater.AddModelError("Error", T("Sms can't be assigned is linked to other contact"));
                            }
                        if (vmel.Id > 0) {
                            CommunicationSmsRecord cmr = _repoSms.Fetch(x => x.Id == vmel.Id).FirstOrDefault();
                            if (cmr.Sms != vmel.Sms || cmr.Prefix != vmel.Prefix || cmr.Validated != vmel.Validated|| 
                                cmr.AccettatoUsoCommerciale != vmel.AccettatoUsoCommerciale ||
                                cmr.AutorizzatoTerzeParti != vmel.AutorizzatoTerzeParti) {
                                cmr.Sms = vmel.Sms;
                                cmr.Prefix = vmel.Prefix;
                                cmr.Validated = vmel.Validated;
                                cmr.AccettatoUsoCommerciale = vmel.AccettatoUsoCommerciale;
                                cmr.AutorizzatoTerzeParti = vmel.AutorizzatoTerzeParti;
                                cmr.DataModifica = DateTime.Now;
                                _repoSms.Update(cmr);

                            }
                        }
                        else {
                            View_SmsVM_element vm = new View_SmsVM_element();
                            CommunicationSmsRecord cmr = new CommunicationSmsRecord();
                            _mapper.Map<View_SmsVM_element, CommunicationSmsRecord>(vm, cmr);
                            cmr.Sms = vmel.Sms;
                            cmr.Validated = vmel.Validated;
                            cmr.AccettatoUsoCommerciale = vmel.AccettatoUsoCommerciale;
                            cmr.AutorizzatoTerzeParti = vmel.AutorizzatoTerzeParti;
                            cmr.Prefix = vmel.Prefix;
                            cmr.SmsContactPartRecord_Id = part.Id;
                            _repoSms.Create(cmr);

                        }
                    }
            }
            if (error == true)
                _transaction.Cancel();
            else
                _repoSms.Flush();
            return Editor(part, shapeHelper);
        }
    }

}