﻿using Orchard.ContentManagement;
using Orchard.Data;
using Orchard.Forms.Services;
using Orchard.Projections.Descriptors;
using Orchard.Projections.Descriptors.Filter;
using Orchard.Projections.Models;
using Orchard.Projections.Services;
using Laser.Orchard.Reporting.Models;
using Laser.Orchard.Reporting.Providers;
using Orchard.Tokens;
using System;
using System.Collections.Generic;
using System.Linq;
using Orchard.Core.Common.Fields;
using System.Collections;
using NHibernate.Transform;
using NHibernate;
using Orchard.Core.Title.Models;
using Orchard.Localization;
using Orchard.Security;
using Orchard.Security.Permissions;
using Orchard.Logging;
using System.Text;
using System.Globalization;

namespace Laser.Orchard.Reporting.Services {
    public class ReportManager : IReportManager
    {
        private readonly IEnumerable<IGroupByParameterProvider> groupByProviders;
        private readonly IContentManager contentManager;
        private readonly IProjectionManager projectionManager;
        private readonly ITokenizer _tokenizer;
        private readonly IRepository<QueryPartRecord> queryRepository;
        private readonly ITransactionManager _transactionManager;
        private readonly IAuthorizer _authorizer;
        private Dictionary<int, Permission> _reportPermissions;
        private Dictionary<int, Permission> _dashboardPermissions;
        private readonly IRepository<ReportRecord> _reportRepository;
        public Localizer T { get; set; }
        public ILogger Log { get; set; }

        public ReportManager(
            IRepository<QueryPartRecord> queryRepository,
            IRepository<ReportRecord> reportRepository,
            IProjectionManager projectionManager,
            IEnumerable<IGroupByParameterProvider> groupByProviders,
            IContentManager contentManager,
            ITokenizer tokenizer,
            ITransactionManager transactionManager,
            IAuthorizer authorizer)
        {
            this.queryRepository = queryRepository;
            _reportRepository = reportRepository;
            this.projectionManager = projectionManager;
            _tokenizer = tokenizer;
            this.contentManager = contentManager;
            this.groupByProviders = groupByProviders;
            _transactionManager = transactionManager;
            _authorizer = authorizer;
            T = NullLocalizer.Instance;
            Log = NullLogger.Instance;
        }

        public IEnumerable<TypeDescriptor<GroupByDescriptor>> DescribeGroupByFields()
        {
            DescribeGroupByContext context = new DescribeGroupByContext();
            foreach (var provider in this.groupByProviders)
            {
                provider.Describe(context);
            }

            return context.Describe();
        }

        public int GetCount(ReportRecord report, IContent container)
        {
            if (report == null) { throw new ArgumentNullException("report"); }
            if (report.Query == null) { throw new ArgumentException("There is no QueryRecord associated with the Report"); }

            var descriptors = this.DescribeGroupByFields();
            var descriptor = descriptors.SelectMany(c => c.Descriptors).FirstOrDefault(c => c.Category == report.GroupByCategory && c.Type == report.GroupByType);

            if (descriptor == null)
            {
                throw new ArgumentOutOfRangeException("There is no GroupByDescriptor for the given category and type");
            }

            var queryRecord = this.queryRepository.Get(report.Query.Id);

            var contentQueries = this.GetContentQueries(queryRecord, queryRecord.SortCriteria, container);

            return contentQueries.Sum(c => c.Count());
        }

        public IEnumerable<AggregationResult> RunReport(ReportRecord report, IContent container)
        {
            if (report == null) { throw new ArgumentNullException("report"); }
            if (report.Query == null) { throw new ArgumentException("There is no QueryRecord associated with the Report"); }

            var descriptors = this.DescribeGroupByFields();
            var descriptor = descriptors.SelectMany(c => c.Descriptors).FirstOrDefault(c => c.Category == report.GroupByCategory && c.Type == report.GroupByType);

            if (descriptor == null)
            {
                throw new ArgumentOutOfRangeException("There is no GroupByDescriptor for the given category and type");
            }

            var queryRecord = this.queryRepository.Get(report.Query.Id);

            var contentQueries = this.GetContentQueries(queryRecord, queryRecord.SortCriteria, container);

            Dictionary<string, AggregationResult> returnValue = new Dictionary<string, AggregationResult>();

            foreach (var contentQuery in contentQueries)
            {
                var dictionary = descriptor.Run(contentQuery, (AggregateMethods)report.AggregateMethod);

                foreach (var item in dictionary)
                {
                    if (returnValue.ContainsKey(item.Label))
                    {
                        var previousItem = returnValue[item.Label];
                        previousItem.AggregationValue += item.AggregationValue;
                        returnValue[item.Label] = previousItem;
                    }
                    else
                    {
                        returnValue[item.Label] = item;
                    }
                }
            }

            return returnValue.Values;
        }

