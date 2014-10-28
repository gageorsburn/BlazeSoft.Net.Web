using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;
using System.CodeDom.Compiler;
using System.Text.RegularExpressions;

namespace BlazeSoft.Net.Web.Core
{
    internal class PageManager
    {
        #region Singleton
        private static PageManager session;

        public static PageManager Session
        {
            get
            {
                if (session == null)
                {
                    session = new PageManager();
                }

                return session;
            }
        }
        #endregion

        internal PageManager()
        {
            LoadPages();
        }

        internal Dictionary<string, CompilerErrorCollection> CompilePages()
        {
            Dictionary<string, CompilerErrorCollection> compilerErrors = new Dictionary<string,CompilerErrorCollection>();

            string pageDirectory = HttpContext.Current.Server.MapPath(SettingsManager.Session.GetSystemSetting("PageDirectory"));

            foreach (string pageFile in Directory.GetFiles(pageDirectory, "*.bpf"))
            {
                CompilerErrorCollection compilerErrorCollection = null;

                if (!CompilePage(pageFile, out compilerErrorCollection))
                    compilerErrors.Add(pageFile, compilerErrorCollection);
            }

            return compilerErrors;
        }

        internal bool CompilePage(string path, out CompilerErrorCollection compilerErrorCollection)
        {
            Contract.Page page = Contract.Page.FromString(File.ReadAllText(path));
            bool compiled = page.CompileClasses(out compilerErrorCollection);
           
            Contract.Page oldPage = this.Pages.Where(p => p.ID == page.ID).FirstOrDefault();
            int pageIndex = Pages.IndexOf(oldPage);

            if (pageIndex != -1)
                Pages[pageIndex] = page;
            else
                Pages.Add(page);

            Debug.WriteLine("{0} = {1}", page.ID, compiled);

            return compiled;
        }

        internal void LoadPages()
        {
            string pageDirectory = HttpContext.Current.Server.MapPath(SettingsManager.Session.GetSystemSetting("PageDirectory"));

            foreach (string pageFile in Directory.GetFiles(pageDirectory, "*.bpf"))
               LoadPage(pageFile);
        }

        internal void LoadPage(string path)
        {
            Contract.Page page = Contract.Page.FromString(File.ReadAllText(path));

            if (page != null)
                if (this.GetPageByID(page.ID) == null)
                    if (page.VerifySignature())
                        Pages.Add(page);
                    else
                        Debug.WriteLine("{0} has invalid signature", path);
                else
                    Debug.WriteLine("{0} is already loaded", path);
            else
                Debug.WriteLine("{0} is not a valid page format", path);
        }

        internal List<Contract.PageReversion> GetPageReversions(string pageID)
        {
            throw new NotImplementedException();
        }

        internal void AppendPageReversion(string pageID, Contract.PageReversion pageReversion)
        {
            string pageDirectory = HttpContext.Current.Server.MapPath(SettingsManager.Session.GetSystemSetting("PageDirectory"));
            string path = Path.Combine(pageDirectory, pageID + ".bpr");

            File.AppendAllText(path, pageReversion.ToString());
        }

        internal void SavePages()
        {
            foreach (Contract.Page page in Pages)
            {
                string signingCertificate = SettingsManager.Session.GetSystemSetting("SigningCertificate");

                if (!signingCertificate.Equals("") && signingCertificate != null)
                    page.GenerateSignature(Convert.FromBase64String(signingCertificate));

                page.Save();
            }
        }

        internal List<Contract.Page> Pages = new List<Contract.Page>();

        public Contract.Page GetPage(string pageUrl)
        {
            return Pages.Where(page => page.Url.Equals(pageUrl, StringComparison.CurrentCultureIgnoreCase)).FirstOrDefault();
        }

        public Contract.Page GetPageByID(string pageID)
        {
            return Pages.Where(page => page.ID == pageID).FirstOrDefault();
        }

        public Contract.Page GetPageByUri(Uri pageUri)
        {
            //return Pages.Where(Page => Regex.IsMatch(PageUri.GetPageUrl(), "^" + Page.Url + "$", RegexOptions.IgnoreCase)).FirstOrDefault();
            return Pages.Where(page => Regex.IsMatch(pageUri.GetPageUrl(), "^" + page.Url + "$", RegexOptions.IgnoreCase) && Regex.IsMatch(pageUri.Host, "^" + page.Domain + "$", RegexOptions.IgnoreCase)).FirstOrDefault();
        }
    }
}