﻿using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Web;

using BlazeSoft.Net.Web.Core;
using BlazeSoft.Net.Web.Serialization;
using BlazeSoft.Security.Cryptography;

namespace BlazeSoft.Net.Web.Contract
{
    /// <summary>
    /// Represents a module.
    /// </summary>
    [DataContract]
    public class Module
    {
        private Assembly assembly;

        /// <summary>
        /// Gets or sets whether or not this module has been changed.
        /// </summary>
        public bool IsChanged { get; set; }

        /// <summary>
        /// Gets the path where all of the depencies for this module should be located.
        /// </summary>
        public string DependencyPath
        {
            get
            {
                return Path.Combine(HttpContext.Current.Server.MapPath("./bin/Libraries/"), this.ID);
            }

            private set { }
        }

        /// <summary>
        /// Gets or sets the ID of this module.
        /// </summary>
        [DataMember]
        public string ID { get; set; }

        /// <summary>
        /// Gets or sets whether or not this module can be altered.
        /// </summary>
        [DataMember]
        public bool Locked { get; set; }

        /// <summary>
        /// Gets or sets whether or not this module will be loaded into every page.
        /// </summary>
        [DataMember]
        public bool Global { get; set; }

        /// <summary>
        /// Gets or sets the language handler information for this module. 
        /// This is only used by modules that are language handlers.
        /// 
        /// At this point in time it is unknown whether this implementation will stay.
        /// </summary>
        [DataMember]
        public LanguageHandlerInfo LanguageHandler { get; set; }

        /// <summary>
        /// Gets or sets the public key for this module.
        /// </summary>
        [DataMember]
        public byte[] PublicKey { get; set; }

        /// <summary>
        /// Gets or sets the public key for the signature for this module.
        /// </summary>
        [DataMember]
        public byte[] Signature { get; set; }

        /// <summary>
        /// Gets or sets the compiled code for this module.
        /// </summary>
        [DataMember]
        public byte[] CompiledCode { get; set; }

        /// <summary>
        /// Gets or sets the compiled code symbols for this module.
        /// </summary>
        [DataMember]
        public byte[] CompiledCodeSymbols { get; set; }

        /// <summary>
        /// Gets or sets the compiler language for this module.
        /// 
        /// Example:
        ///     CSharp
        ///     VisualBasic
        ///     Python
        ///     
        /// </summary>
        [DataMember]
        public string CompilerLanguage { get; set; }

        /// <summary>
        /// Gets or sets the assembly name for this module.
        /// </summary>
        [DataMember]
        public string AssemblyName { get; set; }

        /// <summary>
        /// A list of references that this module uses.
        /// </summary>
        [DataMember]
        public List<string> References = new List<string>();

        /// <summary>
        /// Gets or sets the latest <see cref="ModuleReversion"/> of this module.
        /// </summary>
        [DataMember]
        public ModuleReversion LatestReversion { get; set; }

        /// <summary>
        /// Compiles all of the <see cref="ModuleClass"/> inside of this module.
        /// </summary>
        /// <returns>Returns whether or not this module compiled successfully.</returns>
        public bool CompileClasses()
        {
            CompilerErrorCollection compilerErrorCollection;
            return CompileClasses(out compilerErrorCollection, true);
        }

        /// <summary>
        /// Compiles all of the <see cref="ModuleClass"/> inside of this module.
        /// </summary>
        /// <param name="copyCompiledCode"></param>
        /// <param name="definitions"></param>
        /// <returns>Returns whether or not this module compiled successfully.</returns>
        public bool CompileClasses(bool copyCompiledCode, params string[] definitions)
        {
            CompilerErrorCollection compilerErrorCollection;
            return CompileClasses(out compilerErrorCollection, copyCompiledCode, definitions);
        }

        /// <summary>
        /// Compiles all of the <see cref="ModuleClass"/> inside of this module.
        /// </summary>
        /// <param name="compilerErrorCollection">The collection of errors generated by compiling.</param>
        /// <returns>Returns whether or not this module compiled successfully.</returns>
        public bool CompileClasses(out CompilerErrorCollection compilerErrorCollection)
        {
            return CompileClasses(out compilerErrorCollection, true);
        }

        public bool CompileClasses(out CompilerErrorCollection compilerErrorCollection, bool copyCompiledCode, params string[] definitions)
        {
            assembly = null;

            Debug.StartTimer("Module:" + this.ID + ":Compile()");

            List<string> classCode = new List<string>();

            foreach (ModuleClass moduleClass in LatestReversion.Classes)
                classCode.Add(moduleClass.Data);

            if (this.CompilerLanguage == null) 
                CompilerLanguage = "CSharp";

            if (this.CompilerLanguage != "CSharp" && this.CompilerLanguage != "VisualBasic" && this.CompilerLanguage != "JScript")
                throw new Exception("Invalid module compiler language.");

            CodeDomProvider codeDomProvider = CodeDomProvider.CreateProvider(this.CompilerLanguage);
            CompilerParameters compilerParameters = new CompilerParameters();

            compilerParameters.CompilerOptions = string.Format("/debug:{0} /lib:{1},{2}{3} /define:{4}", "pdbonly", HttpContext.Current.Server.MapPath("./bin/"), HttpContext.Current.Server.MapPath("./bin/Libraries/"), Directory.Exists(this.DependencyPath) ? "," + this.DependencyPath : "", definitions.Length > 0 ? "WEBCORE;WEBCORE_2014;" + string.Join(";", definitions) : "WEBCORE;WEBCORE_2014");
            compilerParameters.GenerateInMemory = false;
            compilerParameters.WarningLevel = 4;
            compilerParameters.ReferencedAssemblies.AddRange(References.ToArray());

            CompilerResults compilerResults = codeDomProvider.CompileAssemblyFromSource(compilerParameters, classCode.ToArray());

            compilerErrorCollection = compilerResults.Errors;

            foreach (CompilerError compilerError in compilerErrorCollection)
                System.Diagnostics.Debug.WriteLine(compilerError.ErrorText);

            if (compilerResults.Errors.HasErrors)
                return false;

            FileInfo AssemblyFileInfo = new FileInfo(compilerResults.PathToAssembly);

            this.CompiledCode = File.ReadAllBytes(AssemblyFileInfo.FullName);
            this.CompiledCodeSymbols = File.ReadAllBytes(AssemblyFileInfo.FullName.Replace(".dll", ".pdb"));
            this.AssemblyName = compilerResults.CompiledAssembly.GetName().Name;

            Debug.StopTimer("Module:" + this.ID + ":Compile()");

            return true;
        }

