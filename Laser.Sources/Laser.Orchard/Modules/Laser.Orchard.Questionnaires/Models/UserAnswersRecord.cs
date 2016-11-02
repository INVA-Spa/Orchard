﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;

namespace Laser.Orchard.Questionnaires.Models {
    public class UserAnswersRecord {
        public UserAnswersRecord() {
            AnswerDate = DateTime.UtcNow;
            AnswerRecord_Id = null;
        }
        public virtual int Id { get; set; }
        public virtual int QuestionRecord_Id { get; set; }
        public virtual int? AnswerRecord_Id { get; set; }
        public virtual string QuestionText { get; set; }
        public virtual string AnswerText { get; set; }
        public virtual int User_Id { get; set; }
        public virtual DateTime AnswerDate { get; set; }
        [Required]
        public virtual int QuestionnairePartRecord_Id { get; set; }
        [Required]
        public virtual string SessionID { get; set; }
        public virtual string Context { get; set; }
    }
}