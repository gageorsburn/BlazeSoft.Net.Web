using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Dynamic;
using System.Linq;
using System.Reflection;
using BlazeSoft.Net.Web.Core;

namespace BlazeSoft.Net.Web.Link
{
    public class ModuleLink : DynamicObject
    {
        internal ModuleLink() : base() { }

        internal dynamic Parent;
        internal ModuleLink(dynamic Parent) { this.Parent = Parent; }

        public bool IsSigned { get { return Parent.CoreModule.IsSigned; } }
        public bool IsSystemSigned { get { return Parent.CoreModule.IsSystemSigned; } }

        public void Require(string ModuleID)
        {
            Contract.Module Module = ModuleManager.Session.GetModuleByID(ModuleID);

            if (Module == null)
                throw new Exceptions.ModuleNotFoundException(ModuleID);

            Parent.ModuleSession.GetInstance(Module);
        }

        public override bool TryGetMember(GetMemberBinder binder, out object result)
        {
            Contract.Module Module = ModuleManager.Session.GetModuleByID(binder.Name);

            if (Module == null)
                throw new Exceptions.ModuleNotFoundException(binder.Name);

            result = Parent.ModuleSession.GetInstance(Module);
            return true;
        }

        public dynamic InvokeMethod(string ModuleID, string MethodName, params object[] Params)
        {
            Contract.Module Module = ModuleManager.Session.GetModuleByID(ModuleID);

            if (Module == null)
                throw new Exceptions.ModuleNotFoundException(ModuleID);

            dynamic ModuleInstance = Parent.ModuleSession.GetInstance(Module);
            Type ModuleType = ModuleInstance.GetType();

            var ParamTypes = from param in Params select param.GetType();
            MethodInfo Method = ModuleType.GetMethod(MethodName, ParamTypes.ToArray());

            if (Method == null)
                throw new MissingMethodException(ModuleType.FullName, MethodName);

            ObsoleteAttribute Obsolete = Method.GetCustomAttribute<ObsoleteAttribute>();
            if (Obsolete != null)
            {
                var ParamTypeNames = from param in Params select param.GetType().Name;

                if (Obsolete.IsError)
                    throw new Exception("InvokeMethod(" + ModuleID + ") -> " + MethodName + "(" + string.Join(", ", ParamTypeNames) + ") is obsolete" + (Obsolete.Message != null ? ": '" + Obsolete.Message + "'" : ""));
                else
                    Debug.WriteLine("[Warning] InvokeMethod(" + ModuleID + ") -> " + MethodName + "(" + string.Join(", ", ParamTypeNames) + ") is obsolete" + (Obsolete.Message != null ? ": '" + Obsolete.Message + "'" : ""));
            }

            string PublicKey = null;

            if (this.Parent is Page)
                PublicKey = this.Parent.CorePage.PublicKey != null ? Convert.ToBase64String(this.Parent.CorePage.PublicKey) : null;
            else if (this.Parent is Contract.Module || this.Parent is LanguageHandler)
                PublicKey = this.Parent.CoreModule.PublicKey != null ? Convert.ToBase64String(this.Parent.CoreModule.PublicKey) : null;

            InvokableAttribute Invokable = Method.GetCustomAttribute<InvokableAttribute>();
            if (Invokable == null)
                throw new MissingMethodException(ModuleType.FullName, MethodName);
            else
            {
               // if (Invokable.PublicKey != null && PublicKey != Invokable.PublicKey)
                  //  throw new Exceptions.SignatureException();

                return Method.Invoke(ModuleInstance, Params);
            }
        }

        public dynamic GetProperty(string ModuleID, string PropertyName)
        {
            return this.InvokeMethod(ModuleID, "get_" + PropertyName);
        }

        public void SetProperty(string ModuleID, string PropertyName, object Value)
        {
            this.InvokeMethod(ModuleID, "set_" + PropertyName, Value);
        }

        #region Obsolete
        [Obsolete("Moved to SystemLink")]
        public List<string> IDs
        {
            get
            {
                return Parent.System.ModuleIDs;
            }
        }

        [Obsolete("Moved to SystemLink")]
        public List<Contract.Module> Modules
        {
            get
            {
                return Parent.System.Modules;
            }
        }

        [Obsolete("Moved to SystemLink")]
        public dynamic GetModuleProperties(string moduleID)
        {
            return Parent.System.GetModuleProperties(moduleID);
        }

        [Obsolete("Moved to SystemLink")]
        public bool DeleteModule(string ID)
        {
            return Parent.System.DeleteModule(ID);
        }

        [Obsolete("Moved to SystemLink")]
        public CompilerErrorCollection UpdateModule(dynamic moduleObject)
        {
            return Parent.System.UpdateModule(moduleObject);
        }

        [Obsolete("Moved to SystemLink")]
        public CompilerErrorCollection TryCompileModule(dynamic moduleObject)
        {
            return Parent.System.TryCompileModule(moduleObject);
        }

        [Obsolete]
        public MethodInfo[] GetInvokableMethods(string ID)
        {
            try
            {
                Contract.Module Module = ModuleManager.Session.Modules.Where(M => M.ID == ID).FirstOrDefault();
                Type ModuleType = Module.Type;
                return ModuleType.GetMethods().Where(M => M.GetCustomAttribute<InvokableAttribute>() != null).ToArray();
            }
            catch { return new MethodInfo[0]; }
        }
        #endregion
    }
}