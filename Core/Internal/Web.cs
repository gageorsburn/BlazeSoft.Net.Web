using System;
using System.Linq;
using System.Web;
using System.Threading;
using System.IO;

using Reflection = System.Reflection;

namespace BlazeSoft.Net.Web.Core
{
    public class Web : System.Web.UI.Page
    {
        private Uri pageUri;
        private static int PageAccessCount = 0;
        
        private ModuleSession moduleSession;
        internal static DateTime StartUpTime = DateTime.UtcNow;

        public Web() 
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
        }

        Reflection.Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            string assemblyStrongName = args.Name;
            string assemblyName = assemblyStrongName.Split(',').First();
            string assemblyFileName = assemblyName + ".dll";

            Reflection.Assembly resolvedAssembly = null;
            
            try
            {
                string assemblyBinPath = Server.MapPath("./bin/" + assemblyFileName);

                if (!File.Exists(assemblyBinPath))
                {
                    foreach (Contract.Module module in ModuleManager.Session.Modules)
                        if (File.Exists(Path.Combine(module.DependencyPath, assemblyFileName)) && resolvedAssembly == null)
                            resolvedAssembly = Reflection.Assembly.Load(File.ReadAllBytes(Path.Combine(module.DependencyPath, assemblyFileName)));

                    if (resolvedAssembly == null)
                    {
                        var module = ModuleManager.Session.GetModuleByAssemblyName(assemblyName);

                        if (module != null)
                            resolvedAssembly = module.Assembly;
                    }
                }
                else
                {
                    resolvedAssembly = Reflection.Assembly.LoadFrom(assemblyBinPath);
                }
            }
            catch { throw new Exception("Exception while trying to resolve assembly \"" + assemblyName + "\"."); }

