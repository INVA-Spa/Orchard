﻿using Laser.Orchard.StartupConfig.Models;
using Laser.Orchard.StartupConfig.Services;
using Orchard.ContentManagement.MetaData;
using Orchard.Core.Contents.Extensions;
using Orchard.Data.Migration;
using Orchard.Environment.Extensions;
using System.Data;

namespace Laser.Orchard.StartupConfig {

    public class MigrationStartupConfig : DataMigrationImpl {

        public int Create() {
            ContentDefinitionManager.AlterPartDefinition("CommonPart", b => b
                                                            .WithField("Creator", cfg => cfg.OfType("NumericField").WithDisplayName("Id of user Creator"))
                                                            .WithField("LastModifier", cfg => cfg.OfType("NumericField").WithDisplayName("Id of last user that have modified the content item"))
                                                            );
            return 1;
        }

        public int UpdateFrom1() {
            ContentDefinitionManager.AlterPartDefinition("PublishExtensionPart", b => b
                .Attachable()
                .WithField("PublishExtensionStatus", cfg => cfg.OfType("EnumerationField")
                    .WithSetting("EnumerationFieldSettings.Required", "true")
                    .WithSetting("EnumerationFieldSettings.ListMode", "Dropdown")
                    .WithSetting("EnumerationFieldSettings.Options", "Created\r\nLoaded\r\nAccepted\r\nRejected")
                    .WithDisplayName("Status"))

                );

            return 2;
        }
    }

    [OrchardFeature("Laser.Orchard.StartupConfig.PermissionExtension")]
    public class MigrationsUsersGroups : DataMigrationImpl {

        public int Create() {
            return 1;
        }

        public int UpdateFrom1() {
            //ContentDefinitionManager.AlterPartDefinition("UsersGroupsPart", part => part.Attachable());
            ContentDefinitionManager.AlterTypeDefinition("User",
                cfg => cfg
                    .WithPart(typeof(UsersGroupsPart).Name)
                );
            //SchemaBuilder.CreateTable("UsersGroupsSettingsPartRecord", table => table
            //           .ContentPartRecord()
            //           .Column<string>("GroupSerialized")
            //);

            SchemaBuilder.CreateTable("UsersGroupsPartRecord", table => table
                       .ContentPartRecord()
                       .Column<string>("theUserGroup")
           );

            SchemaBuilder.CreateTable("ExtendedUsersGroupsRecord",
        table =>

                table
                .Column<int>("Id", column => column.PrimaryKey().Identity())
                .Column<string>("GroupName", column => column.WithLength(50))
                );
            return 2;
        }

        public int UpdateFrom2() {
            SchemaBuilder.AlterTable("ExtendedUsersGroupsRecord", table => table.AlterColumn("GroupName", col => col.WithType(System.Data.DbType.String).WithLength(150)));
            return 3;
        }
    }

    /// <summary>
    /// Estendo le taxonomy con una informazione in grado di modificare l'ordinamento dei risultati
    /// </summary>
    [OrchardFeature("Laser.Orchard.StartupConfig.TaxonomiesExtensions")]
    public class MigrationTaxonomies : DataMigrationImpl {

        public int Create() {
            ContentDefinitionManager.AlterTypeDefinition("Taxonomy",
            cfg => cfg
                .WithPart(typeof(TaxonomyExtensionPart).Name)
            );
            return 1;
        }
    }

    [OrchardFeature("Laser.Orchard.StartupConfig.Maintenance")]
    public class MigrationMaintenance : DataMigrationImpl {
        private readonly IUtilsServices _utilsServices;

        public MigrationMaintenance(IUtilsServices utilsServices) {
            _utilsServices = utilsServices;
        }

        public int Create() {
            _utilsServices.EnableFeature("Orchard.PublishLater");
            ContentDefinitionManager.AlterPartDefinition("MaintenancePart", b => b
                .Attachable(false));
            ContentDefinitionManager.AlterTypeDefinition("Maintenance", cfg => cfg
                .WithPart("CommonPart")
                .WithPart(typeof(MaintenancePart).Name)
                .WithPart("PublishLaterPart")
                .Creatable(false)
                .Draftable()
                );
            SchemaBuilder.CreateTable("MaintenancePartRecord", table => table
                .ContentPartVersionRecord()
                .Column<string>("MaintenanceNotifyType")
                .Column<string>("MaintenanceNotify")
            );
            SchemaBuilder.AlterTable("MaintenancePartRecord", table => table
            .AddColumn("Selected_Tenant", DbType.String));
            return 1;
        }

        public int UpdateFrom1() {
            return 2;
        }

        public int UpdateFrom2() {
            return 3;
        }

        public int UpdateFrom3() {
            return 4;
        }

        public int UpdateFrom4() {
            //ContentDefinitionManager.AlterTypeDefinition("Maintenance", b => b
            //         .Draftable()
            //         );
            return 5;
        }

        public int UpdateFrom5() {
            //SchemaBuilder.CreateTable("MaintenancePartRecord", table => table
            //                .ContentPartVersionRecord()
            //                .Column<string>("MaintenanceNotifyType")
            //                .Column<string>("MaintenanceNotify")
            //    );
            return 6;
        }

        public int UpdateFrom6() {
            //      SchemaBuilder.DropTable("MaintenancePartRecord");

            return 7;
        }
        public int UpdateFrom7() {
            //      SchemaBuilder.DropTable("MaintenancePartRecord");

            return 8;
        }
    }
}