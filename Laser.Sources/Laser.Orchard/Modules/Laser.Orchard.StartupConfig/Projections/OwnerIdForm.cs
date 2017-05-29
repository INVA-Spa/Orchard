﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using Orchard.DisplayManagement;
using Orchard.Events;
using Orchard.Localization;


namespace Laser.Orchard.StartupConfig.Projections {
    public class OwnerIdForm : IFormProvider {
        public Localizer T { get; set; }
        protected dynamic Shape { get; set; }

        public OwnerIdForm(IShapeFactory shapeFactory) {
            Shape = shapeFactory;
            T = NullLocalizer.Instance;
        }
        public void Describe(dynamic context) {
            Func<IShapeFactory, dynamic> form =
                shape => {

                    var f = Shape.Form(
                        Id: "OwnerIdForm",
                        _OwnerId: Shape.TextBox(
                            Id: "OwnerId", Name: "OwnerId",
                            Title: T("Owner Id"),
                            Description: T("The owner id code to filter for."),
                            Classes: new[] { "text", "tokenized" }
                            )
                            );
                    return f;
                };

            context.Form("OwnerIdForm", form);

        }

    }
}