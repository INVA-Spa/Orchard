﻿using AutoMapper;
using Laser.Orchard.StartupConfig.Models;
using Laser.Orchard.StartupConfig.Services;
using Laser.Orchard.StartupConfig.ViewModels;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Drivers;
using Orchard.Environment.Extensions;
using Orchard.Localization;
using System.Web.Mvc;
using System.Linq;
using System.Collections.Generic;
using Orchard.Environment.Configuration;

namespace Laser.Orchard.StartupConfig.Drivers {

    [OrchardFeature("Laser.Orchard.StartupConfig.Maintenance")]
    public class MaintenancePartDriver : ContentPartDriver<MaintenancePart> {
       // public Localizer T { get; set; }
        private readonly IMaintenanceService _maintenance;
        private readonly ShellSettings _shellSettings;
        public MaintenancePartDriver(IMaintenanceService maintenance, ShellSettings shellSettings) {
            _maintenance = maintenance;
            _shellSettings = shellSettings;
        }
        protected override string Prefix {
            get { return "MaintenancePartDriver"; }
        }

        //GET
        protected override DriverResult Editor(MaintenancePart part, dynamic shapeHelper) {
            MaintenanceVM MaintenanceVM = new MaintenanceVM();
            Mapper.CreateMap<MaintenancePart, MaintenanceVM>();
            Mapper.Map(part, MaintenanceVM);
            List<string> AllTenantName = new List<string>();
            AllTenantName.Add("All Tenant");
            AllTenantName.AddRange(_maintenance.GetAllTenantName());


            MaintenanceVM.List_Tenant =  new SelectList(AllTenantName,"All Tenant");
            MaintenanceVM.Selected_TenantVM = (part.Selected_Tenant ?? "All Tenant").Split(',').ToArray();
            MaintenanceVM.CurrentTenant=_shellSettings.Name;
            //MaintenanceVM.Selected_Tenant = (part.Selected_Tenant??"").Split(',').ToList();
            return ContentShape("Parts_Maintenance_Edit",
                    () => shapeHelper.EditorTemplate(
                        TemplateName: "Parts/Maintenance_Edit",
                        Model: MaintenanceVM,
                        Prefix: Prefix));
        }

        //POST
        protected override DriverResult Editor(MaintenancePart part, IUpdateModel updater, dynamic shapeHelper) {
            MaintenanceVM MaintenanceVM = new MaintenanceVM();
            if (updater.TryUpdateModel(MaintenanceVM, Prefix, null, null)) {
                Mapper.CreateMap<MaintenanceVM, MaintenancePart>();
                Mapper.Map( MaintenanceVM,part);
                part.Selected_Tenant=string.Join(",",MaintenanceVM.Selected_TenantVM);
            }
            else {
                //foreach (var modelState in ModelState.Values) {
                //    foreach (var error in modelState.Errors) {
                //        Debug.WriteLine(error.ErrorMessage);
                //    }
                //}
                //  _notifier.Add(NotifyType.Error, T("Error on update google analytics"));
            }
            return Editor(part, shapeHelper);
        }
    }
}