﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Orchard;
using Orchard.Environment.Configuration;
using Orchard.Logging;
using Orchard.Tokens;
using RazorEngine;
using RazorEngine.Compilation;
using RazorEngine.Compilation.ReferenceResolver;
using RazorEngine.Configuration;
using RazorEngine.Templating;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Web.Helpers;
using System.Web.Hosting;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Xsl;
using System.Numerics;
using System.Web;
using System.Collections.Specialized;
using System.Dynamic;
using System.Data.Entity.Design.PluralizationServices;
using System.Security.Cryptography.X509Certificates;
using Laser.Orchard.ExternalContent.Settings;
using Laser.Orchard.Commons.Helpers;
using Orchard.Caching;
using Orchard.Caching.Services;
using System.Web.Script.Serialization;
using System.Xml.Schema;
using System.Configuration;
using System.Web.Configuration;
using Orchard.ContentManagement;
using Orchard.Tasks.Scheduling;
using Laser.Orchard.StartupConfig.Exceptions;

//using System.Web.Razor;




namespace Laser.Orchard.ExternalContent.Services {
    public interface IFieldExternalService : IDependency {
        dynamic GetContentfromField(Dictionary<string, object> contesto, string field, string nomexlst, FieldExternalSetting settings, string contentType = "", HttpVerbOptions httpMethod = HttpVerbOptions.GET, HttpDataTypeOptions httpDataType = HttpDataTypeOptions.JSON, string bodyRequest = "");
        string GetUrl(Dictionary<string, object> contesto, string externalUrl);
        void ScheduleNextTask(Int32 minute, ContentItem ci);
    }

    public class ExtensionObject {
        public string HtmlEncode(string input) {
            return System.Web.HttpUtility.HtmlEncode(input);
        }
        public string HtmlDecode(string input) {
            return (System.Web.HttpUtility.HtmlDecode(input)).Replace("\t", " ");
        }
        public string GuidToId(string input) {
            BigInteger huge = BigInteger.Parse('0' + input.Replace("-", "").Replace(" ", ""), NumberStyles.AllowHexSpecifier);
            return (huge + 5000000).ToString();
        }

        public string ToTitleCase(string input) {
            TextInfo myTI = new CultureInfo("en-US", false).TextInfo;
            return myTI.ToTitleCase(input);
        }

    }

    public class FieldExternalService : IFieldExternalService {
        //    private readonly ICacheStorageProvider _cacheStorageProvider;
        private readonly ITokenizer _tokenizer;
        private readonly ShellSettings _shellSetting;
        private readonly IWorkContextAccessor _workContext;
        private readonly ICacheStorageProvider _cacheStorageProvider;
        private readonly IOrchardServices _orchardServices;
        private readonly IScheduledTaskManager _scheduledTaskManager;
        private const string TaskType = "FieldExternalTask";