            return resolvedAssembly;
        }

        private void Page_Load(object sender, EventArgs e)
        {
            try
            {
                #region Signing stuff
                //string CompiledKeySignature = "ACQAAASAAACUAAAABgIAAAAkAABSU0ExAAQAAAEAAQDdmZjq3STJVDfs8UsPnWlObTAO3q2S6ksjcEcNZyjndUVSBsg7Taeyl13TQIZvdcs7JotZxqFN/6Ix+A5D3luYGwfalRsPUm4MSo8tHgVg2frb8KkgxJnb8ma7d+KWdliykrAFfaCamF9xDAUxNPapzO4u2ip1m0IiL9Y9JCGFnw==";
                //string AssemblyKeySignature = Convert.ToBase64String(Assembly.GetExecutingAssembly().GetName().GetPublicKey());

                //if (CompiledKeySignature != AssemblyKeySignature)
                //throw new Exception("WebCore modification has been detected. This is a voilation of the BlazeSoft terms of service and has been reported to BlazeSoft.");
                #endregion

                Debug.StartTimer("PageLoad");
                Debug.WriteLine("Page Access Count: {0}", ++PageAccessCount);

                string httpProtocol = (Request.IsSecureConnection) ? "https://" : "http://";
                this.pageUri = new Uri(httpProtocol + Request.Url.Host + Request.RawUrl);

                #if !DEBUG
                if (SettingsManager.Session.GetSystemSetting("IsSetup", false) == false)
                {
                    if (Request.UserHostAddress != "::1")
                    {
                        Response.Write("We're sorry. This web site has not been set up yet.");
                        return;
                    }
                    else
                    {
                        pageUri = pageUri.ChangePageUriPath("/administration/setup");
                    }
                }
                #endif

                if (pageUri.AbsolutePath.ToLower().StartsWith("/themes"))
                {
                    string[] urlParts = pageUri.AbsolutePath.ToLower().Split('/');

                    if (urlParts.Length >= 4)
                    {
                        string themeKey = urlParts[2];
                        string assetKey = string.Join("/", urlParts, 3, urlParts.Length - 3);

                        Theme theme = ThemeManager.Session.GetTheme(themeKey);

                        if (theme == null)
                            throw new Exceptions.PageNotFoundException();

                        Asset asset = theme.GetAsset(assetKey);

                        if (asset == null)
                            throw new Exceptions.PageNotFoundException();

                        Response.ContentType = asset.MimeType;
                        Response.BinaryWrite(asset.Data);

                        Response.End();
                    }
                    else
                        throw new Exceptions.PageNotFoundException();
                }
                else
                {
                    moduleSession = new ModuleSession(HttpContext.Current);

                    foreach (Contract.Module module in moduleSession.GetLoadedModules())
                        if (moduleSession.GetInstance(module) != null)
                            pageUri = moduleSession.GetInstance(module).GetPageUri(pageUri);

                    Uri newPageUri = pageUri;

                    foreach (Contract.Module module in moduleSession.GetLoadedModules())
                        if (moduleSession.GetInstance(module) != null)
                            newPageUri = moduleSession.GetInstance(module).GetPageRedirect(newPageUri);

                    if (newPageUri.ToString() != pageUri.ToString())
                        Response.Redirect(newPageUri.ToString());

                    Contract.Page page = PageManager.Session.GetPageByUri(pageUri);

                    if (page == null)
                        throw new Exceptions.PageNotFoundException();

                    if (page.CompilerLanguage == "Html")
                    {
                        var htmlPageInstance = new Page();
                        htmlPageInstance.CorePage = page;
                        htmlPageInstance.ModuleSession = moduleSession;

                        Response.Write(ThemeManager.Session.ParseTemplate(string.Join("\r\n", page.LatestReversion.Classes.Select(c => c.Data)), page, htmlPageInstance, moduleSession));
                    }
                    else
                    {
                        //PageManager.Session.CompilePages();

                        Page PageInstance = page.Instance; 

                        if (PageInstance != null)
                        {
                            PageInstance.CorePage = page;
                            PageInstance.ModuleSession = moduleSession;

                            Debug.StartTimer("Page:" + page.ID + ":Initialize()");
                            PageInstance.Initialize(); 
                            Debug.StopTimer("Page:" + page.ID + ":Initialize()");
                            string Output = PageInstance.Themes.GetHtmlOutput(page, PageInstance, moduleSession);
                            Debug.StartTimer("Page:" + page.ID + ":Deinitialize()");
                            PageInstance.Deinitialize();
                            Debug.StopTimer("Page:" + page.ID + ":Deinitialize()");

                            Response.Write(Output);
                        }
                    }
                }

                Debug.StopTimer("PageLoad");

                #if DEBUG
                Response.Write(Debug.Output);
                #else
                //if(SettingsCore.Instance.GetSystemSetting("InDev", false))
                    Response.Write(Debug.Output);
                #endif
            }
            catch (ThreadAbortException) { this.Page_Unload(sender, e); }
        }

        private void Page_Unload(object sender, EventArgs e)
        {
            if (moduleSession != null)
            {
                foreach (Contract.Module module in moduleSession.GetLoadedModules())
                {
                    if (moduleSession.GetInstance(module) != null)
                    {
                        Debug.StartTimer("Module:" + module.ID + ":Deinitialize()");

                        try { moduleSession.GetInstance(module).Deinitialize(); }
                        catch { }

                        Debug.StopTimer("Module:" + module.ID + ":Deinitialize()");
                    }
                }
            }
            
            //System.Diagnostics.Trace.TraceInformation(Debug.Output);
        }

        private void Page_Error(object sender, EventArgs e)
        {
            SettingsManager.Session.CheckForFileException();

            Exception exception = CoreUtilities.GetInnerMostException(Server.GetLastError());

            Response.Clear();

            var fakePage = new Contract.Page();
            fakePage.References.AddRange("BlazeSoft.Net.Web.dll,System.dll,System.Core.dll,System.Data.dll,System.Net.dll,System.Xml.dll,System.Xml.Linq.dll,System.Web.dll,Microsoft.CSharp.dll".Split(','));
            var accessObject = exception;

            if (exception.GetType() == typeof(Exceptions.PageNotFoundException))
            {
                Response.StatusCode = 404;

                try
                {
                    Response.Write(ThemeManager.Session.ParseTemplate(ThemeManager.Session.GetTheme("Errors").GetTemplate("404.html"), fakePage, accessObject, moduleSession));
                }
                catch(Exception ex)
                {
                    Response.Write("404");
                    Response.Write(ex.ToString());
                }
            }
            else if(exception.GetType() == typeof(Exceptions.AuthorizationLevelException))
            {
                Response.StatusCode = 403;

                try
                {
                    Response.Write(ThemeManager.Session.ParseTemplate(ThemeManager.Session.GetTheme("Errors").GetTemplate("403.html"), fakePage, accessObject, moduleSession));
                }
                catch
                {
                    Response.Write("403");
                }
            }
            else
            {
                Response.StatusCode = 500;

                try
                {
                    Response.Write(ThemeManager.Session.ParseTemplate(ThemeManager.Session.GetTheme("Errors").GetTemplate("500.html"), fakePage, accessObject, moduleSession));
                }
                catch
                {
                    Response.Write("500<br />");
                    Response.Write("<pre>" + exception.ToString() + "</pre>");
                }
            }

#if DEBUG
            Response.Write(Debug.Output);
#else
                //if(SettingsCore.Instance.GetSystemSetting("InDev", false))
                Response.Write(Debug.Output);
#endif
            Response.End();
        }

        internal string AssemblyDirectory
        {
            get
            {
                string codeBase = System.Reflection.Assembly.GetExecutingAssembly().CodeBase;
                UriBuilder uriBuilder = new UriBuilder(codeBase);
                return Uri.UnescapeDataString(uriBuilder.Path);
            }
        }
    }
}