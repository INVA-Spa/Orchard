﻿using Orchard.ContentManagement.MetaData;
using Orchard.Core.Contents.Extensions;
using Orchard.Data.Migration;

namespace Laser.Orchard.CommunicationGateway {

    public class CoomunicationMigrations : DataMigrationImpl {
        //  private readonly IUtilsServices _utilServices;

        //public Migrations(IUtilsServices utilsServices)
        //{
        //    _utilServices = utilsServices;
        //}

        /// <summary>
        /// This executes whenever this module is activated.
        /// </summary>
        public int Create() {
            ContentDefinitionManager.AlterPartDefinition(
              "QueryFilterPart",
              b => b
              .Attachable(false)
              );

            ContentDefinitionManager.AlterPartDefinition(
                "CommunicationCampaignPart",
                 b => b
                    .Attachable(false)
                    .WithField("FromDate", cfg => cfg.OfType("DateTimeField").WithDisplayName("From Date"))
                    .WithField("ToDate", cfg => cfg.OfType("DateTimeField").WithDisplayName("To Date"))
            );
            ContentDefinitionManager.AlterTypeDefinition(
              "CommunicationCampaign",
              type => type
                  .WithPart("TitlePart")
                  //.WithPart("AutoroutePart", p => p
                  //  .WithSetting("AutorouteSettings.AllowCustomPattern", "true")
                  //  .WithSetting("AutorouteSettings.AutomaticAdjustmentOnEdit", "false")
                  //  .WithSetting("AutorouteSettings.PatternDefinitions", @"[{Name:'Title', Pattern:'{Content.Slug}',Description:'Title of campaign'}]")
                  //  .WithSetting("AutorouteSettings.DefaultPatternIndex", "0")
                  //     )
                .WithPart("IdentityPart")
                .WithPart("CommunicationCampaignPart")
                  // .WithPart("LocalizationPart")
                .WithPart("CommonPart")
                .Creatable(false)
                .DisplayedAs("Campaign")
              );
            ContentDefinitionManager.AlterPartDefinition(
   "CommunicationAdvertisingPart",
    b => b
       .Attachable(false)
       .WithField("Campaign", cfg => cfg
           .OfType("ContentPickerField")
               .WithSetting("ContentPickerFieldSettings.Hint", "Select a Campaign.")
               .WithSetting("ContentPickerFieldSettings.Required", "False")
               .WithSetting("ContentPickerFieldSettings.Multiple", "False")
               .WithSetting("ContentPickerFieldSettings.ShowContentTab", "True")
               .WithSetting("ContentPickerFieldSettings.ShowSearchTab", "True")
               .WithSetting("ContentPickerFieldSettings.DisplayedContentTypes", "Campaign")
               .WithDisplayName("Campaign")
               .WithSetting("ContentPartSettings.Attachable", "True")
           )
       .WithField("ContentLinked", cfg => cfg
           .OfType("ContentPickerField")
               .WithSetting("ContentPickerFieldSettings.Hint", "Select a ContentItem.")
               .WithSetting("ContentPickerFieldSettings.Required", "False")
               .WithSetting("ContentPickerFieldSettings.Multiple", "False")
               .WithSetting("ContentPickerFieldSettings.ShowContentTab", "True")
               .WithSetting("ContentPickerFieldSettings.ShowSearchTab", "True")
               .WithSetting("ContentPickerFieldSettings.DisplayedContentTypes", "")
               .WithDisplayName("Content")
               .WithSetting("ContentPartSettings.Attachable", "True")
           )
       .WithField("Gallery", cfg => cfg
           .OfType("MediaLibraryPickerField")
            .WithDisplayName("Gallery")
            .WithSetting("MediaLibraryPickerFieldSettings.Required", "false")
            .WithSetting("MediaLibraryPickerFieldSettings.Multiple", "false")
            .WithSetting("MediaLibraryPickerFieldSettings.DisplayedContentTypes", "Image")
            .WithSetting("MediaLibraryPickerFieldSettings.AllowedExtensions", "jpg jpeg png gif")
            .WithSetting("MediaLibraryPickerFieldSettings.Hint", "Insert Image")
           )
        );
            ContentDefinitionManager.AlterTypeDefinition(
            "CommunicationAdvertising",
            type => type
               .WithPart("TitlePart")
                //      .WithPart("BodyPart")
                .WithPart("AutoroutePart", p => p
                   .WithSetting("AutorouteSettings.AllowCustomPattern", "false")
                   .WithSetting("AutorouteSettings.AutomaticAdjustmentOnEdit", "false")
                   .WithSetting("AutorouteSettings.PatternDefinitions", @"[{Name:'Title', Pattern:'{Content.Slug}',Description:'Title of Advertising'}]")
                   .WithSetting("AutorouteSettings.DefaultPatternIndex", "0")
                      )
              .WithPart("IdentityPart")
              .WithPart("CommunicationAdvertisingPart")
              .WithPart("LocalizationPart")
              .WithPart("CommonPart")
              .WithPart("PublishLaterPart")
              .WithPart("QueryFilterPart")
                //  .WithPart("FacebookPostPart")
                //  .WithPart("TwitterPostPart")
              .Creatable(false)
               .Draftable(true)
              .DisplayedAs("Advertising")

            );
            return 1;
        }

        public int UpdateFrom1() {
            return 2;
        }

        public int UpdateFrom2() {
            ContentDefinitionManager.AlterTypeDefinition(
            "CommunicationAdvertising",
            type => type
                .WithPart("TagsPart")
                );
            return 3;
        }

        public int UpdateFrom3() {
            ContentDefinitionManager.AlterPartDefinition(
                "CommunicationAdvertisingPart",
                b => b
                .WithField("UrlLinked", cfg => cfg
                    .WithSetting("LinkFieldSettings.LinkTextMode", "Static")
                .OfType("LinkField"))
                );
            return 4;
        }

        public int UpdateFrom4() {
            ContentDefinitionManager.AlterPartDefinition(
              "CommunicationContactPart",
               b => b
               .Attachable(false)
            );
            SchemaBuilder.CreateTable("CommunicationContactPartRecord",
                table => table
                    .ContentPartRecord()
            );

            ContentDefinitionManager.AlterTypeDefinition(
                   "CommunicationContact", type => type
                       .WithPart("TitlePart")
                       .WithPart("CommonPart")
                       .WithPart("IdentityPart")
                       .WithPart("CommunicationContactPart")
                       .WithPart("ProfilePart")
                       .Creatable(false)
                       .Draftable(false)
                   );
            return 5;
        }
    }
}