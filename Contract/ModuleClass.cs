using System.Runtime.Serialization;

namespace BlazeSoft.Net.Web.Contract
{

    /// <summary>
    /// Represents a class inside of a module.
    /// </summary>
    [DataContract]
    public class ModuleClass
    {
        internal ModuleClass() { }

        /// <summary>
        /// Gets or sets the class name.
        /// </summary>
        [DataMember]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the class code.
        /// </summary>
        [DataMember]
        public string Data { get; set; }
    }
}