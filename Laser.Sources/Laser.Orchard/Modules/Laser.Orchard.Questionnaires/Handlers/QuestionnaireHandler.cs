﻿using Laser.Orchard.Questionnaires.Models;
using Laser.Orchard.Questionnaires.Settings;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Handlers;
using Orchard.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;

namespace Laser.Orchard.Questionnaires.Handlers {

    public class QuestionnaireHandler : ContentHandler {

        public QuestionnaireHandler(IRepository<QuestionnairePartRecord> repository) {
            Filters.Add(StorageFilter.For(repository));

            OnLoaded<QuestionnairePart>((context, part) => InitializeQuestionsToDisplay(part));
        }

        protected void InitializeQuestionsToDisplay(QuestionnairePart part) {
            // Pre load some stuff in the QuestionsToDisplay property. This way, when we do _contentManager.Get(id)
            // of an item that has the QuestionnairePart, that property has a value already. We will want to set it
            // back to null when building an editor sape, because we edit the List of QuestionRecords
            var qNumber = part.Settings
                .GetModel<QuestionnairesPartSettingVM>()
                .QuestionsSortedRandomlyNumber;
            if (qNumber > 0) {
                part.QuestionsToDisplay = Shuffle(
                    part.Questions
                        .Where(x => x.Published)
                        .ToList()
                        .ConvertAll(x => (dynamic)x))
                    .ConvertAll(x => (QuestionRecord)x)
                    .ToList()
                    .Take(qNumber)
                    .ToList();
            }

            if (part.Settings
                .GetModel<QuestionnairesPartSettingVM>()
                .RandomResponse) {
                foreach (var qr in part.Questions) {
                    qr.Answers = Shuffle(qr.Answers.ToList().ConvertAll(x => (dynamic)x))
                        .ConvertAll(x => (AnswerRecord)x).ToList();
                }
            }
        }

        protected override void BuildEditorShape(BuildEditorContext context) {
            var part = context.Content.As<QuestionnairePart>();
            if (part != null) {
                part.QuestionsToDisplay = null;
            }

            base.BuildEditorShape(context);

        }

        protected override void BuildDisplayShape(BuildDisplayContext context) {
            base.BuildDisplayShape(context);
            if (context.DisplayType == "Detail") {
                QuestionnairePart qp = context.ContentItem.As<QuestionnairePart>();

                if (qp != null) {
                    Int32 QuestionsSortedRandomlyNumber = qp.Settings.GetModel<QuestionnairesPartSettingVM>().QuestionsSortedRandomlyNumber;
                    if (QuestionsSortedRandomlyNumber > 0) {
                        qp.QuestionsToDisplay = Shuffle(qp.Questions.Where(x => x.Published).ToList().ConvertAll(x => (dynamic)x)).ConvertAll(x => (QuestionRecord)x).ToList().Take(QuestionsSortedRandomlyNumber).ToList();
                    }

                    bool RandomResponse = qp.Settings.GetModel<QuestionnairesPartSettingVM>().RandomResponse;
                    if (RandomResponse) {
                        foreach (QuestionRecord qr in qp.Questions) {
                            qr.Answers = Shuffle(qr.Answers.ToList().ConvertAll(x => (dynamic)x)).ConvertAll(x => (AnswerRecord)x).ToList();
                        }
                    }
                }
            }
        }

        private List<dynamic> Shuffle(List<dynamic> list) {
            RNGCryptoServiceProvider provider = new RNGCryptoServiceProvider();
            int n = list.Count;
            while (n > 1) {
                byte[] box = new byte[1];
                do provider.GetBytes(box);
                while (!(box[0] < n * (Byte.MaxValue / n)));
                int k = (box[0] % n);
                n--;
                dynamic value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
            return list;
        }
    }
}