using System.Collections.Generic;
using System.Runtime.Serialization;

namespace BlazeSoft.Net.Web.Contract
{
    /// <summary>
    /// Represents information about a language handler.
    /// </summary>
    public class LanguageHandlerInfo
    {
        internal LanguageHandlerInfo() { }

        /// <summary>
        /// Gets or sets the ID of the language handler.
        /// </summary>
        [DataMember]
        public string ID { get; set; }

        /// <summary>
        /// Gets or sets the file extension of the language.
        /// 
        /// Example:
        ///     CSharp = .cs
        ///     VBasic = .vb
        ///     Python = .py
        ///     Ruby   = .rb
        /// </summary>
        [DataMember]
        public string FileExtension { get; set; }

        /// <summary>
        /// A list of templates that the language handler has defined.
        /// </summary>
        [DataMember]
        public List<LanguageHandlerTemplate> Templates = new List<LanguageHandlerTemplate>();
    }
}