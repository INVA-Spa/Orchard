﻿using Laser.Orchard.ContentExtension.Services;
using Laser.Orchard.StartupConfig.RazorCodeExecution.Services;
using Laser.Orchard.StartupConfig.Services;
using Laser.Orchard.StartupConfig.ViewModels;
using Laser.Orchard.StartupConfig.WebApiProtection.Filters;
using Laser.Orchard.UsersExtensions.Filters;
using Orchard;
using Orchard.Autoroute.Models;
using Orchard.Autoroute.Services;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Handlers;
using Orchard.ContentManagement.MetaData;
using Orchard.ContentManagement.MetaData.Models;
using Orchard.Core.Contents.Settings;
using Orchard.Data;
using Orchard.Environment.Configuration;
using Orchard.Fields.Fields;
using Orchard.Localization;
using Orchard.Localization.Models;
using Orchard.Localization.Services;
using Orchard.Logging;
using Orchard.MediaLibrary.Fields;
using Orchard.Security;
using Orchard.Taxonomies.Fields;
using Orchard.Taxonomies.Models;
using Orchard.Taxonomies.Services;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Web.Hosting;
using System.Web.Http;
using OrchardCore = Orchard.Core;
using Orchard.UI.Notify;

namespace Laser.Orchard.ContentExtension.Controllers {

    [WebApiKeyFilter(true)]
    public class ContentItemController : ApiController {
        private readonly IAuthenticationService _authenticationService;
        private readonly ICsrfTokenHelper _csrfTokenHelper;
        private readonly IOrchardServices _orchardServices;
        private readonly Lazy<IAutorouteService> _autorouteService;
        private readonly ILocalizationService _localizationService;
        private readonly ICultureManager _cultureManager;
        private readonly ShellSettings _shellSettings;
        private readonly IContentDefinitionManager _contentDefinitionManager;
        private readonly ITaxonomyService _taxonomyService;
        public ILogger Logger { get; set; }
        private readonly IContentExtensionService _contentExtensionService;
        private readonly ILocalizedStringManager _localizedStringManager;
        private readonly IUtilsServices _utilsServices;
        private readonly ITransactionManager _transactionManager;
        private readonly Lazy<IEnumerable<IContentHandler>> _handlers;
        private readonly IRazorTemplateManager _razorTemplateManager;
        private readonly INotifier _notifier;
        public Localizer T { get; set; }

        public ContentItemController(
           ShellSettings shellSettings,
            INotifier notifier,
           ICsrfTokenHelper csrfTokenHelper,
           IOrchardServices orchardServices,
           IAuthenticationService authenticationService,
           IContentExtensionService contentExtensionService,
           Lazy<IAutorouteService> autorouteService,
           ILocalizationService localizationService,
           ICultureManager cultureManager,
           IUtilsServices utilsServices,
           IContentDefinitionManager contentDefinitionManager,
           ITaxonomyService taxonomyService,
           ILocalizedStringManager localizedStringManager,

           ITransactionManager transactionManager,
            Lazy<IEnumerable<IContentHandler>> handlers,
            IRazorTemplateManager razorTemplateManager
           ) {
            _razorTemplateManager = razorTemplateManager;
            _localizedStringManager = localizedStringManager;
            _taxonomyService = taxonomyService;
            _contentDefinitionManager = contentDefinitionManager;
            _shellSettings = shellSettings;
            _csrfTokenHelper = csrfTokenHelper;
            _orchardServices = orchardServices;
            T = NullLocalizer.Instance;
            _authenticationService = authenticationService;
            _contentExtensionService = contentExtensionService;
            _autorouteService = autorouteService;
            _localizationService = localizationService;
            _cultureManager = cultureManager;
            _utilsServices = utilsServices;
            Logger = NullLogger.Instance;
            _transactionManager = transactionManager;
            _handlers = handlers;
            _notifier = notifier;
        }

        public IEnumerable<IContentHandler> Handlers
        {
            get { return _handlers.Value; }
        }

