using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace BlazeSoft.Net.Web.Core
{
    internal class ModuleManager
    {
        private static ModuleManager session;

        public static ModuleManager Session
        {
            get
            {
                if (session == null)
                    session = new ModuleManager();

                return session;
            }
        }

        internal ModuleManager()
        {
            LoadModules();
        }

        internal void LoadModules()
        {
            string moduleDirectory = HttpContext.Current.Server.MapPath(SettingsManager.Session.GetSystemSetting("ModuleDirectory"));

            foreach (string moduleFile in Directory.GetFiles(moduleDirectory, "*.bmf"))
                LoadModule(moduleFile);
        }

        internal void LoadModule(string path)
        {
            Contract.Module module = Contract.Module.FromString(File.ReadAllText(path));

            if (module != null)
                if(module.VerifySignature())
                    Modules.Add(module);
        }

        internal Dictionary<string, CompilerErrorCollection> CompileModules()
        {
            Dictionary<string, CompilerErrorCollection> compilerErrors = new Dictionary<string, CompilerErrorCollection>();

            string moduleDirectory = HttpContext.Current.Server.MapPath(SettingsManager.Session.GetSystemSetting("ModuleDirectory"));

            foreach (string moduleFile in Directory.GetFiles(moduleDirectory, "*.bmf"))
            {
                CompilerErrorCollection compilerErrorCollection = null;

                if (!CompileModule(moduleFile, out compilerErrorCollection))
                    compilerErrors.Add(moduleFile, compilerErrorCollection);
            }

            return compilerErrors;
        }

        internal bool CompileModule(string path, out CompilerErrorCollection compilerErrorCollection)
        {
            Contract.Module module = Contract.Module.FromString(File.ReadAllText(path));
            bool compiledModule = module.CompileClasses(out compilerErrorCollection);

            Contract.Module oldModule = this.Modules.Where(p => p.ID == module.ID).FirstOrDefault();

            int moduleIndex = Modules.IndexOf(oldModule);

            if (moduleIndex != -1)
                Modules[moduleIndex] = module;
            else
                Modules.Add(module);

            return compiledModule;
        }

        internal List<Contract.ModuleReversion> GetModuleReversions(string moduleID)
        {
            throw new NotImplementedException();
        }

        internal void AppendModuleReversion(string moduleID, Contract.ModuleReversion moduleReversion)
        {
            string moduleDirectory = HttpContext.Current.Server.MapPath(SettingsManager.Session.GetSystemSetting("ModuleDirectory"));
            string path = Path.Combine(moduleDirectory, moduleID + ".bmr");

            File.AppendAllText(path, moduleReversion.ToString());
        }

        internal void SaveModules()
        {
            foreach (Contract.Module module in modules)
            {
                string signingCertificate = SettingsManager.Session.GetSystemSetting("SigningCertificate");

                if (!signingCertificate.Equals("") && signingCertificate != null)
                    module.GenerateSignature(Convert.FromBase64String(signingCertificate));

                module.Save();
            }
        }

        private List<Contract.Module> modules = new List<Contract.Module>();

        public Contract.Module GetModuleByID(string moduleID)
        {
            return Modules.Where(module => module.ID == moduleID).FirstOrDefault();
        }

        public Contract.Module GetModuleByAssemblyName(string assemblyName)
        {
            return Modules.Where(module => module.AssemblyName == assemblyName).FirstOrDefault();
        }

        public static string[] GetModules()
        {
            return (from module in ModuleManager.Session.Modules select module.ID).ToArray();
        }

        public static Contract.Module GetModule(string ModuleID)
        {
            return ModuleManager.Session.GetModuleByID(ModuleID);
        }

        public static void UpdateModule(Contract.Module newModule)
        {
            Contract.Module oldModule = ModuleManager.GetModule(newModule.ID);

            if (oldModule != null)
                ModuleManager.Session.Modules[ModuleManager.Session.Modules.IndexOf(oldModule)] = newModule;
            else
                ModuleManager.Session.Modules.Add(newModule);
        }

        internal List<Contract.Module> Modules
        {
            get
            {
                return modules;
            }
        }
    }

    //TODO move into own file
    public class ModuleSession
    {
        #region ModuleSession
        Dictionary<string, BlazeSoft.Net.Web.Module> ModuleInstances = new Dictionary<string, BlazeSoft.Net.Web.Module>();
        Dictionary<string, LanguageHandler> LanguageHandlerInstances = new Dictionary<string, LanguageHandler>();
        Dictionary<Type, dynamic> LanguageHandlerObjects = new Dictionary<Type, dynamic>();

        public ModuleSession(HttpContext Context)
        {
            SettingsManager.Session.CheckForFileException();

            ModuleSessions.Add(Context, this);

            foreach (Contract.Module Module in ModuleManager.Session.Modules.Where(M => M.Global))
                GetInstance(Module);
        }

        static Dictionary<HttpContext, ModuleSession> ModuleSessions = new Dictionary<HttpContext, ModuleSession>();

        public void Finished(HttpContext Context)
        {
            ModuleSessions.Remove(Context);
        }

        public static ModuleSession Get(HttpContext Context)
        {
            if (!ModuleSessions.ContainsKey(Context))
                new ModuleSession(Context);

            return ModuleSessions[Context];
        }
        #endregion
        #region Module Instance
        internal BlazeSoft.Net.Web.Module GetInstance(Contract.Module Module)
        {
            BlazeSoft.Net.Web.Module ModuleInstance;

            if (!ModuleInstances.ContainsKey(Module.ID))
            {
                ModuleInstance = Module.Instance;

                ModuleInstance.CoreModule = Module;
                ModuleInstance.ModuleSession = this;

                Debug.StartTimer("Module:" + Module.ID + ":Initialize()");
                ModuleInstance.Initialize();
                Debug.StopTimer("Module:" + Module.ID + ":Initialize()");

                ModuleInstances.Add(Module.ID, ModuleInstance);
            }
            else
                ModuleInstance = ModuleInstances[Module.ID];

            return ModuleInstance;
        }

        public dynamic GetInstance(string ModuleID)
        {
            if (ModuleInstances.ContainsKey(ModuleID))
                return ModuleInstances[ModuleID];
            else
                return null;
        }
        #endregion
        #region Language Handler Instance
        public LanguageHandler GetLanguageHandler(string Language)
        {
            LanguageHandler LanguageHandlerInstance;
            Contract.Module Module = ModuleManager.Session.Modules.Where(M => M != null && M.LanguageHandler != null).Where(M => M.LanguageHandler != null && M.LanguageHandler.ID  == Language).FirstOrDefault();

            if (Module == null)
                return null;
            
            if (!LanguageHandlerInstances.ContainsKey(Module.ID))
            {
                LanguageHandlerInstance = Module.LanguageHandlerInstance;

                LanguageHandlerInstance.CoreModule = Module;
                LanguageHandlerInstance.ModuleSession = this;

                LanguageHandlerInstances.Add(Module.ID, LanguageHandlerInstance);
            }
            else
                LanguageHandlerInstance = LanguageHandlerInstances[Module.ID];

            if (LanguageHandlerInstance == null)
                throw new Exception("Language handler instance is null.");

            return LanguageHandlerInstance;
        }

        public void RegisterLanguageHandlerObject(dynamic LanguageHandler, object Object)
        {
            this.LanguageHandlerObjects.Add(Object.GetType(), LanguageHandler);
        }

        public dynamic GetLanguageHandlerFromObject(object Object)
        {
            if (this.LanguageHandlerObjects.ContainsKey(Object.GetType()))
                return this.LanguageHandlerObjects[Object.GetType()];
            else
                return null;
        }
        #endregion
        #region Loaded Modules
        internal Contract.Module[] GetLoadedModules()
        {
            return ModuleManager.Session.Modules.Where(Module => ModuleInstances.Keys.Contains(Module.ID)).ToArray();
        }

        internal Contract.Module[] GetLoadedLanguageHandlerModules()
        {
            return ModuleManager.Session.Modules.Where(Module => this.LanguageHandlerInstances.Keys.Contains(Module.ID)).ToArray();
        }
        #endregion
    }
}