        /// <summary>
        /// Gets the assembly generated from this module.
        /// </summary>
        public Assembly Assembly
        {
            get
            {
                if (assembly == null)
                {
                    if (this.CompiledCode == null)
                        this.CompileClasses();

                    if (this.CompiledCode != null)
                        if (this.CompiledCodeSymbols != null)
                            assembly = Assembly.Load(this.CompiledCode, this.CompiledCodeSymbols);
                        else
                            assembly = Assembly.Load(this.CompiledCode);
                }

                return assembly;
            }
        }

        /// <summary>
        /// Gets the type of this module.
        /// </summary>
        public Type Type
        {
            get
            {
                return this.Assembly.GetTypes().Where(Type => Type.BaseType.FullName == "BlazeSoft.Net.Web.Module").FirstOrDefault();
            }
        }

        /// <summary>
        /// Gets the language handler type of this module.
        /// </summary>
        public Type LanguageHandlerType
        {
            get
            {
                return this.Assembly.GetTypes().Where(Type => Type.BaseType.FullName == "BlazeSoft.Net.Web.LanguageHandler").FirstOrDefault();
            }
        }

        /// <summary>
        /// Gets an instance of this module.
        /// </summary>
        public BlazeSoft.Net.Web.Module Instance
        {
            get
            {
                return Activator.CreateInstance(this.Type) as BlazeSoft.Net.Web.Module;
            }
        }

        /// <summary>
        /// Gets an instance of this module as a language handler.
        /// </summary>
        public LanguageHandler LanguageHandlerInstance
        {
            get
            {
                return Activator.CreateInstance(this.LanguageHandlerType) as LanguageHandler;
            }
        }

        /// <summary>
        /// Gets whether or not the module is signed by the system.
        /// </summary>
        public bool IsSystemSigned
        {
            get
            {
                #if DEBUG
                return true;
                #else
                if (this.PublicKey == null || this.Signature == null)
                    return false;
                else
                    return this.VerifySignature() && Convert.ToBase64String(this.PublicKey) == "BgIAAACkAABSU0ExAAQAAAEAAQDvnQsT2NQ2yM5xJRA4/i4uvasi2XneBDZcQ1UBqzHJH6NOt27dM7paRlVJ74TfB1U5XqtRSCeyxMxYM44C9OlUKv2YHyyn9CElveXXcxAC94zaXZTXnMpUGaSPvwbH6DF5cSJtuo3dr8ikbR9OLaMB1FeiphQxuNb9xu66VSKXkA==";
                #endif
            }
        }

        /// <summary>
        /// Gets whether or not the module is signed.
        /// </summary>
        public bool IsSigned
        {
            get
            {
                #if DEBUG
                return true;
                #else
                return this.VerifySignature() && this.Signature != null && this.PublicKey != null;
                #endif
            }
        }

        /// <summary>
        /// Generates a unique signature for this module.
        /// </summary>
        /// <param name="privateKey">The private key that only the developer of this module should know.</param>
        /// <returns>Returns whether or not the signature of the module was successfully generated.</returns>
        public bool GenerateSignature(byte[] privateKey)
        {
            this.PublicKey = Signing.GetPublicKey(privateKey);
            this.Signature = Signing.GenerateSignature(this.CompiledCode, privateKey);

            return this.Signature != null;
        }

        /// <summary>
        /// Checks whether or not the signatue of this module is valid.
        /// </summary>
        /// <returns>Returns a whether or not the signature of this module is valid.</returns>
        public bool VerifySignature()
        {
            #if DEBUG
            return true;
            #else
            if (this.Signature == null || this.PublicKey == null)
                return true;
            else
                return Signing.VerifySignature(this.CompiledCode, this.Signature, this.PublicKey);
            #endif
        }

        /// <summary>
        /// Saves this module on the file system and sets <see cref="Module.IsChanged"/> to false.
        /// </summary>
        public void Save()
        {
            File.WriteAllText(HttpContext.Current.Server.MapPath(string.Format("{0}/{1}.bmf", SettingsManager.Session.GetSystemSetting("ModuleDirectory"), this.ID)), Serialization.XmlSerializer.ModuleSerializer.Serialize(this));
            this.IsChanged = false;
        }

        /// <summary>
        /// Serializes the instance of this module into an XML string.
        /// </summary>
        /// <returns>Returns a serialized XML string of this module.</returns>
        public override string ToString()
        {
            return XmlSerializer.ModuleSerializer.Serialize(this);
        }

        /// <summary>
        /// Creates an instance of a module from a serialized XML string.
        /// </summary>
        /// <param name="moduleData">A serialized XML string.</param>
        /// <returns>Returns an instance of a module.</returns>
        public static Module FromString(string moduleData)
        {
            return (Module)XmlSerializer.ModuleSerializer.Deserialize(moduleData);
        }
    }
}