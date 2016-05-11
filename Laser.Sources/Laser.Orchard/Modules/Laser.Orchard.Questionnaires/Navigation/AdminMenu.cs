﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard.Localization;
using Orchard.UI.Navigation;

namespace Laser.Orchard.Questionnaires.Navigation {
    public class AdminMenu : INavigationProvider {

        public string MenuName {
            get { return "admin"; }
        }

        public AdminMenu() {
            T = NullLocalizer.Instance;
        }

        public Localizer T { get; set; }

        public void GetNavigation(NavigationBuilder builder) {



            builder.Add(T("Statistics"), "10", menu => menu.LinkToFirstChild(false).Permission(Permissions.AccessStatistics)
                    .Add(item => item
                        .Caption(T("Questionnaires"))
                        .Position("0")
                        .Action("Index", "QuestionnaireStats", new { area = "Laser.Orchard.Questionnaires" })
                        .Permission(Permissions.AccessStatistics)
                    )
                    .Add(item => item
                        .Caption(T("Games"))
                        .Position("1")
                        .Action("Index", "adminranking", new { area = "Laser.Orchard.Questionnaires" })
                        .Permission(Permissions.AccessStatistics)
                        .Add(T("Total"), "1", ranking => ranking
                            .LocalNav())
                        .Add(T("Apple"), "2", ranking => ranking
                            .LocalNav())
                        .Add(T("Android"), "3", ranking => ranking 
                            .LocalNav())
                        .Add(T("Windows Phone"), "4", ranking => ranking
                            .LocalNav())
                    )   
                //.Add(T("Questionnaires"), "0", subMenu => subMenu.Permission(Permissions.AccessStatistics).LinkToFirstChild(true)
                //    .Add(T("Single choice Answers"), "0", local => local.Action("IndexUserAnswers", "Questionnaire", new { area = "Laser.Orchard.Questionnaires", type = QuestionType.SingleChoice }).Permission(Permissions.AccessStatistics).LocalNav())
                //    .Add(T("Multi choice Answers"), "1", local => local.Action("IndexUserAnswers", "Questionnaire", new { area = "Laser.Orchard.Questionnaires", type = QuestionType.MultiChoice }).Permission(Permissions.AccessStatistics).LocalNav())
                //    .Add(T("Open Answers"), "2", local => local.Action("IndexUserAnswers", "Questionnaire", new { area = "Laser.Orchard.Questionnaires", type = QuestionType.OpenAnswer }).Permission(Permissions.AccessStatistics).LocalNav())
                //)
                    );
        }


    }
}