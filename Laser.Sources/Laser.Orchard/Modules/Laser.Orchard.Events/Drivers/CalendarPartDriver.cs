﻿using Laser.Orchard.Events.Models;
using Laser.Orchard.Events.Services;
using Laser.Orchard.Events.ViewModels;
using Orchard;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Drivers;
using Orchard.Core.Title.Models;
using Orchard.Data;
using Orchard.Forms.Services;
using Orchard.Localization;
using Orchard.Projections.Descriptors.Layout;
using Orchard.Projections.Models;
using Orchard.Projections.Services;
using Orchard.UI.Navigation;
using Orchard.Utility.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Globalization;

namespace Laser.Orchard.Events.Drivers
{
    public class CalendarPartDriver : ContentPartDriver<CalendarPart>
    {
        public Localizer T { get; set; }
        public IOrchardServices _orchardServices { get; set; }

        private readonly IProjectionManager _projectionManager;
        private readonly IRepository<QueryPartRecord> _queryRepository;
        private readonly IEventsService _eventsService;
        private readonly IRepository<LayoutRecord> _layoutRepository;

        public CalendarPartDriver(IProjectionManager projectionManager, IRepository<QueryPartRecord> queryRepository,
                                  IOrchardServices orchardServices, IEventsService eventsService, IRepository<LayoutRecord> layoutRepository)
        {
            T = NullLocalizer.Instance;
            _projectionManager = projectionManager;
            _queryRepository = queryRepository;
            _eventsService = eventsService;
            _orchardServices = orchardServices;
            _layoutRepository = layoutRepository;
        }

        protected override string Prefix
        {
            get { return "Calendar"; }
        }

        /// <summary>
        /// Defines the shapes required for the part's main view.
        /// </summary>
        /// <param name="part">The part.</param>
        /// <param name="displayType">The display type.</param>
        /// <param name="shapeHelper">The shape helper.</param>
        protected override DriverResult Display(CalendarPart part, string displayType, dynamic shapeHelper)
        {
            if (displayType == "Summary" || displayType == "SummaryAdmin")
                return null;

            if (part.CalendarShape.Equals("L"))
            {
                dynamic pagerShape = null;

                var list = new List<EventViewModel>();
                list = _eventsService.GetEventList(part);
                list = _eventsService.OrderEventList(list, part);

                if (list != null && part.DisplayPager)
                {
                    var page = 0;
                    var pageKey = String.IsNullOrWhiteSpace(part.PagerSuffix) ? "page" : "page-" + part.PagerSuffix;

                    //Recupero i parametri di paging
                    var queryString = _orchardServices.WorkContext.HttpContext.Request.QueryString;
                    if (queryString.AllKeys.Contains(pageKey))
                        Int32.TryParse(queryString[pageKey], out page);

                    int pageSize = 1;
                    if (part.ItemsPerPage > 0)
                        pageSize = part.ItemsPerPage;
                    else
                    {
                        var siteSettings = _orchardServices.WorkContext.CurrentSite;
                        pageSize = siteSettings.PageSize;
                    }

                    Pager pager = new Pager(_orchardServices.WorkContext.CurrentSite, page, pageSize);
                    pagerShape = shapeHelper.Pager(pager).PagerId(pageKey)
                                                            .ContentPart(part)
                                                            .TotalItemCount(list.Count());

                    list = list.Skip(pager.GetStartIndex()).Take(pager.PageSize).ToList();
                }

                return ContentShape("Parts_EventList",
                    () =>
                    {
                        return shapeHelper.Parts_EventList(
                            EventList: list,
                            Pager: pagerShape
                        );
                    });
            }
            else
            {
                return ContentShape("Parts_Calendar",
                    () => shapeHelper.Parts_Calendar(
                        Calendar: part
                        ));
            }
        }