        public dynamic Get(Int32 id) {
            ContentItem ContentToView;
            Response rsp = new Response();
            if (id > 0) {
                List<ContentItem> li = _orchardServices.ContentManager.GetAllVersions(id).ToList();
                if (li.Count() == 0)
                    return _utilsServices.GetResponse(ResponseType.Validation, T("No content with this Id").ToString());
                else
                    if (li.Count() == 1)
                    ContentToView = li[0];
                else
                    ContentToView = _orchardServices.ContentManager.Get(id, VersionOptions.Latest);
                if (!_orchardServices.Authorizer.Authorize(OrchardCore.Contents.Permissions.ViewContent, ContentToView))
                    if (!_contentExtensionService.HasPermission(ContentToView.ContentType, Methods.Get, ContentToView))
                        return _utilsServices.GetResponse(ResponseType.UnAuthorized);
                if (((dynamic)ContentToView).AutoroutePart != null) {
                    string tenantname = "";
                    if (string.IsNullOrWhiteSpace(_shellSettings.RequestUrlPrefix) == false) {
                        tenantname = _shellSettings.RequestUrlPrefix + "/";
                    }
                    return Redirect(Url.Content("~/" + tenantname + "WebServices/Alias?displayAlias=" + ((dynamic)ContentToView).AutoroutePart.DisplayAlias));
                }
                else {
                    throw new Exception("Method not implemented, content without AutoroutePart");
                }
            }
            else
                return _utilsServices.GetResponse(ResponseType.None, T("No content with this Id").ToString());
            //return (_utilsServices.GetResponse(ResponseType.Success));// { Message = "Invalid Token/csrfToken", Success = false, ErrorCode=ErrorCode.InvalidXSRF,ResolutionAction=ResolutionAction.Login });
        }

