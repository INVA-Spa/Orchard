﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Web;
using Orchard.Core.Contents.ViewModels;

namespace Laser.Orchard.AdvancedSearch.ViewModels {
    public class ListContentsViewModelExtension : ListContentsViewModel {
        public ListContentsViewModelExtension() {
            AdvancedOptions = new AdvancedContentOptions();
        }
        public AdvancedContentOptions AdvancedOptions { get; set; }

    }
    public class AdvancedContentOptions {
        public int SelectedLanguageId { get; set; }
        public int SelectedUntranslatedLanguageId { get; set; }
        public int SelectedTermId { get; set; }
        public string SelectedOwner { get; set; }
        public string SelectedStatus { get; set; }

        public string SelectedFromDate { get; set; }
        public string SelectedToDate { get; set; }

        //[DataType(DataType.Date)]
        //public DateTime? SelectedFromDate { get;  }
        //[DataType(DataType.Date)]
        //public DateTime? SelectedToDate { get;  }

        public DateFilterOptions DateFilterType { get; set; }
        public bool HasMedia { get; set; }
        public IEnumerable<KeyValuePair<int, string>> LanguageOptions { get; set; }
        public IEnumerable<KeyValuePair<int, string>> TaxonomiesOptions { get; set; }
        public IEnumerable<KeyValuePair<string, string>> StatusOptions { get; set; }

        //used with the MayChooseToSeeOthersContent permission
        private bool _ownedByMe = true;
        public bool OwnedByMe { 
            get {return _ownedByMe;}
            set { _ownedByMe = value; }
        }

        //used with the SeesAllContent permission
        private bool _ownedByMeSeeAll = false;
        public bool OwnedByMeSeeAll {
            get { return _ownedByMeSeeAll; }
            set { _ownedByMeSeeAll = value; }
        }
    }

}