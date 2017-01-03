﻿using Laser.Orchard.Questionnaires.Models;
using Laser.Orchard.Questionnaires.Services;
using Laser.Orchard.Questionnaires.ViewModels;
using Laser.Orchard.StartupConfig.Services;
using Laser.Orchard.StartupConfig.ViewModels;
using Laser.Orchard.StartupConfig.WebApiProtection.Filters;
using Orchard;
using Orchard.ContentManagement;
using Orchard.Data;
using Orchard.Logging;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using System.Web.Http;

namespace Laser.Orchard.Questionnaires.Controllers {
    [WebApiKeyFilter(false)]
    public class QuestionnaireResponseController : ApiController {
        private readonly IQuestionnairesServices _questionnairesServices;
        private readonly IContentManager _contentManager;
        private readonly IOrchardServices _orchardServices;
        private readonly ICsrfTokenHelper _csrfTokenHelper;
        private readonly IRepository<QuestionRecord> _repositoryQuestions;
        private readonly IRepository<AnswerRecord> _repositoryAnswer;
        private readonly IUtilsServices _utilsServices;

        public ILogger Logger { get; set; }

        public QuestionnaireResponseController(
            IQuestionnairesServices questionnairesServices
            , IContentManager contentManager
            , IOrchardServices orchardServices
             , ICsrfTokenHelper csrfTokenHelper
            , IRepository<QuestionRecord> repositoryQuestions
            , IRepository<AnswerRecord> repositoryAnswer
            , IUtilsServices utilsServices
            ) {
            _questionnairesServices = questionnairesServices;
            _contentManager = contentManager;
            _orchardServices = orchardServices;
            _csrfTokenHelper = csrfTokenHelper;
            _repositoryQuestions = repositoryQuestions;
            _repositoryAnswer = repositoryAnswer;
            Logger = NullLogger.Instance;
            _utilsServices = utilsServices;
        }

        /// <summary>
        /// esempio [{"Answered":1,"AnswerText":"cioa","Id":5,"QuestionRecord_Id":5}]
        /// </summary>
        /// <param name="Risps">elenco con valorizzati solo id della risposta scelta  nel caso di risposta semplice
        /// QuestionRecord_Id e e AnswerText nel caso di risposta con testo libero
        /// </param>
        /// <returns></returns>
        public Response Post([FromBody] List<AnswerWithResultViewModel> Risps) {
            return ExecPost(Risps);
        }
        /// <summary>
        /// esempio {"Answers":[{"Answered":1,"AnswerText":"cioa","Id":5,"QuestionRecord_Id":5}], "QuestionnaireContext":"Campagna Dicembre 2016"}
        /// </summary>
        /// <param name="Risps">elenco con valorizzati solo id della risposta scelta  nel caso di risposta semplice
        /// QuestionRecord_Id e e AnswerText nel caso di risposta con testo libero
        /// </param>
        /// <returns></returns>
        public Response Put([FromBody] ExternalAnswerWithResultViewModel data) {
            if (data != null) {
                return ExecPost(data.Answers, data.QuestionnaireContext);
            }
            else {
                return (_utilsServices.GetResponse(ResponseType.Validation, "Validation: invalid input data structure."));
            }
        }
        private Response ExecPost(List<AnswerWithResultViewModel> Risps, string QuestionnaireContext = "external") {
#if DEBUG
            Logger.Error(Request.Headers.ToString());
#endif

            if (_csrfTokenHelper.DoesCsrfTokenMatchAuthToken()) {
                var currentUser = _orchardServices.WorkContext.CurrentUser;
                if (currentUser == null) {
                    return (_utilsServices.GetResponse(ResponseType.InvalidUser));
                }
                Int32 QuestionId = 0;
                if (Risps[0].Id > 0)
                    QuestionId = _repositoryAnswer.Fetch(x => x.Id == Risps[0].Id).FirstOrDefault().QuestionRecord_Id;
                else
                    QuestionId = Risps[0].QuestionRecord_Id;
                Int32 id = _repositoryQuestions.Fetch(x => x.Id == QuestionId).FirstOrDefault().QuestionnairePartRecord_Id;

                var qp = _contentManager.Get(id).As<QuestionnairePart>();
                QuestionnaireWithResultsViewModel qVM = _questionnairesServices.BuildViewModelWithResultsForQuestionnairePart(qp);
                foreach (QuestionWithResultsViewModel qresult in qVM.QuestionsWithResults) {
                    if (qresult.QuestionType == QuestionType.OpenAnswer) {
                        foreach (AnswerWithResultViewModel Risp in Risps) {
                            if (qresult.Id == Risp.QuestionRecord_Id && !(string.IsNullOrEmpty(Risp.AnswerText))) {
                                qresult.OpenAnswerAnswerText = Risp.AnswerText;
                            }
                        }
                    }
                    else {
                        foreach (AnswerWithResultViewModel asw in qresult.AnswersWithResult) {
                            foreach (AnswerWithResultViewModel Risp in Risps) {
                                if (asw.Id == Risp.Id) {
                                    if (qresult.QuestionType == QuestionType.SingleChoice) {
                                        qresult.SingleChoiceAnswer = asw.Id;
                                    }
                                    else
                                        asw.Answered = true;
                                }
                            }
                        }
                    }
                }
                var context = new ValidationContext(qVM, serviceProvider: null, items: null);
                var results = new List<ValidationResult>();
                var isValid = Validator.TryValidateObject(qVM, context, results);
                if (!isValid) {
                    string messaggio = "";
                    foreach (var validationResult in results) {
                        messaggio += validationResult.ErrorMessage + " ";
                    }
                    return (_utilsServices.GetResponse(ResponseType.Validation, "Validation:" + messaggio));
                }
                else {
                    qVM.Context = QuestionnaireContext;
                    _questionnairesServices.Save(qVM, currentUser, HttpContext.Current.Session.SessionID);
                    return (_utilsServices.GetResponse(ResponseType.Success));
                }
            }
            else
                return (_utilsServices.GetResponse(ResponseType.InvalidXSRF));
        }
    }
}