﻿using System;
using System.Collections.Generic;
using System.Linq;
using Orchard.Autoroute.Models;
using Orchard.Autoroute.Settings;
using Orchard.ContentManagement;
using Orchard.ContentManagement.MetaData;
using Orchard.Core.Contents.Extensions;
using Orchard.Data.Migration;
using Orchard.Localization.Services;

namespace Orchard.Autoroute {
    public class Migrations : DataMigrationImpl {
        private readonly IContentManager _contentManager;
        private readonly ICultureManager _cultureManager;

        public Migrations(IContentManager contentManager, ICultureManager cultureManager) {
            _contentManager = contentManager;
            _cultureManager = cultureManager;
        }

        public int Create() {
            SchemaBuilder.CreateTable("AutoroutePartRecord",
                table => table
                    .ContentPartVersionRecord()
                            .Column<string>("CustomPattern", c => c.WithLength(2048))
                            .Column<bool>("UseCustomPattern", c => c.WithDefault(false))
                            .Column<bool>("UseCulturePattern", c => c.WithDefault(false))
                            .Column<string>("DisplayAlias", c => c.WithLength(2048)));

            ContentDefinitionManager.AlterPartDefinition("AutoroutePart", part => part
                .Attachable()
                .WithDescription("Adds advanced url configuration options to your content type to completely customize the url pattern for a content item."));

            SchemaBuilder.AlterTable("AutoroutePartRecord", table => table
                .CreateIndex("IDX_AutoroutePartRecord_DisplayAlias", "DisplayAlias")
            );

            return 4;
        }

        public int UpdateFrom1() {
            ContentDefinitionManager.AlterPartDefinition("AutoroutePart", part => part
                .WithDescription("Adds advanced url configuration options to your content type to completely customize the url pattern for a content item."));
            return 2;
        }

        public int UpdateFrom2() {

            SchemaBuilder.AlterTable("AutoroutePartRecord", table => table
                .CreateIndex("IDX_AutoroutePartRecord_DisplayAlias", "DisplayAlias")
            );

            return 3;
        }

        public int UpdateFrom3() {

            SchemaBuilder.AlterTable("AutoroutePartRecord", table => table
                .AddColumn<bool>("UseCulturePattern", c => c.WithDefault(false))
            );

            return 4;
        }

        public int UpdateFrom4() {
            // Adding some culture neutral patterns if they don't exist
            var types = _contentManager.GetContentTypeDefinitions().Where(t => t.Parts.Any(p => p.PartDefinition.Name.Equals(typeof(AutoroutePart).Name)));
            foreach (var type in types) {
                var typeDefinition = ContentDefinitionManager.GetTypeDefinition((type.Name));
                if (typeDefinition != null) {
                    var settings = typeDefinition.Parts.First(x => x.PartDefinition.Name == "AutoroutePart").Settings.GetModel<AutorouteSettings>();
                    if (!settings.Patterns.Any(x => String.IsNullOrWhiteSpace(x.Culture))) {
                        string siteCulture = _cultureManager.GetSiteCulture();
                        string newPatterns = "";
                        if (settings.Patterns.Any(x => String.Equals(x.Culture, siteCulture, StringComparison.OrdinalIgnoreCase))) {
                            var siteCulturePatterns = settings.Patterns.Where(x => String.Equals(x.Culture, siteCulture, StringComparison.OrdinalIgnoreCase)).ToList();

                            foreach (RoutePattern pattern in siteCulturePatterns) {
                                newPatterns += String.Format("{{\"Name\":\"{0}\",\"Pattern\":\"{1}\",\"Description\":\"{2}\"}},", pattern.Name, pattern.Pattern, pattern.Description);
                            }
                        }
                        else {
                            newPatterns += String.Format("{{\"Name\":\"{0}\",\"Pattern\":\"{1}\",\"Description\":\"{2}\"}},", "Title", "{Content.Slug}", "my-title");
                        }

                        string oldPatterns = typeDefinition.Parts.First(x => x.PartDefinition.Name == "AutoroutePart").Settings["AutorouteSettings.PatternDefinitions"];
                        if (oldPatterns.StartsWith("[") && oldPatterns.EndsWith("]")) {
                            if (String.Equals(oldPatterns, "[]"))
                                newPatterns = newPatterns.TrimEnd(',');
                            newPatterns = oldPatterns.Insert(1, newPatterns);
                        }
                        else
                            newPatterns = "[" + newPatterns.TrimEnd(',') + "]";

                        ContentDefinitionManager.AlterTypeDefinition(type.Name, cfg => cfg
                        .WithPart("AutoroutePart", builder => builder
                            .WithSetting("AutorouteSettings.PatternDefinitions", newPatterns)
                        ));
                    }
                }
            }

            return 5;
        }
    }
}