        /// <summary>
        /// esempio http://localhost/Laser.Orchard/expoincitta/api/Laser.Orchard.ContentExtension/Content?ContentType=User
        /// da richiamare come application/json e non come form
        /// </summary>
        /// <param name="ContentType"></param>
        /// <param name="Language"></param>
        /// <returns></returns>
        public dynamic Get(string ContentType, string Language = "it-IT") {
            // _authorizationService.TryCheckAccess(Core.Contents.Permissions.EditOwnContent, UserSimulation.Create(x), null) })

            //var currentUser = _authenticationService.GetAuthenticatedUser();
            //if (currentUser == null){
            //       return (_utilsServices.GetResponse(ResponseType.InvalidUser));// { Message = "Error: No current User", Success = false,ErrorCode=ErrorCode.InvalidUser,ResolutionAction=ResolutionAction.Login });
            //}
            //   var aa = _contentDefinitionManager.ListTypeDefinitions().Where(x => x.DisplayName.Contains("eport"));
            ContentTypeDefinition contentTypeDefinition = _contentDefinitionManager.GetTypeDefinition(ContentType);
            if (contentTypeDefinition == null) {
                Response resp = new Response() {
                    Success = false,
                    Message = T("ContentType not exist").ToString(),
                    ErrorCode = ErrorCode.Validation
                };
                return resp;
            }
            var eObj = new ExpandoObject() as IDictionary<string, Object>;

            #region Tutti i field

            foreach (ContentTypePartDefinition ctpd in contentTypeDefinition.Parts) {
                // var fields = contentTypeDefinition.Parts.SelectMany(x => x.PartDefinition.Fields).ToList();
                var fields = ctpd.PartDefinition.Fields.ToList();
                string tipofield = "";
                foreach (ContentPartFieldDefinition singleField in fields) {
                    tipofield = singleField.FieldDefinition.Name;
                    if (tipofield == typeof(TaxonomyField).Name) {

                        #region Tassonomia in Lingua

                        var taxobase = _taxonomyService.GetTaxonomyByName(singleField.Settings["TaxonomyFieldSettings.Taxonomy"]);
                        int idmaster = taxobase.Id;
                        if(taxobase.ContentItem.Has<LocalizationPart>()) {
                            if (((dynamic)taxobase.ContentItem).LocalizationPart.MasterContentItem != null) {
                                idmaster = ((dynamic)taxobase.ContentItem).LocalizationPart.MasterContentItem.Id;
                            }
                            if (((dynamic)taxobase.ContentItem).LocalizationPart.Culture.Culture != Language) {
                                taxobase = _taxonomyService.GetTaxonomies().Where(x => (x.Id == idmaster || (((dynamic)x.ContentItem).LocalizationPart.MasterContentItem != null && ((dynamic)x.ContentItem).LocalizationPart.MasterContentItem.Id == idmaster)) && ((dynamic)x.ContentItem).LocalizationPart.Culture.Culture == Language).FirstOrDefault();
                            }
                        }
                        List<TermPart> cata = _taxonomyService.GetTerms(taxobase.Id).ToList();//.GetTermsForContentItem(currentUser.Id, singleField.Name).ToList();
                        List<string> ListCategory = new List<string>();
                        ListCategory = cata.Select(x => x.Id.ToString()).ToList();
                        List<ElementDetail> elements = new List<ElementDetail>();
                        foreach (var term in taxobase.Terms) {
                            Int32? valore = term.Id;
                            Int32? mediaid = null;
                            try {
                                MediaLibraryPickerField mpf = (MediaLibraryPickerField)(term.Fields.Where(x => x.FieldDefinition.Name == "MediaLibraryPickerField").FirstOrDefault());
                                mediaid = mpf.Ids[0];
                            }
                            catch { }
                            if (!term.Selectable)
                                valore = null;
                            if (term.FullPath == "/" + term.Id.ToString() || term.FullPath == term.Id.ToString())
                                elements.Add(new ElementDetail() { Name = term.Name, Value = valore, ImageId = mediaid });
                            else {
                                Int32 idtermfather = Convert.ToInt32(term.FullPath.Split('/')[term.FullPath.Split('/').Length - 2]);
                                FindTaxoVM(elements, idtermfather).Children.Add(new ElementDetail() { Name = term.Name, Value = valore, ImageId = mediaid });
                            }
                        }
                        ResponseElement re = new ResponseElement();
                        bool solofoglie = Convert.ToBoolean(singleField.Settings["TaxonomyFieldSettings.LeavesOnly"]);
                        if (solofoglie) {
                            ElementDetail TempElement = new ElementDetail(); //elemento fittizzio per procedura ricorsiva
                            TempElement.Children = elements;
                            AnnullaNonFoglie(TempElement);
                            elements = TempElement.Children;
                        }
                        re.Values = elements;
                        re.Setting = new ResponseSetting { Type = "Taxonomie", Required = Convert.ToBoolean(singleField.Settings["TaxonomyFieldSettings.Required"]), SingleChoice = Convert.ToBoolean(singleField.Settings["TaxonomyFieldSettings.SingleChoice"]) };
                        eObj.Add(ctpd.PartDefinition.Name + "." + singleField.Name, re);

                        #endregion Tassonomia in Lingua
                    }
                    else
                        if (tipofield == typeof(EnumerationField).Name) {
                        string[] elencovalori = singleField.Settings["EnumerationFieldSettings.Options"].Split(new string[] { "\r\n" }, StringSplitOptions.None);
                        List<string> elencoValoriInLingua = new List<string>();
                        List<ElementDetail> ele = new List<ElementDetail>();
                        foreach (string val in elencovalori) {
                            ElementDetail tvm = new ElementDetail();
                            tvm.Value = val;
                            tvm.Name = _localizedStringManager.GetLocalizedString("UserEnumeratore", val, Language);
                            ele.Add(tvm);
                        }
                        ResponseElement re = new ResponseElement();
                        re.Values = ele;
                        bool singlechoise = true;
                        if (singleField.Settings["EnumerationFieldSettings.ListMode"] == "Listbox" || singleField.Settings["EnumerationFieldSettings.ListMode"] == "Checkbox")
                            singlechoise = false;
                        re.Setting = new ResponseSetting { Type = "Enumerator", Required = Convert.ToBoolean(singleField.Settings["EnumerationFieldSettings.Required"]), SingleChoice = singlechoise };
                        eObj.Add(ctpd.PartDefinition.Name + "." + singleField.Name, re);
                    }
                }
            }

            #endregion Tutti i field

            return Json(eObj);
        }

        #region private class/method for get

