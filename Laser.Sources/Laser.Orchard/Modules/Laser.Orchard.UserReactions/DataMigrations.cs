﻿using Laser.Orchard.UserReactions.Models;
using Orchard.Data.Migration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard.Core.Contents.Extensions;
using Orchard.ContentManagement.MetaData;

namespace Laser.Orchard.UserReactions {
    public class DataMigrations : DataMigrationImpl {

        public int Create() {
            SchemaBuilder.CreateTable("UserReactionsTypesRecord", table => table
                .Column<int>("Id", col => col.PrimaryKey().Identity())
                .Column<string>("TypeName", col => col.WithLength(50))
                .Column<string>("TypeCssClass", col => col.WithLength(50))
                .Column<int>("Priority")
                );

            return 1;
        }


        public int UpdateFrom1() 
        {
            SchemaBuilder.CreateTable("UserReactionsPartRecord",table => table.ContentPartRecord());
            SchemaBuilder.CreateTable("UserReactionsSummaryRecord", table => table
                
                .Column<int>("Id", col => col.PrimaryKey().Identity())
                .Column<int>("UserReactionsPartRecord_Id")
                .Column<int>("UserReactionsTypesRecord_Id")
                .Column<int>("Quantity",col => col.NotNull().WithDefault(0))                               
                );


            SchemaBuilder.AlterTable("UserReactionsSummaryRecord", table => table
                        .CreateIndex("Index_UserReactionsSummaryRecord_UserReactionsPartRecord_Id",
                                    "UserReactionsPartRecord_Id"));

            SchemaBuilder.AlterTable("UserReactionsSummaryRecord", table => table
                        .CreateIndex("Index_UserReactionsSummaryRecord_UserReactionsTypesRecord_Id",
                                    "UserReactionsTypesRecord_Id"));
            ContentDefinitionManager.AlterPartDefinition(
            typeof(UserReactionsPart).Name,
                cfg => cfg.Attachable());


            return 2;
        
        }


    }
}