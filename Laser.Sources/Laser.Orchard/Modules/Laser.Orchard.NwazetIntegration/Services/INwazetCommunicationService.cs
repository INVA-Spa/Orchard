﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using AutoMapper;
using Laser.Orchard.CommunicationGateway.Services;
using Laser.Orchard.NwazetIntegration.Models;
using Nwazet.Commerce.Models;
using Orchard;
using Orchard.ContentManagement;
using Orchard.Data;
using Orchard.Logging;
using Orchard.Security;

namespace Laser.Orchard.NwazetIntegration.Services {
    public interface INwazetCommunicationService : IDependency {
        void OrderToContact(OrderPart order);
        List<AddressRecord> GetShippingByUser(IUser user);
        List<AddressRecord> GetBillingByUser(IUser user);
    }



    public class NwazetCommunicationService: INwazetCommunicationService {

        private readonly IOrchardServices _orchardServices;
        private readonly ICommunicationService _communicationService;
        private readonly IRepository<AddressRecord> _addressRecord;
        public ILogger _Logger { get; set; }
        public NwazetCommunicationService(
            IOrchardServices orchardServices
            ,ICommunicationService communicationService
            , IRepository<AddressRecord> addressRecord) {
            _orchardServices = orchardServices;
            _communicationService = communicationService;
            _addressRecord = addressRecord;
            _Logger = NullLogger.Instance;

        }
        private List<AddressRecord> GetAddressByUser(IUser user, AddressRecordType type) {
            if (user.Id > 0) {
                var contactpart = _communicationService.GetContactFromUser(user.Id);
                if (contactpart == null) { // non dovrebbe mai succedere (inserito nel caso cambiassimo la logica già implementata)
                    _communicationService.UserToContact(user);
                    contactpart = _communicationService.GetContactFromUser(user.Id);
                }
                if (contactpart != null) {
                    return _addressRecord.Fetch(c => c.AddressType == type && c.NwazetContactPartRecord_Id == contactpart.Id).OrderByDescending(z => z.TimeStampUTC).ToList();
                }
            }
            return new List<AddressRecord>();
        }
        public List<AddressRecord> GetShippingByUser(IUser user) {
            return GetAddressByUser(user, AddressRecordType.ShippingAddress);
        }
        public List<AddressRecord> GetBillingByUser(IUser user) {
            return GetAddressByUser(user, AddressRecordType.BillingAddress);
        }

        public void OrderToContact(OrderPart order) {
            // tutto in try catch perchè viene scatenato appena finito il pagamento e quindi non posso permettermi di annullare la transazione
            try {
                // recupero il contatto
                var currentUser = _orchardServices.WorkContext.CurrentUser;
                var ContactList = new List<ContentItem>();
                if (currentUser != null) {
                    var contactpart = _communicationService.GetContactFromUser(currentUser.Id);
                    if (contactpart == null) { // non dovrebbe mai succedere (inserito nel caso cambiassimo la logica già implementata)
                        _communicationService.UserToContact(currentUser);
                        contactpart = _communicationService.GetContactFromUser(currentUser.Id);
                    }
                    ContactList.Add(contactpart.ContentItem);
                }
                else {
                    var contacts = _communicationService.GetContactsFromMail(order.CustomerEmail);
                    if (contacts.Count > 0) {
                        ContactList = contacts;
                    }
                    else {
                        var newcontact = _orchardServices.ContentManager.Create("CommunicationContact", VersionOptions.Draft);
                        ((dynamic)newcontact).CommunicationContactPart.Master = false;
                        ContactList.Add(newcontact);
                    }
                }
                foreach (var contactItem in ContactList) {
                    // nel caso in cui una sincro fallisce continua con 
                    try {
                        StoreAddress(order.BillingAddress, "BillingAddress", contactItem);
                    }
                    catch (Exception ex) {
                        _Logger.Error("OrderToContact -> BillingAddress -> order id= " + order.Id.ToString() + " Error: " + ex.Message);
                    }
                    try {
                        StoreAddress(order.ShippingAddress, "ShippingAddress", contactItem);
                    }
                    catch (Exception ex) {
                        _Logger.Error("OrderToContact -> ShippingAddress -> order id= " + order.Id.ToString() + " Error: " + ex.Message);
                    }
                    try {
                        _communicationService.AddEmailToContact(order.CustomerEmail, contactItem);
                    }
                    catch (Exception ex) {
                        _Logger.Error("OrderToContact -> AddEmailToContact -> order id= " + order.Id.ToString() + " Error: " + ex.Message);
                    }
                    try {
                        _communicationService.AddSmsToContact((order.CustomerPhone+' ').Split(' ')[0], (order.CustomerPhone + ' ').Split(' ')[1], contactItem);
                    }
                    catch (Exception ex) {
                        _Logger.Error("OrderToContact -> AddSmsToContact -> order id= " + order.Id.ToString() + " Error: " + ex.Message);
                    }
                }
            }
            catch (Exception myex){
                _Logger.Error("OrderToContact -> order id= " + order.Id.ToString()+" Error: " + myex.Message);
            }
        }

        private void StoreAddress(Address address, string typeAddress,  ContentItem contact) {
            var typeAddressValue = (AddressRecordType)Enum.Parse(typeof(AddressRecordType), typeAddress);
            Mapper.Initialize(cfg => {
                cfg.CreateMap<Address, AddressRecord>();
            });
            var addressToStore = new AddressRecord();
            Mapper.Map<Address, AddressRecord>(address, addressToStore);
            addressToStore.AddressType = typeAddressValue;
            addressToStore.NwazetContactPartRecord_Id = contact.Id;
            bool AddNewAddress = true;
            foreach (var existingAddressRecord in contact.As<NwazetContactPart>().NwazetAddressRecord) {
                if (addressToStore.Equals(existingAddressRecord)) {
                    AddNewAddress = false;
                    existingAddressRecord.TimeStampUTC = DateTime.UtcNow;
                    _addressRecord.Update(existingAddressRecord);
                    _addressRecord.Flush();
                }
            }
            if (AddNewAddress) {
                _addressRecord.Create(addressToStore);
                _addressRecord.Flush();
            }
        }
    }
}