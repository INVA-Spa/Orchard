﻿using Orchard;
using Orchard.ContentManagement.MetaData.Models;
using System.Collections.Generic;

namespace Contrib.Profile.Services {
    public interface IDefaultFrontEndSettingsProvider : IDependency {
        /// <summary>
        /// Sets the defaults for the part for this implementation of the provider.
        /// </summary>
        /// <param name="definition">The definition of the type being processed</param>
        void ConfigureDefaultValues(ContentTypeDefinition definition);

        /// <summary>
        /// Tells what parts this provider is set to handle
        /// </summary>
        /// <returns>The names of all the parts this provider will handle.</returns>
        IEnumerable<string> ForParts();
    }
}
