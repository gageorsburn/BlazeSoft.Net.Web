using System;
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
    [DataContract]
    public class PageReversion
    {
        internal PageReversion() { }

        [DataMember]
        public int Version { get; set; }

        [DataMember]
        public DateTime Updated { get; set; }

        [DataMember]
        public string Message { get; set; }

        [DataMember]
        public List<PageClass> Classes = new List<PageClass>();

        public override string ToString()
        {
            return XmlSerializer.PageReversionSerializer.Serialize(this);
        }
    }

    [DataContract]
    public class PageClass
    {
        internal PageClass() { }

        [DataMember]
        public string Name;
        [DataMember]
        public string Data;
    }

    [DataContract]
    public class Page
    {
        internal Page() { }

        public bool IsChanged { get; set; }

        [DataMember]
        public string ID { get; set; }

        [DataMember]
        public string Name { get; set; }

        [DataMember]
        public string Domain { get; set; }

        [DataMember]
        public string Url { get; set; }

        [DataMember]
        public string CompilerLanguage { get; set; }

        [DataMember]
        public byte[] PublicKey { get; set; }
        [DataMember]
        public byte[] Signature { get; set; }

        [DataMember]
        public bool IsLocked { get; set; }

        [DataMember]
        public List<string> References = new List<string>();

        [DataMember]
        public PageReversion LatestReversion { get; set; }

        [DataMember]
        public byte[] CompiledCode { get; set; }

        [DataMember]
        public byte[] CompiledCodeSymbols { get; set; }

        public bool CompileClasses()
        {
            CompilerErrorCollection compilerErrorCollection;
            return CompileClasses(out compilerErrorCollection, true);
        }

        public bool CompileClasses(bool copyCompiledCode, params string[] definitions)
        {
            CompilerErrorCollection compilerErrorCollection;
            return CompileClasses(out compilerErrorCollection, copyCompiledCode, definitions);
        }

        public bool CompileClasses(out CompilerErrorCollection compilerErrorCollection)
        {
            return CompileClasses(out compilerErrorCollection, true);
        }

        public bool CompileClasses(out CompilerErrorCollection compilerErrorCollection, bool copyCompiledCode, params string[] definitions)
        {
            if(this.CompilerLanguage == "Html")
            {
                compilerErrorCollection = new CompilerErrorCollection();
                return true;
            }

            this.assembly = null;

            Debug.StartTimer("Page:" + this.ID + ":Compile()"); 

            List<string> classCode = new List<string>();

            foreach (PageClass pageClass in LatestReversion.Classes) 
                classCode.Add(pageClass.Data);

            if (this.CompilerLanguage == null || this.CompilerLanguage == string.Empty)
                this.CompilerLanguage = "CSharp";

            dynamic languageHandler = null;

            try 
            { 
                languageHandler = ModuleSession.Get(HttpContext.Current).GetLanguageHandler(this.CompilerLanguage);
            }
            catch (Exception e) 
            {
                Debug.WriteLine("Could not fetch language handler for {0}, Exception: {1}", this.ID, e); 
                compilerErrorCollection = new CompilerErrorCollection(); 
                return false;
            }

            if (languageHandler != null)
            {
                if (copyCompiledCode)
                    this.CompiledCode = languageHandler.CompileClasses(classCode.ToArray(), this.References.ToArray(), out compilerErrorCollection);
                else
                    languageHandler.CompileClasses(classCode.ToArray(), this.References.ToArray(), out compilerErrorCollection);

                return true;
            }
            else
            {
                string[] references = this.References.ToArray();

                for (int i = 0; i < references.Length; i++)
                {
                    if (references[i].ToLower().EndsWith(".bmf"))
                    {
                        string moduleID = references[i].Substring(0, references[i].Length - 4);
                        File.WriteAllBytes(HttpContext.Current.Server.MapPath("./Data/Temp/") + moduleID + ".bmf.dll", ModuleManager.Session.GetModuleByID(moduleID).CompiledCode);
                        references[i] += ".dll";
                    }
                }

                CodeDomProvider codeDomProvider = CodeDomProvider.CreateProvider(this.CompilerLanguage);
                CompilerParameters codeParameters = new CompilerParameters();

                codeParameters.IncludeDebugInformation = true;
                codeParameters.CompilerOptions = string.Format("{0}/lib:{1},{2},{3} /define:{4}", this.CompilerLanguage != "JScript" ? "/debug:pdbonly " : "", HttpContext.Current.Server.MapPath("./bin/"), HttpContext.Current.Server.MapPath("./bin/Libraries/"), HttpContext.Current.Server.MapPath("./Data/Temp/"), definitions.Length > 0 ? "WEBCORE;WEBCORE_2014;" + string.Join(";", definitions) : "WEBCORE;WEBCORE_2014");
                codeParameters.GenerateInMemory = false;
                codeParameters.WarningLevel = 4;
                codeParameters.ReferencedAssemblies.AddRange(References.ToArray());

                CompilerErrorCollection tmpCompilerErrorCollection = new CompilerErrorCollection();

                CompilerResults compilerResults = codeDomProvider.CompileAssemblyFromSource(codeParameters, classCode.ToArray());

                compilerErrorCollection = compilerResults.Errors;
                Debug.StopTimer("Page:" + this.ID + ":Compile()");

                if (tmpCompilerErrorCollection.Count > 0)
                    compilerErrorCollection.AddRange(tmpCompilerErrorCollection);

                if (compilerErrorCollection.HasErrors)
                    return false;

                FileInfo AssemblyFileInfo = new FileInfo(compilerResults.PathToAssembly);

                if (copyCompiledCode)
                {
                    this.CompiledCode = File.ReadAllBytes(AssemblyFileInfo.FullName);
                    if (File.Exists(AssemblyFileInfo.FullName.Replace(".dll", ".pdb")))
                        this.CompiledCodeSymbols = File.ReadAllBytes(AssemblyFileInfo.FullName.Replace(".dll", ".pdb"));
                }

                return true;
            }
        }

        private Assembly assembly;

        public Assembly Assembly
        {
            get  
            { 
                if (assembly == null)
                {
                    if (this.CompiledCode == null)
                        this.CompileClasses();

                    for (int i = 0; i < References.Count; i++)
                        if (References[i].ToLower().EndsWith(".bmf"))
                        {
                            string moduleID = References[i].Substring(0, References[i].Length - 4);
                            Assembly.Load(ModuleManager.Session.GetModuleByID(moduleID).CompiledCode);
                        }

                    if (this.CompiledCode != null)
                        if (this.CompiledCodeSymbols != null)
                            assembly = Assembly.Load(this.CompiledCode, this.CompiledCodeSymbols);
                        else
                            assembly = Assembly.Load(this.CompiledCode);
                }

                return assembly;
            }
        }

        public Type Type
        {
            get
            {
                return this.Assembly.GetTypes().Where(Type => Type.BaseType.FullName == "BlazeSoft.Net.Web.Page").FirstOrDefault();
            }
        }

        public BlazeSoft.Net.Web.Page Instance
        {
            get
            {
                LanguageHandler languageHandler = null;
                
                if (this.CompilerLanguage == null)
                    throw new Exception("Compiler Language is null.");

                languageHandler = ModuleSession.Get(HttpContext.Current).GetLanguageHandler(this.CompilerLanguage);

                if(languageHandler == null)
                    return Activator.CreateInstance(this.Type) as BlazeSoft.Net.Web.Page;

                if (this.CompiledCode == null)
                    this.CompileClasses();

                return languageHandler.GetInstance(languageHandler.GetPageType(this.CompiledCode)) as BlazeSoft.Net.Web.Page;
            }
        }

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

        public bool GenerateSignature(byte[] privateKey)
        {
            this.PublicKey = Signing.GetPublicKey(privateKey);

            if (this.CompiledCode == null)
                throw new Exception("Compiled code is null for page: " + this.ID);

            this.Signature = Signing.GenerateSignature(this.CompiledCode, privateKey);
            return this.Signature != null;
        }

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

        public void Save()
        {
            File.WriteAllText(HttpContext.Current.Server.MapPath(string.Format("{0}/{1}.bpf", SettingsManager.Session.GetSystemSetting("PageDirectory"), this.ID)), Serialization.XmlSerializer.PageSerializer.Serialize(this));
            this.IsChanged = false;
        }

        public override string ToString()
        {
            return XmlSerializer.PageSerializer.Serialize(this);
        }

        public static Page FromString(string pageData)
        {
            return (Page)XmlSerializer.PageSerializer.Deserialize(pageData);
        }
    }
}