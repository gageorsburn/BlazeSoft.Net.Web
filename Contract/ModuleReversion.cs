using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

using BlazeSoft.Net.Web.Serialization;

namespace BlazeSoft.Net.Web.Contract
{
    /// <summary>
    /// Represents a revision of a module.
    /// </summary>
    public class ModuleReversion
    {
        internal ModuleReversion() { }

        /// <summary>
        /// Gets or sets the version of the module reversion.
        /// </summary>
        [DataMember]
        public int Version { get; set; }

        /// <summary>
        /// Gets or sets when the module reversion was last updated,
        /// </summary>
        [DataMember]
        public DateTime Updated { get; set; }

        /// <summary>
        /// Gets or sets the message associated with the module reversion.
        /// </summary>
        [DataMember]
        public string Message { get; set; }

        /// <summary>
        /// A list of classes inside of the module reversion.
        /// </summary>
        [DataMember]
        public List<ModuleClass> Classes = new List<ModuleClass>();

        /// <summary>
        /// Serializes the data into a XML string to store in the Blaze Module Reversion files.
        /// </summary>
        /// <returns>Returns a serialized XML string.</returns>
        public override string ToString()
        {
            return XmlSerializer.ModuleReversionSerializer.Serialize(this);
        }
    }
}