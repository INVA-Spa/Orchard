﻿using Orchard.ContentManagement.MetaData;
using Orchard.Core.Contents.Extensions;
using Orchard.Data.Migration;
namespace Laser.Orchard.ContentExtension {
    public class Migrations : DataMigrationImpl {
        public int Create() {
            SchemaBuilder.CreateTable("ContentTypePermissionRecord", table => table
                     .Column<int>("Id", column => column.PrimaryKey().Identity())
                     .Column<string>("ContentType")
                     .Column<string>("PostPermission")
                     .Column<string>("GetPermission")
                     .Column<string>("DeletePermission")
                     .Column<string>("PublishPermission")
                  );
            return 1;
        }
        public int UpdateFrom1() {
            SchemaBuilder.CreateTable("DynamicProjectionPartRecord", table => table
                     .ContentPartRecord()
                     .Column<string>("AdminMenuText")
                     .Column<string>("AdminMenuPosition")
                     .Column<bool>("OnAdminMenu", col => col.WithDefault(true))
                     .Column<string>("Icon")
                     .Column<string>("Shape")
                     .Column<int>("Items")
                     .Column<int>("ItemsPerPage")
                     .Column<int>("Skip")
                     .Column<string>("PagerSuffix", col => col.WithLength(255))
                     .Column<bool>("DisplayPager", col => col.WithDefault(true))
                     .Column<int>("MaxItems")
                     .Column<int>("QueryPartRecord_id")
                     .Column<int>("LayoutRecord_Id")
                  );
            ContentDefinitionManager.AlterPartDefinition("DynamicProjectionPart", builder => builder
                    .Attachable()
                    .WithDescription("Adds a menu item to the Admin menu that links to this content item."));
            return 2;
        }
    }
}