        protected override DriverResult Editor(CalendarPart part, dynamic shapeHelper)
        {
            return ContentShape("Parts_Calendar_Edit",
                                () =>
                                {
                                    var model = new CalendarEditViewModel
                                    {
                                        DisplayPager = part.DisplayPager,
                                        ItemsPerPage = part.ItemsPerPage,
                                        CalendarShape = part.CalendarShape,
                                        PagerSuffix = part.PagerSuffix,
                                        StartDate = part.StartDate,
                                        NumDays = part.NumDays,
                                        QueryLayoutRecordId = "-1",
                                    };

                                    // concatenated Query and Layout ids for the view
                                    if (part.QueryPartRecord != null)
                                    {
                                        model.QueryLayoutRecordId = part.QueryPartRecord.Id + ";";
                                    }

                                    if (part.LayoutRecord != null)
                                    {
                                        model.QueryLayoutRecordId += part.LayoutRecord.Id.ToString();
                                    }
                                    else
                                    {
                                        model.QueryLayoutRecordId += "-1";
                                    }

                                    // populating the list of queries and layouts
                                    var layouts = _projectionManager.DescribeLayouts().SelectMany(x => x.Descriptors).ToList();
                                    model.QueryRecordEntries = _orchardServices.ContentManager.Query<QueryPart>().Join<TitlePartRecord>()
                                                                                                                 .OrderBy(x => x.Title)
                                                                                                                 .List()
                                        .Select(x => new QueryRecordEntry
                                        {
                                            Id = x.Id,
                                            Name = x.Name,
                                            LayoutRecordEntries = x.Layouts.Select(l => new LayoutRecordEntry
                                            {
                                                Id = l.Id,
                                                Description = GetLayoutDescription(layouts, l)
                                            })
                                        });


                                    return shapeHelper.EditorTemplate(TemplateName: "Parts/Calendar", Model: model, Prefix: Prefix);
                                });
        }

        protected override DriverResult Editor(CalendarPart part, IUpdateModel updater, dynamic shapeHelper)
        {
            var model = new CalendarEditViewModel();

            if (updater.TryUpdateModel(model, Prefix, null, null))
            {

                var queryLayoutIds = model.QueryLayoutRecordId.Split(new[] { ';' });

                part.DisplayPager = model.DisplayPager;
                part.ItemsPerPage = model.ItemsPerPage;
                part.CalendarShape = model.CalendarShape;
                part.PagerSuffix = (model.PagerSuffix ?? String.Empty).Trim();
                part.StartDate = model.StartDate;
                part.NumDays = model.NumDays;
                part.QueryPartRecord = _queryRepository.Get(Int32.Parse(queryLayoutIds[0]));
                part.LayoutRecord = part.QueryPartRecord.Layouts.FirstOrDefault(x => x.Id == Int32.Parse(queryLayoutIds[1]));

                if (!String.IsNullOrWhiteSpace(part.PagerSuffix) && !String.Equals(part.PagerSuffix.ToSafeName(), part.PagerSuffix, StringComparison.OrdinalIgnoreCase))
                {
                    updater.AddModelError("PagerSuffix", T("Suffix should not contain special characters."));
                }
            }

            return Editor(part, shapeHelper);
        }

