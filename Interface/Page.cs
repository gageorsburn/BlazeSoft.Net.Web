using System;
using System.Web;
using BlazeSoft.Net.Web.Link;

namespace BlazeSoft.Net.Web
{
    /// <summary>
    /// Represents the page interface.
    /// </summary>
    public class Page
    {
        private Contract.Page corePage;
        private Core.ModuleSession moduleSession;

        internal Contract.Page CorePage
        {
            get
            {
                return corePage;
            }

            set
            {
                if (corePage != null)
                    throw new Exception();

                this.corePage = value;
            }
        }

        internal Core.ModuleSession ModuleSession
        {
            get
            {
                return moduleSession;
            }

            set
            {
                if (moduleSession != null)
                    throw new Exception();

                this.moduleSession = value;
            }
        }

        internal bool IsSystemSigned { get { return CorePage.IsSystemSigned; } }

        /// <summary>
        /// Returns the HttpContext for this page.
        /// </summary>
        public readonly HttpContext Http;

        /// <summary>
        /// Returns the current HttpRequest for this page.
        /// </summary>
        public readonly HttpRequest Request;

        /// <summary>
        /// Returns the current HttpResponse for this page.
        /// </summary>
        public readonly HttpResponse Response;

        /// <summary>
        /// Returns the HttpServerUtility for this page.
        /// </summary>
        public readonly HttpServerUtility Server;

        /// <summary>
        /// Returns a link object to provide access to Themes.
        /// </summary>
        public readonly ThemeLink Themes;

        /// <summary>
        /// Returns a link object to provide access to Settings.
        /// </summary>
        public readonly SettingsLink Settings;

        /// <summary>
        /// Returns a link object to provide access to Modules.
        /// </summary>
        public readonly dynamic Modules;

        /// <summary>
        /// Returns a link object to provide access to Pages.
        /// </summary>
        public readonly PageLink Pages;

        /// <summary>
        /// Returns a link object to provide access to System.
        /// </summary>
        public readonly SystemLink System;

        /// <summary>
        /// Sets this pages globals.
        /// </summary>
        public Page()
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

        /// <summary>
        /// Initializes this page.
        /// </summary>
        public virtual void Initialize()
        {

        }

        /// <summary>
        /// Deinitializes this page.
        /// </summary>
        public virtual void Deinitialize()
        {

        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns>Returns output for this page.</returns>
        public string GetOutput()
        {
            return "";
        }
    }
}