        private void AnnullaNonFoglie(ElementDetail myelement) {
            if (myelement.Children.Count > 0)
                myelement.Value = null;
            foreach (ElementDetail el in myelement.Children) {
                AnnullaNonFoglie(el);
            }
        }

        private class ResponseElement {

            //     public object Default { get; set; }
            public object Values { get; set; }

            public ResponseSetting Setting { get; set; }
        }

        private class ResponseSetting {
            public string Type { get; set; }
            public bool Required { get; set; }
            public bool SingleChoice { get; set; }

            public ResponseSetting() {
                Required = false;
                SingleChoice = false;
            }
        }

        private class ElementDetail {

            public ElementDetail() {
                Name = "";
                Children = new List<ElementDetail>();
            }

            public string Name { get; set; }
            public object Value { get; set; }
            public Int32? ImageId { get; set; }
            public List<ElementDetail> Children { get; set; }
        }

        private ElementDetail FindTaxoVM(List<ElementDetail> elements, Int32 idToFind) {
            if (elements != null) {
                foreach (ElementDetail myterm in elements) {
                    if ((Int32)myterm.Value == idToFind)
                        return myterm;
                    else {
                        var foundinchildren = FindTaxoVM(myterm.Children, idToFind);
                        if (foundinchildren != null)
                            return FindTaxoVM(myterm.Children, idToFind);
                    }
                }
                return null;
            }
            else
                return null;
        }

        #endregion private class/method for get

        private Response DeleteContent(Int32 id) {
            ContentItem ContentToDelete;
            Response rsp = new Response();
            if (id > 0) {
                List<ContentItem> li = _orchardServices.ContentManager.GetAllVersions(id).ToList();
                if (li.Count() == 0)
                    return _utilsServices.GetResponse(ResponseType.Validation, T("No content with this Id").ToString());
                else
                    if (li.Count() == 1)
                    ContentToDelete = li[0];
                else
                    ContentToDelete = _orchardServices.ContentManager.Get(id, VersionOptions.Latest);
                if (!_orchardServices.Authorizer.Authorize(OrchardCore.Contents.Permissions.DeleteContent, ContentToDelete))
                    if (!_contentExtensionService.HasPermission(ContentToDelete.ContentType, Methods.Delete, ContentToDelete))
                        return _utilsServices.GetResponse(ResponseType.UnAuthorized);
                try {
                    _orchardServices.ContentManager.Remove(ContentToDelete);
                    // propaga l'evento Removed per il ContentItem
                    var context = new RemoveContentContext(ContentToDelete);
                    Handlers.Invoke(handler => handler.Removed(context), Logger);
                }
                catch (Exception ex) {
                    return _utilsServices.GetResponse(ResponseType.None, ex.Message);
                }
            }
            else
                return _utilsServices.GetResponse(ResponseType.None, T("No content with this Id").ToString());
            return (_utilsServices.GetResponse(ResponseType.Success));// { Message = "Invalid Token/csrfToken", Success = false, ErrorCode=ErrorCode.InvalidXSRF,ResolutionAction=ResolutionAction.Login });
        }

        /// <summary>
        /// http://localhost/Laser.Orchard/expoincitta/Api/Laser.Orchard.ContentExtension/ContentItem/2925
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public Response Delete(Int32 id) {
            var currentUser = _orchardServices.WorkContext.CurrentUser;
            if (currentUser == null)
                return DeleteContent(id);
            else
                if (_csrfTokenHelper.DoesCsrfTokenMatchAuthToken())
                return DeleteContent(id);
            else
                return (_utilsServices.GetResponse(ResponseType.InvalidXSRF));// { Message = "Invalid Token/csrfToken", Success = false, ErrorCode=ErrorCode.InvalidXSRF,ResolutionAction=ResolutionAction.Login });
        }

        /// <summary>
        /// http://localhost/Laser.Orchard/expoincitta/Api/Laser.Orchard.ContentExtension/ContentItem/2940
        /// </summary>
        /// <param name="id"></param>
        /// <param name="eObj"></param>
        /// <returns></returns>
        public Response Put(Int32 id, ExpandoObject eObj) {
            ((dynamic)eObj).Id = id;
            return Post(eObj);
        }

