using System;
using System.Web;

using BlazeSoft.Net.Web.Link;

namespace BlazeSoft.Net.Web
{
    public class Module
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

        public Module()
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

        public virtual Uri GetPageUri(Uri pageUri)
        {
            return pageUri;
        }

        public virtual Uri GetPageRedirect(Uri pageUri)
        {
            return pageUri;
        }

        public virtual void Initialize()
        {

        }

        public virtual void Deinitialize()
        {

        }
    }
}