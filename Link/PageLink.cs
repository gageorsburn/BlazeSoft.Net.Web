using System;
using System.Collections.Generic;
using System.Web;
using System.CodeDom.Compiler;
using System.Text.RegularExpressions;

namespace BlazeSoft.Net.Web.Link
{
    public class PageLink
    {
        internal dynamic Parent;

        internal PageLink(dynamic Parent) { this.Parent = Parent; }

        public bool IsSigned { get { return Parent.CorePage.IsSigned; } }
        public bool IsSystemSigned { get { return Parent.CorePage.IsSystemSigned; } }

        public string[] UrlGroupParameters
        {
            get
            {
                string httpProtocol = (HttpContext.Current.Request.IsSecureConnection) ? "https://" : "http://";
                Uri pageUri = new Uri(httpProtocol + HttpContext.Current.Request.Url.Host + HttpContext.Current.Request.RawUrl);
                List<string> urlParameters = new List<string>();
                GroupCollection groups = Regex.Match(pageUri.GetPageUrl(), "^" + this.Parent.CorePage.Url + "$").Groups;
                foreach (Group group in groups)
                    urlParameters.Add(group.Value);
                return urlParameters.ToArray();
            }
        }

        public string GetUrlParameter(int index, string defaultValue = null)
        {
            string rawUrl = HttpContext.Current.Request.RawUrl;
            if (rawUrl.StartsWith("/"))
                rawUrl = rawUrl.Substring(1);
            if (rawUrl.EndsWith("/"))
                rawUrl = rawUrl.Substring(0, rawUrl.Length - 1);
            rawUrl = rawUrl.Split('?')[0];

            string[] urlParameters = rawUrl.Split('/');

            if (urlParameters.Length > index)
                return urlParameters[index];
            else
                return defaultValue;
        }

        #region Obsolete
        [Obsolete("Use UrlGroupParameters")]
        public string[] UrlParameters { get { return this.UrlGroupParameters; } }

        [Obsolete("Moved to SystemLink")]
        public List<string> IDs
        {
            get
            {
                return Parent.System.PageIDs;
            }
        }

        [Obsolete("Moved to SystemLink")]
        public List<Contract.Page> Pages
        {
            get
            {
                return Parent.System.Pages;
            }
        }

        [Obsolete("Moved to SystemLink")]
        public dynamic GetPageProperties(string pageID)
        {
            return Parent.System.GetPageProperties(pageID);
        }

        [Obsolete("Moved to SystemLink")]
        public bool DeletePage(string ID)
        {
            return Parent.System.DeletePage(ID);
        }

        [Obsolete("Moved to SystemLink")]
        public CompilerErrorCollection UpdatePage(dynamic pageObject)
        {
            return Parent.System.UpdatePage(pageObject);
        }

        [Obsolete("Moved to SystemLink")]
        public CompilerErrorCollection TryCompilePage(dynamic pageObject)
        {
            return Parent.System.TryCompilePage(pageObject);
        }

        //public bool TryDebugPage(dynamic pageObject, out CompilerErrorCollection compilerErrors, out Exception exception, out string output)
        //{
        //    compilerErrors = new CompilerErrorCollection();
        //    exception = null;
        //    output = null;

        //    Contract.Page page = new Contract.Page()
        //    {
        //        ID = pageObject.ID,
        //        Domain = pageObject.Domain,
        //        Url = pageObject.Url,
        //        CompilerLanguage = pageObject.CompilerLanguage,
        //        References = ((ThirdParty.JsonArray)pageObject.References).Select(r => (string)r).ToList(),
        //    };

        //    PageReversion pageReversion = new PageReversion() { Version = 1, Updated = DateTime.UtcNow, Classes = new List<PageClass>() };
        //    pageReversion.Classes = ((ThirdParty.JsonArray)pageObject.LatestReversion.Classes).Select(p => new PageClass() { Name = ((dynamic)p).Name, Data = ((dynamic)p).Data }).ToList();
        //    page.Reversions.Add(pageReversion);

        //    page.CompileClasses(out compilerErrors);

        //    if (compilerErrors.HasErrors)
        //        return true;

        //    Stream originalFilter = HttpContext.Current.Response.Filter;
        //    try
        //    {
        //        HttpContext.Current.Response.Clear();
        //        ResponseInterceptFilter responseFilter = new ResponseInterceptFilter();
        //        HttpContext.Current.Response.Filter = responseFilter;

        //        Page pageInstance = page.Instance;
        //        ModuleSession moduleSession = ModuleSession.Get(HttpContext.Current);

        //        pageInstance.CorePage = page;
        //        pageInstance.ModuleSession = moduleSession;

        //        pageInstance.Initialize();
        //        output = pageInstance.Themes.GetHtmlOutput(page, pageInstance, moduleSession);
        //        pageInstance.Deinitialize();

        //        HttpContext.Current.Response.Write(output);
        //        HttpContext.Current.Response.Write(Debug.Output);
        //        HttpContext.Current.Response.Flush();

        //        output = responseFilter.Output;
        //        HttpContext.Current.Response.Filter = originalFilter;
        //    }
        //    catch(Exception e)
        //    {
        //        exception = e;
        //        HttpContext.Current.Response.Clear();
        //        HttpContext.Current.Response.Filter = originalFilter;
        //        return true;
        //    }

        //    return false;
        //}
        #endregion
    }
}