        /// <summary>
        /// test in feedler
        /// User-Agent: Fiddler
        ///Content-Type: application/json
        ///Host: localhost
        ///Content-Length: 105
        ///Cookie: .ASPXAUTH=3BDDACF3339764AE73D8AF6A9992CE0C34247D3107C7E66561BCF0A8ACB81C0B0708A274B5B5A57B3C9425AA98825AF5429AE0DE9958CEC8923B998667D89184755E3446DAA5832C7C16C519ABDD1981E919AC22E1A81D277F615F3240264D5FB0B46F174EEC84EB839387462EA250CC71B856178CE26EF0EDEFD6B00E40FACF
        ///X-XSRF-TOKEN: Tnaq+qFFu+B/NPrfJZnTg5FoATHDCrTP3aAAXA90MiS1vpkk5y2QwfRJ5aAmqu4n7GFv+6CcUos+klKlOXBu1A==
        ///{"ContentType":"contenutoutente","TitlePart.Title":"Titolodiprova","sottotitolo":"il mio sottotitolo","BodyPart.Text":"<b>il mio body</b>","media":[158] }
        /// </summary>
        /// <param name="eObj"></param>
        /// <returns></returns>
        [PolicyApiFilter]
        public Response Post(ExpandoObject eObj) {
            var currentUser = _orchardServices.WorkContext.CurrentUser;
            if (currentUser == null)
                return StoreNewContentItem(eObj, null);
            else
                if (_csrfTokenHelper.DoesCsrfTokenMatchAuthToken()) {
                return StoreNewContentItem(eObj, currentUser.ContentItem);
            }
            else
                return (_utilsServices.GetResponse(ResponseType.InvalidXSRF));// { Message = "Invalid Token/csrfToken", Success = false, ErrorCode=ErrorCode.InvalidXSRF,ResolutionAction=ResolutionAction.Login });
        }

        #region private method