        protected override void Importing(CalendarPart part, global::Orchard.ContentManagement.Handlers.ImportContentContext context) {
            var root = context.Data.Element(part.PartDefinition.Name);
            var CalendarShape = root.Attribute("CalendarShape");
            if (CalendarShape != null) {
                part.CalendarShape = CalendarShape.Value;
            }
            var DisplayPager = root.Attribute("DisplayPager");
            if (DisplayPager != null) {
                part.DisplayPager = Convert.ToBoolean(DisplayPager.Value);
            }
            var ItemsPerPage = root.Attribute("ItemsPerPage");
            if (ItemsPerPage != null) {
                part.ItemsPerPage = Convert.ToInt32(ItemsPerPage.Value);
            }
            var NumDays = root.Attribute("NumDays");
            if (NumDays != null) {
                part.NumDays = NumDays.Value;
            }
            var PagerSuffix = root.Attribute("PagerSuffix");
            if (PagerSuffix != null) {
                part.PagerSuffix = PagerSuffix.Value;
            }
            var StartDate = root.Attribute("StartDate");
            if (StartDate != null) {
                part.StartDate = StartDate.Value;
            }
            context.ImportAttribute(part.PartDefinition.Name, "QueryPartRecord_Id", x => {
                // cerca la campagna con l'identity indicato
                var query = context.GetItemFromSession(x);
                // verifica che si tratti effettivamente di una query
                if (query != null && query.Has<QueryPart>()) {
                    part.QueryPartRecord = query.As<QueryPart>().Record;
                    // layout
                    var LayoutRecord_Description = root.Attribute("LayoutRecord_Description");
                    var layoutDesc = LayoutRecord_Description != null ? LayoutRecord_Description.Value : null;
                    var LayoutRecord_Position = root.Attribute("LayoutRecord_Position");
                    var posizione = LayoutRecord_Position != null ? Convert.ToInt32(LayoutRecord_Position.Value) : 0;

                    var elencoLayout = _layoutRepository.Fetch(y => y.QueryPartRecord.Id == query.Id && (y.Description ?? "") == (layoutDesc ?? "")).OrderBy<LayoutRecord, int>(y => {
                        return y.Id;
                    });
                    int conteggio = 0;
                    foreach (var layout in elencoLayout) {
                        if (conteggio == posizione) {
                            part.LayoutRecord = layout;
                            break;
                        }
                        conteggio++;
                    }

                    //var layout = _layoutRepository.Fetch(y => y.QueryPartRecord.Id == query.Id && y.Description == layoutDesc).OrderBy<LayoutRecord, int>(y => {
                    //    return y.Id;
                    //}).Take(posizione + 1).LastOrDefault();
                    //if (layout != null) {
                    //    part.LayoutRecord = layout;
                    //}
                }
            });
        }

        protected override void Exporting(CalendarPart part, global::Orchard.ContentManagement.Handlers.ExportContentContext context) {
            var root = context.Element(part.PartDefinition.Name);
            root.SetAttributeValue("CalendarShape", part.CalendarShape);
            root.SetAttributeValue("DisplayPager", part.DisplayPager);
            root.SetAttributeValue("ItemsPerPage", part.ItemsPerPage);
            root.SetAttributeValue("NumDays", part.NumDays);
            root.SetAttributeValue("PagerSuffix", part.PagerSuffix);
            root.SetAttributeValue("StartDate", part.StartDate);
            if(part.QueryPartRecord != null && part.QueryPartRecord.Id > 0) {
                var query = _orchardServices.ContentManager.Get(part.QueryPartRecord.Id);
                root.SetAttributeValue("QueryPartRecord_Id", _orchardServices.ContentManager.GetItemMetadata(query).Identity.ToString());
                if (part.LayoutRecord != null && part.LayoutRecord.Id > 0) {
                    // identifica il layout in base a description e posizione sequenziale (a parità di description)
                    var layoutDesc = part.LayoutRecord.Description;
                    var elencoLayout = _layoutRepository.Fetch(x => x.QueryPartRecord.Id == query.Id && x.Description == layoutDesc).OrderBy<LayoutRecord, int>(x => {
                        return x.Id;
                    });
                    int posizione = 0;
                    foreach (var layout in elencoLayout) {
                        if (layout.Id == part.LayoutRecord.Id) {
                            break;
                        }
                        posizione++;
                    }
                    root.SetAttributeValue("LayoutRecord_Description", layoutDesc);
                    root.SetAttributeValue("LayoutRecord_Position", posizione);
                }
            }
        }
        private static string GetLayoutDescription(IEnumerable<LayoutDescriptor> layouts, LayoutRecord l)
        {
            var descriptor = layouts.FirstOrDefault(x => l.Category == x.Category && l.Type == x.Type);
            return String.IsNullOrWhiteSpace(l.Description) ? descriptor.Display(new LayoutContext { State = FormParametersHelper.ToDynamic(l.State) }).Text : l.Description;
        }
    }
}