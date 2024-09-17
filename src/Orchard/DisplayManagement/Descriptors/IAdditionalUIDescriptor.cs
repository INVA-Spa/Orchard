using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Orchard.ContentManagement;

namespace Orchard.DisplayManagement.Descriptors {
    public interface IAdditionalUIDescriptor:IDependency {
        /// <summary>
        /// Based on the content builds a description to be consumed wherever desscriptor is called
        /// </summary>
        /// <param name="content"></param>
        /// <returns></returns>
        public string Evaluate(IContent content);
        /// <summary>
        /// context the descriptor is applicable for
        /// </summary>
        public string UIContext {
            get; set;
        }
        /// <summary>
        /// parameter used to sort the descriptor if needed
        /// </summary>
        public int Priority {
            get;
        }
    }
}