        /// <summary>
        /// Formato DateTimeField: 2009-06-15T13:45:30  yyyy-MM-ddThh:mm:ss NB: L’ora deve essere riferita all’ora di Greenwich
        /// </summary>
        /// <param name="eObj"></param>
        /// <param name="TheContentItem"></param>
        /// <returns></returns>
        private Response StoreNewContentItem(ExpandoObject eObj, ContentItem TheContentItem = null) {
            string tipoContent = ((dynamic)eObj).ContentType;
            Int32 IdContentToModify = 0; // new content
            // NewContent.As<TitlePart>.Title = "Creazione";
            try {
                if ((Int32)(((dynamic)eObj).Id) > 0) {
                    IdContentToModify = (Int32)(((dynamic)eObj).Id);
                }
            }
            catch {
                // Fix per Username nullo
                if (tipoContent == "User")
                    return _utilsServices.GetResponse(ResponseType.Validation, "Missing user Id");
            }
            ContentItem NewOrModifiedContent;
            Response rsp = new Response();
            string validateMessage = "";
            if (IdContentToModify > 0) {
                List<ContentItem> li = _orchardServices.ContentManager.GetAllVersions(IdContentToModify).ToList();
                if (li.Count() == 0)
                    return _utilsServices.GetResponse(ResponseType.Validation, "No content with this Id");
                else {
                    var typeSettings = li[0].TypeDefinition.Settings.TryGetModel<ContentTypeSettings>();
                    if (typeSettings.Draftable) {
                        NewOrModifiedContent = _orchardServices.ContentManager.Get(IdContentToModify, VersionOptions.DraftRequired); // quando edito estraggo sempre il draftrequired (come in Orchard.Core.Contents.Controllers)
                    }
                    else {
                        NewOrModifiedContent = _orchardServices.ContentManager.Get(IdContentToModify, VersionOptions.Latest);
                    }
                    //if (li.Count() == 1)
                    //    NewOrModifiedContent = li[0];
                    //else
                    //    NewOrModifiedContent = _orchardServices.ContentManager.Get(IdContentToModify, VersionOptions.DraftRequired); // quando edito estraggo sempre il draftrequired (come in Orchard.Core.Contents.Controllers)
                }
                if (!_orchardServices.Authorizer.Authorize(OrchardCore.Contents.Permissions.EditContent, NewOrModifiedContent))
                    if (!_contentExtensionService.HasPermission(tipoContent, Methods.Post, NewOrModifiedContent))
                        return _utilsServices.GetResponse(ResponseType.UnAuthorized);
                validateMessage = ValidateMessage(NewOrModifiedContent, "Modified");
            }
            else {
                NewOrModifiedContent = _orchardServices.ContentManager.New(tipoContent);
                if (!_orchardServices.Authorizer.Authorize(OrchardCore.Contents.Permissions.EditContent, NewOrModifiedContent)) {
                    if (!_contentExtensionService.HasPermission(tipoContent, Methods.Post))
                        return _utilsServices.GetResponse(ResponseType.UnAuthorized);
                }
                _orchardServices.ContentManager.Create(NewOrModifiedContent, VersionOptions.Draft); // quando creo creo sempre in draft (come in Orchard.Core.Contents.Controllers), se non faccio il create poi non vengono salvati i field
                validateMessage = ValidateMessage(NewOrModifiedContent, "Created");
            }
            if (string.IsNullOrEmpty(validateMessage))
                rsp = _contentExtensionService.StoreInspectExpando(eObj, NewOrModifiedContent);
            else
                rsp = _utilsServices.GetResponse(ResponseType.None, validateMessage);
            if (rsp.Success) {
                try {
                    string language = "";
                    try {
                        language = ((dynamic)eObj).Language;
                    }
                    catch { }
                    if (NewOrModifiedContent.As<LocalizationPart>() != null) {
                        if (!string.IsNullOrEmpty(language))
                            NewOrModifiedContent.As<LocalizationPart>().Culture = _cultureManager.GetCultureByName(language);
                        NewOrModifiedContent.As<LocalizationPart>().MasterContentItem = NewOrModifiedContent;
                    }
                    validateMessage = ValidateMessage(NewOrModifiedContent, "");
                    if (string.IsNullOrEmpty(validateMessage)) {
                        //    _orchardServices.ContentManager.Create(NewOrModifiedContent, VersionOptions.DraftRequired);
                    }
                    else {
                        rsp = _utilsServices.GetResponse(ResponseType.None, validateMessage);
                    }
                    if (NewOrModifiedContent.As<AutoroutePart>() != null) {
                        //           ((dynamic)NewOrModifiedContent).AutoroutePart.DisplayAlias = _autorouteService.Value.GenerateAlias(((dynamic)NewOrModifiedContent).AutoroutePart);
                        //           _autorouteService.Value.ProcessPath(((dynamic)NewOrModifiedContent).AutoroutePart);
                        //           _autorouteService.Value.PublishAlias(((dynamic)NewOrModifiedContent).AutoroutePart);
                        dynamic data = new ExpandoObject();
                        data.DisplayAlias = ((dynamic)NewOrModifiedContent).AutoroutePart.DisplayAlias;
                        data.Id = (Int32)(((dynamic)NewOrModifiedContent).Id);
                        data.ContentType = ((dynamic)NewOrModifiedContent).ContentType;
                        rsp.Data = data;
                    }
                }
                catch (Exception ex) {
                    rsp = _utilsServices.GetResponse(ResponseType.None, ex.Message);
                }
            }
            //   else {
            //try {
            //    _orchardServices.ContentManager.Remove(NewOrModifiedContent);
            //}
            //catch (Exception ex2) {
            //    rsp = _utilsServices.GetResponse(ResponseType.None, ex2.Message);
            //}
            //      }
            if (!rsp.Success)
                _transactionManager.Cancel();
            else {
                // forza il publish solo per i contenuti non draftable
                var typeSettings = NewOrModifiedContent.TypeDefinition.Settings.TryGetModel<ContentTypeSettings>();
                if ((typeSettings == null) || (typeSettings.Draftable == false)) {
                    NewOrModifiedContent.VersionRecord.Published = false; //not draftable items may have this flag set to published, and that would mean that the .Publish would not actually be executed.
                    _orchardServices.ContentManager.Publish(NewOrModifiedContent);
                }
                // propaga l'evento Updated per il ContentItem
                var context = new UpdateContentContext(NewOrModifiedContent);
                Handlers.Invoke(handler => handler.Updated(context), Logger);

                foreach (var notifi in _notifier.List()) {
                    if (notifi.Type == NotifyType.Error) {
                        _transactionManager.Cancel();
                        rsp.Success = false;
                        rsp.Message = "Error on update";
                        Logger.Error(notifi.Message.ToString());
                        break;
                    }
                }
            }
            return rsp;
        }