        public IEnumerable<AggregationResult> RunHqlReport(ReportRecord report, IContent container) {
            if (report == null) { throw new ArgumentNullException("report"); }
            if (report.Query == null) { throw new ArgumentException("There is no QueryRecord associated with the Report"); }

            var queryRecord = contentManager.Get(report.Query.Id);
            var contentQuery = queryRecord.Parts.FirstOrDefault(x => x.PartDefinition.Name == "MyCustomQueryPart");
            if(contentQuery == null) {
                throw new ArgumentOutOfRangeException("HQL query not valid.");
            }
            var queryField = contentQuery.Get(typeof(TextField), "QueryString") as TextField;
            var query = queryField.Value.Trim();
            // tokens replacement
            Dictionary<string, object> contextDictionary = new Dictionary<string, object>();
            if (container != null) {
                contextDictionary.Add("Content", container);
            }
            query = _tokenizer.Replace(query, contextDictionary);
            IQuery hql = null;
            Dictionary<string, AggregationResult> returnValue = new Dictionary<string, AggregationResult>();
            IEnumerable result = null;
            // check on hql query
            if (query.StartsWith("select ", StringComparison.InvariantCultureIgnoreCase) == false) {
                throw new ArgumentOutOfRangeException("HQL query not valid: please specify select clause with at least 2 columns (the first for labels, the second for values).");
            } 
            try {
                hql = _transactionManager.GetSession().CreateQuery(query);
                if (hql.ReturnAliases.Count() < 2) {
                    throw new ArgumentOutOfRangeException("HQL query not valid: please specify select clause with at least 2 columns (the first for labels, the second for values).");
                }
                result = hql.SetResultTransformer(Transformers.AliasToEntityMap).Enumerable();
            } catch (Exception ex) {
                Log.Error(ex, "RunHqlReport error - query: " + query);
                throw new ArgumentOutOfRangeException("HQL query not valid: please specify select clause with at least 2 columns (the first for labels, the second for values).");
            }

            if (hql.ReturnAliases.Count() > 2) {
                returnValue.Add("0", new AggregationResult {
                    AggregationValue = 0,
                    Label = "",
                    GroupingField = "",
                    Other = hql.ReturnAliases
                });
                int rownum = 0;
                foreach (var record in result) {
                    var row = new List<object>();
                    var ht = record as Hashtable;
                    foreach(var alias in hql.ReturnAliases) {
                        row.Add(ht[alias]);
                    }
                    rownum++;
                    returnValue.Add(rownum.ToString(), new AggregationResult {
                        AggregationValue = 0,
                        Label = "",
                        GroupingField = "",
                        Other = row.ToArray()
                    });
                }
            } else {
                foreach (var record in result) {
                    var ht = record as Hashtable;
                    string key = Convert.ToString(ht[hql.ReturnAliases[0]]);
                    double value = 0;
                    double.TryParse(Convert.ToString(ht[hql.ReturnAliases[1]]), out value);
                    if (returnValue.ContainsKey(key)) {
                        var previousItem = returnValue[key];
                        previousItem.AggregationValue += value;
                        returnValue[key] = previousItem;
                    } else {
                        returnValue[key] = new AggregationResult {
                            AggregationValue = value,
                            Label = key,
                            GroupingField = hql.ReturnAliases[0],
                            Other = null
                        };
                    }
                }
            }
            return returnValue.Values;
        }

        public IHqlQuery ApplyFilter(IHqlQuery contentQuery, string category, string type, dynamic state)
        {
            var availableFilters = projectionManager.DescribeFilters().ToList();

            // look for the specific filter component
            var descriptor = availableFilters
                .SelectMany(x => x.Descriptors)
                .FirstOrDefault(x => x.Category == category && x.Type == type);

            // ignore unfound descriptors
            if (descriptor == null)
            {
                return contentQuery;
            }

            var filterContext = new FilterContext
            {
                Query = contentQuery,
                State = state
            };

            // apply alteration
            descriptor.Filter(filterContext);

            return filterContext.Query;
        }

