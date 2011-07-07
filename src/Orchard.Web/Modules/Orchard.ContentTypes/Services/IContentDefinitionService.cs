﻿using System.Collections.Generic;
using Orchard.ContentManagement;
using Orchard.ContentManagement.Definition;
using Orchard.ContentManagement.Definition.Models;
using Orchard.ContentTypes.ViewModels;

namespace Orchard.ContentTypes.Services {
    public interface IContentDefinitionService : IDependency {
        IEnumerable<EditTypeViewModel> GetTypes();
        EditTypeViewModel GetType(string name);
        ContentTypeDefinition AddType(string name, string displayName);
        void AlterType(EditTypeViewModel typeViewModel, IUpdateModel updater);
        void RemoveType(string name);
        void AddPartToType(string partName, string typeName);
        void RemovePartFromType(string partName, string typeName);
        string GenerateContentTypeNameFromDisplayName(string displayName);

        IEnumerable<EditPartViewModel> GetParts(bool metadataPartsOnly);
        EditPartViewModel GetPart(string name);
        EditPartViewModel AddPart(CreatePartViewModel partViewModel);
        void AlterPart(EditPartViewModel partViewModel, IUpdateModel updater);
        void RemovePart(string name);

        IEnumerable<ContentFieldInfo> GetFields();
        void AddFieldToPart(string fieldName, string fieldTypeName, string partName);
        void RemoveFieldFromPart(string fieldName, string partName);
    }
}