        //public event EventHandler ExternalContentCreated;
        private string ValidateMessage(ContentItem ci, string postfix) {
            string validate_folder = HostingEnvironment.MapPath("~/") + @"App_Data\Sites\" + _shellSettings.Name + @"\Validation\";
            if (!System.IO.Directory.Exists(validate_folder))
                System.IO.Directory.CreateDirectory(validate_folder);
            string myfile = HostingEnvironment.MapPath("~/") + @"App_Data\Sites\" + _shellSettings.Name + @"\Validation\" + ci.ContentType + postfix + ".cshtml";
            var model = new RazorModelContext {
                OrchardServices = _orchardServices,
                ContentItem = ci,
                Tokens = new Dictionary<string, object>(),
                T = T
            };
            string result = _razorTemplateManager.RunFile(myfile, model);
            string resultnobr = result.Replace("\r\n", "").Replace(" ", "");
            if (!string.IsNullOrEmpty(resultnobr)) {
                return result;
            }
            return null;
        }

        private bool TryValidate(ContentItem ci) {
            //var context = new ValidationContext(ci.Parts.FirstOrDefault(), serviceProvider: null, items: null);
            //var results = new List<ValidationResult>();
            //var isValid = Validator.TryValidateObject(ci.Parts.FirstOrDefault(), context, results);
            //if (!isValid)
            //    return false;
            //else
            return true;
        }

        #endregion private method
    }

    //internal class MyIReferenceResolver : IReferenceResolver {
    //    //public string FindLoaded(IEnumerable<string> refs, string find) {
    //    //    return refs.First(r => r.EndsWith(System.IO.Path.DirectorySeparatorChar + find));
    //    //}
    //    public IEnumerable<CompilerReference> GetReferences(TypeContext context, IEnumerable<CompilerReference> includeAssemblies) {
    //        return new[]{
    //             CompilerReference.From(HostingEnvironment.MapPath("~/")+  @"bin\Orchard.Framework.dll")
    //            //CompilerReference.From(HostingEnvironment.MapPath("~/")+  @"App_Data\Dependencies\Orchard.dll")
    //                    };
    //        // TypeContext gives you some context for the compilation (which templates, which namespaces and types)

    //        // You must make sure to include all libraries that are required!
    //        // Mono compiler does add more standard references than csc!
    //        // If you want mono compatibility include ALL references here, including mscorlib!
    //        // If you include mscorlib here the compiler is called with /nostdlib.
    //        //IEnumerable<string> loadedAssemblies = (new UseCurrentAssembliesReferenceResolver())
    //        //    .GetReferences(context, includeAssemblies)
    //        //    .Select(r => r.GetFile())
    //        //    .ToArray();

    //        //    yield return CompilerReference.From(FindLoaded(loadedAssemblies, "mscorlib.dll"));
    //        //    yield return CompilerReference.From(FindLoaded(loadedAssemblies, "System.dll"));
    //        //      yield return CompilerReference.From(FindLoaded(loadedAssemblies, "System.Core.dll"));
    //        //     yield return CompilerReference.From(typeof(MyIReferenceResolver).Assembly); // Assembly

    //        // There are several ways to load an assembly:
    //        //yield return CompilerReference.From("Path-to-my-custom-assembly"); // file path (string)
    //        //byte[] assemblyInByteArray = --- Load your assembly ---;
    //        //yield return CompilerReference.From(assemblyInByteArray); // byte array (roslyn only)
    //        //string assemblyFile = --- Get the path to the assembly ---;
    //        //yield return CompilerReference.From(File.OpenRead(assemblyFile)); // stream (roslyn only)
    //    }
    //}
}