        public IEnumerable<IHqlQuery> GetContentQueries(QueryPartRecord queryRecord, IEnumerable<SortCriterionRecord> sortCriteria, IContent container)
        {
            Dictionary<string, object> filtersDictionary = new Dictionary<string, object>();

            if (container != null)
            {
                filtersDictionary.Add("Content", container);
            }
            
            // pre-executing all groups 
            foreach (var group in queryRecord.FilterGroups)
            {
                var contentQuery = this.contentManager.HqlQuery().ForVersion(VersionOptions.Published);

                // iterate over each filter to apply the alterations to the query object
                foreach (var filter in group.Filters)
                {
                    var tokenizedState = _tokenizer.Replace(filter.State, filtersDictionary);
                    dynamic state = FormParametersHelper.ToDynamic(tokenizedState);
                    contentQuery = this.ApplyFilter(contentQuery, filter.Category, filter.Type, state);
                }

                yield return contentQuery;
            }
        }
        public IEnumerable<GenericItem> GetReportListForCurrentUser(string titleFilter = "") {
            string filter = (titleFilter ?? "").ToLowerInvariant();
            var reportLst = new List<GenericItem>();
            var unfilteredList = GetReports().Select(x => new GenericItem {
                Id = x.Id,
                Title =  (x.Has<TitlePart>() ? x.As<TitlePart>().Title : T("[No Title]").ToString())
            });
            foreach(var report in unfilteredList) {
                if (report.Title.ToLowerInvariant().Contains(filter)) {
                    if (_authorizer.Authorize(GetReportPermissions()[report.Id])) {
                        reportLst.Add(report);
                    }
                }
            }
            return reportLst.OrderBy(x => x.Title);
        }
        public IEnumerable<GenericItem> GetDashboardListForCurrentUser(string titleFilter = "") {
            string filter = (titleFilter ?? "").ToLowerInvariant();
            var dashboardLst = new List<GenericItem>();
            var unfilteredList = contentManager.Query("DataReportDashboard").List().Select(x => new GenericItem {
                Id = x.Id,
                Title = (x.Has<TitlePart>() ? x.As<TitlePart>().Title : T("[No Title]").ToString())
            });
            foreach (var dashboard in unfilteredList) {
                if (dashboard.Title.ToLowerInvariant().Contains(filter)) {
                    if (_authorizer.Authorize(GetDashboardPermissions()[dashboard.Id])) {
                        dashboardLst.Add(dashboard);
                    }
                }
            }
            return dashboardLst.OrderBy(x => x.Title);
        }
        public IEnumerable<DataReportViewerPart> GetReports() {
            // la seguente condizione where è necessaria per ragioni di performance
            return contentManager.Query<DataReportViewerPart>().Where<DataReportViewerPartRecord>(x => true).List();
        }
        public Dictionary<int, Permission> GetReportPermissions() {
            if (_reportPermissions == null) {
                _reportPermissions = new Security.Permissions(contentManager).GetReportPermissions();
            }
            return _reportPermissions;
        }
        public Dictionary<int, Permission> GetDashboardPermissions() {
            if (_dashboardPermissions == null) {
                _dashboardPermissions = new Security.Permissions(contentManager).GetDashboardPermissions();
            }
            return _dashboardPermissions;
        }
        public string GetCsv(DataReportViewerPart part) {
            var report = _reportRepository.Table.FirstOrDefault(c => c.Id == part.Record.Report.Id);
            if (report == null) {
                return null;
            }
            IEnumerable<AggregationResult> reportData = null;
            if (string.IsNullOrWhiteSpace(report.GroupByCategory)) {
                reportData = RunHqlReport(report, part.ContentItem);
            } else {
                reportData = RunReport(report, part.ContentItem);
            }
            var rows = reportData.ToList();
            var sb = new StringBuilder();
            var text = "";
            if (rows.Count > 0) {
                if (string.IsNullOrWhiteSpace(report.GroupByCategory) && rows[0].Other != null) {
                    // multi column hql report
                    foreach (var row in rows) {
                        foreach (var col in (object[])(row.Other)) {
                            if(col != null) {
                                switch (col.GetType().Name) {
                                    case "DateTime":
                                        sb.AppendFormat("{0:yyyy-MM-dd HH:mm:ss}", col);
                                        break;
                                    case "Decimal":
                                    case "Double":
                                    case "Float":
                                        sb.Append(Convert.ToString(col, CultureInfo.InvariantCulture).Replace('.', ','));
                                        break;
                                    default:
                                        text = Convert.ToString(col);
                                        if (text.Contains(';')) {
                                            text = string.Format("\"{0}\"", text);
                                        }
                                        sb.Append(text);
                                        break;
                                }
                            }
                            sb.Append(";"); // terminatore di campo
                        }
                        sb.Append("\r\n"); // terminatore di riga
                    }
                } else {
                    // standard report
                    foreach (var row in rows) {
                        text = Convert.ToString(row.Label);
                        if (text.Contains(';')) {
                            text = string.Format("\"{0}\"", text);
                        }
                        sb.AppendFormat("{0};{1}\r\n", text, row.AggregationValue);
                    }
                }
            }
            return sb.ToString();
        }
    }
}