        public ILogger Logger { get; set; }
        public FieldExternalService(
            ITokenizer tokenizer
            , ShellSettings shellSetting
            , IWorkContextAccessor workContext
            // , ICacheStorageProvider cacheService
            , IOrchardServices orchardServices
            , IScheduledTaskManager scheduledTaskManager
            //  , ICacheStorageProvider cacheStorageProvider
            ) {
            _tokenizer = tokenizer;
            _shellSetting = shellSetting;
            _workContext = workContext;
            Logger = NullLogger.Instance;
            //   _cacheService = cacheService;
            _orchardServices = orchardServices;
            if (_orchardServices.WorkContext != null) {
                _orchardServices.WorkContext.TryResolve<ICacheStorageProvider>(out _cacheStorageProvider);
            }
            _scheduledTaskManager = scheduledTaskManager;

            //    _cacheStorageProvider = cacheStorageProvider;
        }
        public void ScheduleNextTask(Int32 minute, ContentItem ci) {
            if (minute > 0) {
                DateTime date = DateTime.UtcNow.AddMinutes(minute);
                _scheduledTaskManager.CreateTask(TaskType, date, ci);
            }
        }
        public string GetUrl(Dictionary<string, object> contesto, string externalUrl) {
            bool threatUrlAsString = false;
            string pureCleanString, finalUrl;
            var tokenizedzedUrl = _tokenizer.Replace(externalUrl, contesto, new ReplaceOptions { Encoding = ReplaceOptions.UrlEncode });
            pureCleanString = tokenizedzedUrl = tokenizedzedUrl.Replace("+", "%20");


            threatUrlAsString = !tokenizedzedUrl.StartsWith("http");
            if (threatUrlAsString) { // gestisco il caso in cui l'URl dell'externalField sia in realtà una stringa
                tokenizedzedUrl = String.Format("http://{0}/{1}/{2}", _shellSetting.RequestUrlHost ?? "www.fakedomain.com", _shellSetting.RequestUrlPrefix ?? "", tokenizedzedUrl ?? "");

            }
            Uri tokenizedzedUri;
            try {
                tokenizedzedUri = new Uri(tokenizedzedUrl);
            }
            catch {
                // gestisco il caso in cui passo un'url e non i parametri di un'url
                tokenizedzedUrl = _tokenizer.Replace(externalUrl, contesto, new ReplaceOptions { Encoding = ReplaceOptions.NoEncode });
                tokenizedzedUri = new Uri(tokenizedzedUrl);

            }
            if (threatUrlAsString) {
                finalUrl = pureCleanString.Split('?')[0];
            }
            else {
                finalUrl = String.Format("{0}{1}{2}{3}", tokenizedzedUri.Scheme, Uri.SchemeDelimiter, tokenizedzedUri.Authority, tokenizedzedUri.AbsolutePath);
            }
            var queryStringParameters = tokenizedzedUri.Query.Split('&');
            var i = 0;
            foreach (var item in queryStringParameters) {
                if (!item.Trim().EndsWith("=")) {
                    finalUrl += ((i == 0 ? "?" : "&") + item.Replace("?", ""));
                    i++;
                }

            }

            return finalUrl;
        }
        private JObject jsonflusher(JObject jsonObject) {
            JObject newJsonObject = new JObject();
            //JObject newJsonObject = new JObject();
            JProperty property;
            foreach (var token in jsonObject.Children()) {
                if (token != null) {
                    property = (JProperty)token;
                    if (property.Value.Children().Count() == 0)
                        newJsonObject.Add(property.Name.Replace(" ", ""), property.Value);
                    else if (property.Value.GetType().Name == "JArray") {
                        JArray myjarray = new JArray();
                        foreach (var arr in property.Value) {
                            if (arr.ToString() != "[]") {
                                if (arr.GetType().Name == "JValue")
                                    myjarray.Add(arr);
                                else
                                    myjarray.Add(jsonflusher((JObject)arr));
                            }

                        }
                        newJsonObject.Add(property.Name, myjarray);
                        // newJsonObject.Add(property.Name, jsonflusher((JObject)property.Value));
                    }
                    else if (property.Value.GetType().Name == "JObject") {
                        newJsonObject.Add(property.Name.Replace(" ", ""), jsonflusher((JObject)property.Value));
                    }
                }
            }
            return newJsonObject;

        }
        public dynamic GetContentfromField(Dictionary<string, object> contesto, string externalUrl, string nomexlst, FieldExternalSetting settings, string contentType = "", HttpVerbOptions httpMethod = HttpVerbOptions.GET, HttpDataTypeOptions httpDataType = HttpDataTypeOptions.JSON, string bodyRequest = "") {
            // DefaultCacheStorageProvider _cacheService = new DefaultCacheStorageProvider();
            dynamic ci = null;
            string UrlToGet = "";
            string prefix = _shellSetting.Name + "_";
            try {
                UrlToGet = GetUrl(contesto, externalUrl);
                string chiavecache = UrlToGet;
                chiavecache = Path.GetInvalidFileNameChars().Aggregate(chiavecache, (current, c) => current.Replace(c.ToString(), string.Empty));
                chiavecache = chiavecache.Replace('&', '_');
                string chiavedate = prefix + "Date_" + chiavecache;
                chiavecache = prefix + chiavecache;
                dynamic ciDate = _cacheStorageProvider.Get<object>(chiavedate);
                if (settings.CacheMinute > 0) {
                    ci = _cacheStorageProvider.Get<object>(chiavecache);
                }
                if (ci == null || ciDate == null) {
                    string certPath;
                    string webpagecontent;
                    if (settings.CertificateRequired && !String.IsNullOrWhiteSpace(settings.CerticateFileName)) {
                        certPath = String.Format(HostingEnvironment.MapPath("~/") + @"App_Data\Sites\" + _shellSetting.Name + @"\ExternalFields\{0}", settings.CerticateFileName);
                        if (File.Exists(certPath)) {
                            webpagecontent = GetHttpPage(UrlToGet, httpMethod, httpDataType, bodyRequest, certPath, settings.CertificatePrivateKey.DecryptString(_shellSetting.EncryptionKey)).Trim();
                        }
                        else {
                            throw new Exception(String.Format("File \"{0}\" not found! Upload certificate via FTP.", certPath));
                        }
                    }
                    else {
                        webpagecontent = GetHttpPage(UrlToGet, httpMethod, httpDataType, bodyRequest).Trim();
                    }
                    if (!webpagecontent.StartsWith("<")) {
                        if (webpagecontent.StartsWith("[")) {
                            webpagecontent = String.Concat("{\"", nomexlst, "List", "\":", webpagecontent, "}");
                        }

                        if (webpagecontent.Trim() == "") {
                            // fix json vuoto
                            webpagecontent = "{}";
                        }

                        JObject jsonObject = JObject.Parse(webpagecontent);
                        JObject newJsonObject = new JObject();
                        newJsonObject = jsonflusher(jsonObject);
                        webpagecontent = newJsonObject.ToString();
                        XmlDocument newdoc = new XmlDocument();
                        newdoc = JsonConvert.DeserializeXmlNode(webpagecontent, "root");
                        correggiXML(newdoc);
                        webpagecontent = newdoc.InnerXml;
                    }

                    // fix json vuoto
                    if (webpagecontent == "<root />") {

                        XmlDocument xmlDoc = new XmlDocument();
                        xmlDoc.LoadXml(webpagecontent);

                        XmlNode xmlNode = xmlDoc.DocumentElement;
                        xmlNode = XmlWithJsonArrayTag(xmlNode, xmlDoc);

                        string JsonVuoto = JsonConvert.SerializeXmlNode(xmlNode);

                        JavaScriptSerializer ser = new JavaScriptSerializer() {
                            MaxJsonLength = Int32.MaxValue
                        };
                        dynamic dynamiccontent_tmp = ser.Deserialize(JsonVuoto, typeof(object));
                        ci = new DynamicJsonObject(dynamiccontent_tmp as IDictionary<string, object>);

                    }
                    else {

                        dynamic mycache = null;
                        //    Dictionary<string, object> elementcached = null;

                        DynamicViewBag dvb = new DynamicViewBag();
                        // dvb.AddValue(settings.CacheInput, _cacheStorageProvider.Get(settings.CacheInput));
                        if (!string.IsNullOrEmpty(settings.CacheInput)) {
                            string inputcache = _tokenizer.Replace(settings.CacheInput, contesto);

                            //        var mycache = _cacheStorageProvider.Get(settings.CacheInput);
                            mycache = _cacheStorageProvider.Get<object>(inputcache);
                            // var Jsettings = new JsonSerializerSettings();
                            // Jsettings.TypeNameHandling = TypeNameHandling.Arrays;


                            //       string tmpelementcached = JsonConvert.SerializeObject(mycache);//, Jsettings);//, new JsonSerializerSettings { NullValueHandling = NullValueHandling.Ignore });
                            //       JavaScriptSerializer sera = new JavaScriptSerializer();
                            //        sera.MaxJsonLength = Int32.MaxValue;
                            //       elementcached = (Dictionary<string, object>)sera.Deserialize(tmpelementcached, typeof(object));
                            if (mycache == null) {
                                //        if (elementcached == null) {
                                if (File.Exists(String.Format(HostingEnvironment.MapPath("~/") + "App_Data/Cache/" + inputcache))) {
                                    string filecontent = File.ReadAllText(String.Format(HostingEnvironment.MapPath("~/") + "App_Data/Cache/" + inputcache));

                                    //var setdeserialize = new JsonSerializerSettings {
                                    //    TypeNameHandling = TypeNameHandling.Arrays,
                                    //    TypeNameAssemblyFormat = System.Runtime.Serialization.Formatters.FormatterAssemblyStyle.Full
                                    //};
                                    //var aaa = JsonConvert.DeserializeObject(filecontent, setdeserialize);

                                    //               JavaScriptSerializer ser = new JavaScriptSerializer();
                                    //               ser.MaxJsonLength = Int32.MaxValue;
                                    //               elementcached = (Dictionary<string, object>)ser.Deserialize(filecontent, typeof(object));
                                    mycache = JsonConvert.DeserializeObject(filecontent);
                                    _cacheStorageProvider.Put(inputcache, mycache);
                                    //                 _cacheStorageProvider.Put(settings.CacheInput, elementcached);
                                    //elementcached = JsonConvert.DeserializeObject<dynamic>(filecontent);

                                    //   elementcached new DynamicJsonObject(dynamiccontent_tmp);
                                    // elementcached = (object)JObject.Parse(filecontent);
                                    // elementcached = (object)Json.Decode(filecontent);
                                }
                            }
                        }
                        // if (elementcached == null)
                        //     elementcached.Add("NoElement", new {value="true"});
                        if (mycache != null) {
                            XmlDocument xml = JsonConvert.DeserializeXmlNode(JsonConvert.SerializeObject(mycache));
                            dvb.AddValue("CachedData", xml);
                        }
                        else {
                            dvb.AddValue("CachedData", new XmlDocument());
                        }

                        //     dvb.AddValue("CachedData", elementcached);
                        //                    if (elementcached != null) {
                        //              string a = codifica((Dictionary<string,object>)elementcached);
                        //                    }

                        ci = RazorTransform(webpagecontent.Replace(" xmlns=\"\"", ""), nomexlst, contentType, dvb);

                        _cacheStorageProvider.Remove(chiavecache);
                        _cacheStorageProvider.Remove(chiavedate);
                        if (settings.CacheMinute > 0) {
                            _cacheStorageProvider.Put(chiavecache, (object)ci);

                            // Use TimeSpan constructor to specify:
                            // ... Days, hours, minutes, seconds, milliseconds.
                            _cacheStorageProvider.Put(chiavedate, new { When = DateTime.UtcNow }, new TimeSpan(0, 0, settings.CacheMinute, 0, 0));
                        }
                        if (settings.CacheToFileSystem) {
                            if (!Directory.Exists(HostingEnvironment.MapPath("~/") + "App_Data/Cache"))
                                Directory.CreateDirectory(HostingEnvironment.MapPath("~/") + "App_Data/Cache");
                            //var Jsettings = new JsonSerializerSettings();
                            //Jsettings.TypeNameHandling = TypeNameHandling.Arrays;
                            using (StreamWriter sw = File.CreateText(String.Format(HostingEnvironment.MapPath("~/") + "App_Data/Cache/" + chiavecache))) {
                                sw.WriteLine(JsonConvert.SerializeObject(ci));//, Jsettings));// new JsonSerializerSettings {  EmptyArrayHandling = EmptyArrayHandling.Set }));
                            }
                        }
                    }
                }
            }
            catch (Exception ex) {
                Logger.Error(ex, UrlToGet);
                throw new ExternalFieldRemoteException();
            }
            return (ci);
        }


        //   private string codifica(object o) {

        //if (myval!=null){


        //    foreach(string key in myval.Keys){
        //        if (!string.IsNullOrEmpty(key)){
        //            if (myval[key]!=null){
        //                if (myval[key].GetType() == typeof(String)  ){
        //                    if (key=="Sid"){
        //                        testo+="<VenueId>VenueId:" +myval[key].ToString().Substring(6) + "</VenueId>";
        //                    }else{
        //                            testo+="<"+key+"><![CDATA[" +myval[key] + "]]></"+key+">";
        //                    }
        //                }
        //                else{
        //                    if  (myval[key].GetType() == typeof(Decimal) || myval[key].GetType() == typeof(int)){
        //                        testo+="<"+key+">" +myval[key].ToString().Replace(",",".") + "</"+key+">";
        //                    }
        //                    else{
        //                        Type t = myval[key].GetType();
        //                        bool isDict = t.IsGenericType && t.GetGenericTypeDefinition() == typeof(Dictionary<,>);
        //                        if (isDict) {
        //                            Dictionary<string, object> child = (Dictionary<string, object>)myval[key];
        //                            testo += "<" + key + ">'" + codifica(child) + "'</" + key + ">";
        //                        }
        //                        else {
        //                            try{
        //                            var child = (object[])myval[key];
        //                            testo += "<" + key + ">";
        //                            for (Int32 i = 0; i < child.Length; i++) {
        //                                if (child[i] != null) {
        //                                    testo += codifica((Dictionary<string, object>)child[i]);
        //                                }
        //                            }
        //                            testo += "</" + key + ">";
        //                            }catch{}
        //                        }
        //                    }
        //                }
        //            }
        //        }
        //    }
        //}
        //	return testo;
        //	
        //   }

        //private static dynamic XmlToDynamic(XmlReader file, XElement node = null) {
        ////    if (String.IsNullOrWhiteSpace(file) && node == null) return null;
        //     if (file==null && node == null) return null;
        //    // If a file is not empty then load the xml and overwrite node with the
        //    // root element of the loaded document
        //   // node = !String.IsNullOrWhiteSpace(file) ? XDocument.Load(file).Root : node;
        //    node =!(file==null)? XDocument.Load(file).Root : node;
        //    IDictionary<String, dynamic> result = new ExpandoObject();

        //    // implement fix as suggested by [ndinges]
        //    var pluralizationService =
        //        PluralizationService.CreateService(CultureInfo.CreateSpecificCulture("en-us"));

        //    // use parallel as we dont really care of the order of our properties
        //    node.Elements().AsParallel().ForAll(gn => {
        //        // Determine if node is a collection container
        //        var isCollection = gn.HasElements &&
        //            (
        //            // if multiple child elements and all the node names are the same
        //                gn.Elements().Count() > 1 &&
        //                gn.Elements().All(
        //                    e => e.Name.LocalName.ToLower() == gn.Elements().First().Name.LocalName) ||

        //                // if there's only one child element then determine using the PluralizationService if
        //            // the pluralization of the child elements name matches the parent node. 
        //                gn.Name.LocalName.ToLower() == pluralizationService.Pluralize(
        //                    gn.Elements().First().Name.LocalName).ToLower()
        //            );

        //        // If the current node is a container node then we want to skip adding
        //        // the container node itself, but instead we load the children elements
        //        // of the current node. If the current node has child elements then load
        //        // those child elements recursively
        //        var items = isCollection ? gn.Elements().ToList() : new List<XElement>() { gn };

        //        var values = new List<dynamic>();

        //        // use parallel as we dont really care of the order of our properties
        //        // and it will help processing larger XMLs
        //        items.AsParallel().ForAll(i => values.Add((i.HasElements) ?
        //           XmlToDynamic(null, i) : i.Value.Trim()));

        //        // Add the object name + value or value collection to the dictionary
        //        result[gn.Name.LocalName] = isCollection ? values : values.FirstOrDefault();
        //    });
        //    return result;
        //}
        public List<XmlNode> listanodi = new List<XmlNode>();
        private void correggiXML(XmlDocument xml) {

            foreach (XmlNode nodo in xml.ChildNodes) {
                doiteratenode(nodo, xml);
            }
            foreach (XmlNode sottonodo in listanodi) {
                int n;
                bool isNumeric = int.TryParse(sottonodo.Name, out n);
                if (isNumeric) {
                    RenameNode(xml, sottonodo, "lasernumeric");
                }
                if (sottonodo.Name.ToLower() == "description" || sottonodo.Name.ToLower() == "abstract" || sottonodo.Name.ToLower() == "extension") {
                    RenameNode(xml, sottonodo, sottonodo.Name + "text");
                }
            }

        }
        private void doiteratenode(XmlNode nodo, XmlDocument xml) {
            foreach (XmlNode sottonodo in nodo.ChildNodes) {
                int n;
                bool isNumeric = int.TryParse(sottonodo.Name, out n);
                if (isNumeric || sottonodo.Name.ToLower() == "description" || sottonodo.Name.ToLower() == "abstract" || sottonodo.Name.ToLower() == "extension") {
                    listanodi.Add(sottonodo);
                    // RenameNode(xml, sottonodo, "lasernumeric");
                }
                doiteratenode(sottonodo, xml);
            }
        }
        void RenameNode(XmlDocument doc, XmlNode e, string newName) {
            XmlNode newNode = doc.CreateNode(e.NodeType, newName, null);
            while (e.HasChildNodes) {
                newNode.AppendChild(e.FirstChild);
            }
            XmlAttributeCollection ac = e.Attributes;
            while (ac.Count > 0) {
                newNode.Attributes.Append(ac[0]);
            }
            XmlNode parent = e.ParentNode;
            parent.ReplaceChild(newNode, e);
        }

        private dynamic RazorTransform(string xmlpage, string xsltname, string contentType = "", DynamicViewBag dvb = null) {
            string output = "";

            string myfile = HostingEnvironment.MapPath("~/") + @"App_Data\Sites\" + _shellSetting.Name + @"\Xslt\" + contentType + xsltname + ".cshtml";
            if (!System.IO.File.Exists(myfile)) {
                myfile = HostingEnvironment.MapPath("~/") + @"App_Data\Sites\" + _shellSetting.Name + @"\Xslt\" + xsltname + ".cshtml";
            }
            if (System.IO.File.Exists(myfile)) {
                string mytemplate = File.ReadAllText(myfile);
                string myfile2 = HostingEnvironment.MapPath("~/") + @"App_Data\Sites\common.cshtml";
                if (System.IO.File.Exists(myfile2)) {
                    mytemplate = File.ReadAllText(myfile2) + mytemplate; ;
                }
                if (!string.IsNullOrEmpty(mytemplate)) {
                    var config = new TemplateServiceConfiguration();
                    config.Namespaces.Add("Orchard");
                    config.Namespaces.Add("Orchard.ContentManagement");
                    config.Namespaces.Add("Orchard.Caching");
                    string result = "";
                    var docwww = XDocument.Parse(xmlpage);


                    //temp er = new temp();
                    //    XmlDocument mydoc = er.ToXmlDocument(docwww);
                    //er.GetChildValue(mydoc, "//event/descriptiontext", true);
                    //    mydoc = er.ConvertArrayOfStringToString(mydoc, "/event", "descriptiontext", "Descriptiontext", true);


                    //      er.ConvertStringTimeStampToDate(mydoc, "/endDateTime");

                    //foreach (XmlNode bookToModify in mydoc.SelectNodes("/root/_data/lasernumeric/film")) {
                    //    if (!bookToModify.HasChildNodes) {
                    //        bookToModify.ParentNode.ParentNode.RemoveChild(bookToModify.ParentNode);
                    //    }
                    //}
                    //    mydoc = er.aggiungipadre(mydoc, "root", "_dataList", "_data");
                    //    mydoc = er.aggiungipadre(mydoc, "root/_dataList/_data/media", "ImageList", "image");
                    //    mydoc = er.rendinumeric(mydoc, "root/_dataList/_data/id");
                    //    mydoc = er.cambianome(mydoc, "root/_dataList/_data/id", "OriginalId");
                    ////    mydoc = er.RimuoviNodo(mydoc, "root/_dataList");
                    //    mydoc = er.aggiungifiglio(mydoc, "root/_dataList/_data/media/ImageList", "image");
                    //    mydoc = er.RimuoviAlberaturaTranne(mydoc, "root/_dataList");

                    //        er.SpanXDocument(er.ToXDocument(mydoc).Root);
                    using (var service = RazorEngineService.Create(config)) {
                        //DynamicViewBag vb = new DynamicViewBag();
                        //vb.AddValue("WorkContext", _workContext);
                        //foreach(string 
                        //vb.AddValue(
                        //    dvb.Add
                        //    vb.AddValue("CacheService", _cacheService); 

                        result = service.RunCompile(mytemplate, "htmlRawTemplatea", null, docwww, dvb);
                    }
                    output = result.Replace("\r\n", "");
                    //if (!string.IsNullOrEmpty(resultnobr)) {

                    // }
                }
                else
                    output = "";
                while (output.StartsWith("\t")) {
                    output = output.Substring(1);
                }

                string xml = RemoveAllNamespaces(output);

                XmlDocument doc = new XmlDocument();
                doc.LoadXml(xml);

                //  doc.DocumentElement.SetAttribute("xmlns:json", "http://www.w3.org/1999/xhtml");

                XmlNode newNode = doc.DocumentElement;




                //foreach (XmlNode node in newNode){
                //    foreach (XmlNode childNode in node.ChildNodes) {
                //    }
                //}

                //XmlNode thenewnode = doc.CreateElement("ToRemove");
                //thenewnode.AppendChild(newNode);

                newNode = XmlWithJsonArrayTag(newNode, doc);



                //XmlDocument docc = new XmlDocument();
                //XmlSchema schema = new XmlSchema();
                //schema.Namespaces.Add("xmlns:json", "http://www.w3.org/1999/xhtml");
                //docc.Schemas.Add(schema);
                //docc.ImportNode(newNode,true);

                // string JsonDataxxx = JsonConvert.SerializeXmlNode(doc);


                string JsonData = JsonConvert.SerializeXmlNode(newNode);
                JsonData = JsonData.Replace(",{}]}", "]}");

                JsonData = JsonData.Replace("\":lasernumeric", "");
                JsonData = JsonData.Replace("lasernumeric:\"", "");
                JsonData = JsonData.Replace("\":laserboolean", "");
                JsonData = JsonData.Replace("laserboolean:\"", "");
                JsonData = JsonData.Replace(@"\r\n", "");
                JsonData = JsonData.Replace("\":laserDate", "\"\\/Date(");
                JsonData = JsonData.Replace("laserDate:\"", ")\\/\"");

                JavaScriptSerializer ser = new JavaScriptSerializer() {
                    MaxJsonLength = Int32.MaxValue
                };
                dynamic dynamiccontent_tmp = ser.Deserialize(JsonData, typeof(object));
                dynamic dynamiccontent = new DynamicJsonObject(dynamiccontent_tmp as IDictionary<string, object>);

                // return  Json.Decode(JsonData);
                //  return JObject.Parse(JsonData);
                return dynamiccontent;


            }
            else {
                return XsltTransform(xmlpage, xsltname, contentType);
            }
        }

        private XmlNode XmlWithJsonArrayTag(XmlNode xn, XmlDocument doc) {
            bool ForceChildBeArray = false;
            if (xn.ChildNodes.Count > 1) {
                if (xn.ChildNodes[0].Name == xn.ChildNodes[1].Name && xn.ChildNodes[0].Name != "ToRemove") {
                    ForceChildBeArray = true;
                }
            }



            //   foreach( XmlNode iteratenode in xn.ChildNodes) {
            // for (Int32 i = xn.ChildNodes.Count - 1; i >= 0; i--) {// XmlNode iteratenode in xn.ChildNodes) {
            for (Int32 i = 0; i < xn.ChildNodes.Count; i++) {
                if (ForceChildBeArray) {
                    XmlAttribute xattr = doc.CreateAttribute("json", "Array", "http://james.newtonking.com/projects/json");
                    xattr.Value = "true";
                    xn.ChildNodes[i].Attributes.Append(xattr);
                }
                if (xn.ChildNodes[i].HasChildNodes) {
                    XmlNode childnode = XmlWithJsonArrayTag(xn.ChildNodes[i], doc).Clone();
                    if (!string.IsNullOrEmpty(childnode.InnerText)) {
                        xn.InsertBefore(childnode, xn.ChildNodes[i]);
                    }
                    xn.ChildNodes[i].ParentNode.RemoveChild(xn.ChildNodes[i]);
                }
                // for (Int32 i = xn.ChildNodes.Count - 1; i >= 0; i--) {// XmlNode iteratenode in xn.ChildNodes) {
                // XmlNode childnode = XmlWithJsonArrayTag(xn.ChildNodes[i], doc);
                // xn.ChildNodes[i].ParentNode.RemoveChild(xn.ChildNodes[i]);
                // if (!string.IsNullOrEmpty(childnode.InnerText)) {
                //     xn.AppendChild(childnode);
                // }
            }
            //    }

            return xn;

        }





        private dynamic XsltTransform(string xmlpage, string xsltname, string contentType = "") {
            string output = "", myXmlFileMoreSpecific, myXmlFileLessSpecific, myXmlFile;
            var namespaces = this.GetType().FullName.Split('.').AsEnumerable();
            namespaces = namespaces.Except(new string[] { this.GetType().Name });
            namespaces = namespaces.Except(new string[] { namespaces.Last() });
            var area = string.Join(".", namespaces);
            // se esiste un xslt chiamato {ContentType}.{FieldName}.xslt ha priorità rispetto agli altri
            myXmlFile = myXmlFileLessSpecific = HostingEnvironment.MapPath("~/") + @"App_Data\Sites\" + _shellSetting.Name + @"\Xslt\" + xsltname + ".xslt";
            myXmlFileMoreSpecific = HostingEnvironment.MapPath("~/") + @"App_Data\Sites\" + _shellSetting.Name + @"\Xslt\" + contentType + "." + xsltname + ".xslt";
            if (File.Exists(myXmlFileMoreSpecific)) {
                myXmlFile = myXmlFileMoreSpecific;
            }

            if (File.Exists(myXmlFile)) {
                // xmlpage = @"<xml/>";

                XmlReader myXPathDoc = XmlReader.Create(new StringReader(xmlpage));

                myXPathDoc.Read();

                // XPathDocument myXPathDoc = new XPathDocument(new StringReader(xmlpage));
                XsltArgumentList argsList = new XsltArgumentList();

                argsList.AddExtensionObject("my:HttpUtility", new ExtensionObject());


                string cult = _workContext.GetContext().CurrentCulture;
                if (String.IsNullOrEmpty(cult))
                    cult = "it";
                else
                    cult = cult.Substring(0, 2);


                argsList.AddParam("LinguaParameter", "", cult);

                var allrequest = _workContext.GetContext().HttpContext.Request.QueryString.Keys;

                for (var i = 0; i < allrequest.Count; i++) {
                    string _key = allrequest[i];
                    string _value = _workContext.GetContext().HttpContext.Request.QueryString[_key].ToString();
                    argsList.AddParam(_key.ToLower().Trim(), "", _value);
                }


                XsltSettings settings = new XsltSettings();
                settings.EnableScript = true;

                XslCompiledTransform myXslTrans;
                var enableXsltDebug = false;
#if DEBUG
                enableXsltDebug = true;
#endif
                myXslTrans = new XslCompiledTransform(enableXsltDebug);
                myXslTrans.Load(myXmlFile, settings, new XmlUrlResolver());

                StringWriter sw = new StringWriter();
                XmlWriter xmlWriter = new XmlTextWriter(sw);
                myXslTrans.Transform(myXPathDoc, argsList, xmlWriter);

                output = sw.ToString();
            }
            else {
                output = xmlpage;
                Logger.Error("file not exist ->" + myXmlFile);
            }
            string xml = RemoveAllNamespaces(output);
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(xml);
            XmlNode newNode = doc.DocumentElement;
            string JsonData = JsonConvert.SerializeXmlNode(newNode);
            JsonData = JsonData.Replace("\":lasernumeric", "");
            JsonData = JsonData.Replace("lasernumeric:\"", "");
            JsonData = JsonData.Replace("\":laserboolean", "");
            JsonData = JsonData.Replace("laserboolean:\"", "");
            JsonData = JsonData.Replace(@"\r\n", "");
            JsonData = JsonData.Replace("\":laserDate", "\"\\/Date(");
            JsonData = JsonData.Replace("laserDate:\"", ")\\/\"");
            // dynamic dynamiccontent = Json.Decode(JsonData, typeof(object));
            //dynamic dynamiccontent = (object)JObject.Parse(JsonData);
            //dynamic dynamiccontent = JsonConvert.DeserializeObject<dynamic>(JsonData);
            JavaScriptSerializer ser = new JavaScriptSerializer();
            ser.MaxJsonLength = Int32.MaxValue;
            dynamic dynamiccontent_tmp = ser.Deserialize(JsonData, typeof(object));
            dynamic dynamiccontent = new DynamicJsonObject(dynamiccontent_tmp);



            return dynamiccontent;
        }


        private static string GetHttpPage(string uri, HttpVerbOptions httpMethod, HttpDataTypeOptions httpDataType, string bodyRequest, string certificatePath = null, string privateKey = null) {

            //Uri uri = new Uri("https://mysite.com/auth");
            //HttpWebRequest request = (HttpWebRequest)WebRequest.Create(uri) as HttpWebRequest;
            //request.Accept = "application/xml";

            //// authentication
            //var cache = new CredentialCache();
            //cache.Add(uri, "Basic", new NetworkCredential("user", "secret"));
            //request.Credentials = cache;

            //ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(AcceptAllCertifications);

            //// response.
            //HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            Stream dataStream = null;
            String strResult;
            WebResponse objResponse;
            HttpWebRequest objRequest = (HttpWebRequest)WebRequest.Create(uri);
            if (certificatePath != null) {
                var bytes = File.ReadAllBytes(certificatePath);
                X509Certificate2 cert = new X509Certificate2(bytes, privateKey);
                objRequest.ClientCertificates.Add(cert);
            }
            objRequest.Headers.Add(HttpRequestHeader.ContentEncoding, "gzip");
            //    objRequest.Method = WebRequestMethods.Http.Get;
            //  objRequest.Accept = "application/json";
            // HttpWebRequest
            //  objRequest.UseDefaultCredentials = true;
            //objRequest.Accept
            objRequest.Method = httpMethod.ToString();

            // valore di default del content type
            objRequest.ContentType = "application/x-www-form-urlencoded";

            if (httpMethod == HttpVerbOptions.POST) {
                if (httpDataType == HttpDataTypeOptions.JSON) {
                    // JSON
                    objRequest.ContentType = "application/json; charset=utf-8";
                }

                // body del post
                byte[] buffer = System.Text.UTF8Encoding.UTF8.GetBytes(bodyRequest);
                dataStream = objRequest.GetRequestStream();
                dataStream.Write(buffer, 0, buffer.Length);
                dataStream.Close();
            }

            objRequest.PreAuthenticate = false;
            objResponse = objRequest.GetResponse();
            using (StreamReader sr = new StreamReader(objResponse.GetResponseStream())) {
                strResult = sr.ReadToEnd();
                sr.Close();
            }
            //var eliminoacapo = strResult.Split(new string[] { ">\\r\\n" }, StringSplitOptions.None);
            //strResult = string.Join(">", eliminoacapo);
            return strResult;
        }

        private static string RemoveAllNamespaces(string xmlDocument) {
            XElement xmlDocumentWithoutNs = RemoveAllNamespaces(XElement.Parse(xmlDocument));
            return xmlDocumentWithoutNs.ToString();
        }

        //Core recursion function
        private static XElement RemoveAllNamespaces(XElement xmlDocument) {
            if (!xmlDocument.HasElements) {
                XElement xElement = new XElement(xmlDocument.Name.LocalName);
                xElement.Value = xmlDocument.Value;

                foreach (XAttribute attribute in xmlDocument.Attributes())
                    xElement.Add(attribute);

                return xElement;
            }
            return new XElement(xmlDocument.Name.LocalName, xmlDocument.Elements().Select(el => RemoveAllNamespaces(el)));
        }

    }

    public class DynamicXml : DynamicObject {
        XElement _root;
        private DynamicXml(XElement root) {
            _root = root;
        }

        public static DynamicXml Parse(string xmlString) {
            return new DynamicXml(XDocument.Parse(xmlString).Root);
        }

        public static DynamicXml Load(string filename) {
            return new DynamicXml(XDocument.Load(filename).Root);
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result) {
            result = null;

            var att = _root.Attribute(binder.Name);
            if (att != null) {
                result = att.Value;
                return true;
            }

            var nodes = _root.Elements(binder.Name);
            if (nodes.Count() > 1) {
                result = nodes.Select(n => new DynamicXml(n)).ToList();
                return true;
            }

            var node = _root.Element(binder.Name);
            if (node != null) {
                if (node.HasElements) {
                    result = new DynamicXml(node);
                }
                else {
                    result = node.Value;
                }
                return true;
            }

            return true;
        }








    }
}