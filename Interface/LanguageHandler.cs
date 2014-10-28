using System;
using System.Web;
using System.CodeDom.Compiler;

using BlazeSoft.Net.Web.Link;

namespace BlazeSoft.Net.Web
{
    [System.AttributeUsage(System.AttributeTargets.Method)]
    public class InvokableAttribute : System.Attribute
    {
        public string PublicKey { get; private set; }

        public InvokableAttribute() { }
        public InvokableAttribute(string PublicKey) { this.PublicKey = PublicKey; }
    }

    public class LanguageHandler
    {
        internal Contract.Module CoreModule { get; set; }
        internal Core.ModuleSession ModuleSession { get; set; }

        public readonly HttpContext Http;
        public readonly HttpRequest Request;
        public readonly HttpResponse Response;
        public readonly HttpServerUtility Server;

        public readonly ThemeLink Themes;
        public readonly SettingsLink Settings;
        public readonly dynamic Modules;
        public readonly PageLink Pages;
        public readonly SystemLink System;

        public LanguageHandler()
        {
            this.Http = HttpContext.Current;
            this.Request = this.Http.Request;
            this.Response = this.Http.Response;
            this.Server = this.Http.Server;

            this.Themes = new ThemeLink(this);
            this.Settings = new SettingsLink(this);
            this.Modules = new ModuleLink(this);
            this.Pages = new PageLink(this);
            this.System = new SystemLink(this);
        }

        public virtual byte[] CompileClasses(string[] classes, string[] references, out CompilerErrorCollection compilerErrorCollection)
        {
            compilerErrorCollection = new CompilerErrorCollection();
            return null;
        }

        public virtual object GetPageType(byte[] compiledCode)
        {
            return null;
        }

        public virtual object GetInstance(object type)
        {
            return null;
        }

        public virtual object GetFieldValue(object instance, string fieldName)
        {
            return null;
        }

        public virtual object CallMethod(object instance, string methodName, params object[] parameters)
        {
            return null;
        }
    }
}