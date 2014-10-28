using System.Collections.Generic;
using System.Runtime.Serialization;

namespace BlazeSoft.Net.Web.Contract
{
    /// <summary>
    /// Represents generic templates for a language.
    /// </summary>
    public class LanguageHandlerTemplate
    {
        internal LanguageHandlerTemplate() { }

        /// <summary>
        /// Gets or sets the ID of the language handler template.
        /// 
        /// Example:
        ///     Page
        ///     Module
        ///     Language Handler
        ///     
        ///     Class
        ///     Interface
        ///     
        ///     Singleton
        ///     Factory
        ///     etc.
        ///     
        /// 
        /// </summary>
        [DataMember]
        public string ID { get; set; }

        /// <summary>
        /// Gets or sets the code for the template. 
        /// When writing a template for a class you can use {0} to specify the name.
        /// 
        /// Example:
        ///     class {0}():
        ///     __init__(self):
        ///	        pass
        ///	        
        ///     If the user passed in MyClass as the name when creating a class the generated code would result:
        /// 
        ///     class MyClass():
        ///         __init__(self):
        ///             pass
        /// </summary>
        [DataMember]
        public string Data { get; set; }

        /// <summary>
        /// A list of all of the references that the template should have by default.
        /// </summary>
        [DataMember]
        public List<string> RequiredReferences = new List<string>();
    }
}