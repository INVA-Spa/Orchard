﻿using Orchard.ContentManagement.MetaData;
using Orchard.Core.Contents.Extensions;
using Orchard.Data;
using Orchard.Data.Migration;
using Orchard.Data.Migration.Schema;
using System;
using System.Linq;
using System.Data;

namespace Laser.Orchard.Reporting
{
    public class Migrations : DataMigrationImpl
    {
        public int Create()
        {
            // Create ReportRecord table
            SchemaBuilder.CreateTable("ReportRecord", table => table
                .Column<int>("Id", c => c.Identity().PrimaryKey())
                .Column<string>("Title", c => c.WithLength(100).NotNull())
                .Column<string>("Name", c => c.WithLength(100).NotNull())
                .Column<int>("Query_Id", c => c.NotNull())
                .Column<int>("ChartType", c => c.NotNull())
                .Column<int>("AggregateMethod", c => c.NotNull())
                .Column<string>("State", c => c.Unlimited())
                .Column<string>("GroupByCategory", c => c.WithLength(100).NotNull())
                .Column<string>("GroupByType", c => c.WithLength(100).NotNull()));

            // Create DataReportViewerPartRecord table
            SchemaBuilder.CreateTable("DataReportViewerPartRecord", table => table
                .ContentPartRecord()
                .Column<int>("Report_Id", c => c.Nullable())
                .Column<string>("ContainerTagCssClass", c=>c.Nullable().WithLength(100))
                .Column<string>("ChartTagCssClass", c=>c.Nullable().WithLength(100)));

            ContentDefinitionManager.AlterPartDefinition("DataReportViewerPart", builder => builder.Attachable());

            ContentDefinitionManager.AlterTypeDefinition("DataReportViewer", cfg => cfg
              .WithPart("CommonPart")
              .WithPart("TitlePart")
              .WithPart("DataReportViewerPart")
              .Creatable()
              .Listable()
              .DisplayedAs("Data Report Viewer"));

            ContentDefinitionManager.AlterTypeDefinition("DataReportViewerWidget", cfg => cfg
              .WithPart("CommonPart")
              .WithPart("DataReportViewerPart")
              .WithPart("WidgetPart")
              .WithSetting("Stereotype", "Widget")
              .DisplayedAs("Data Report Viewer Widget"));

            return 2;
        }
        public int UpdateFrom2() {
            // crea un content type vuoto di servizio (usato in fase di visualizzazione del report e dei relativi filtri)
            ContentDefinitionManager.AlterTypeDefinition("DataReportEmptyType", cfg => cfg
              .Draftable(false)
              .Listable(false)
              .Securable(false)
              .Creatable(false)
              .DisplayedAs("Data Report Empty Type"));
            return 3;
        }
        public int UpdateFrom3() {
            ContentDefinitionManager.DeleteTypeDefinition("DataReportEmptyType");
            return 4;
        }
    }
}