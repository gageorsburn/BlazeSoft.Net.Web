using System;
using System.Collections.Generic;
using System.Web;
using System.IO;
using System.Net;

using System.Reflection;
using System.CodeDom.Compiler;

using BlazeSoft.Net.Web.Core;
using Microsoft.CSharp.RuntimeBinder;

namespace BlazeSoft.Net.Web.Core
{
    internal class ThemeManager
    {
        #region Singleton
        private static ThemeManager session;

        public static ThemeManager Session
        {
            get
            {
                if (session == null)
                    session = new ThemeManager();

                return session;
            }
        }
        #endregion

        public Theme GetTheme(string key)
        {
            return Theme.GetTheme(key);
        }

        internal string ParseTemplate(string template, Contract.Page page, dynamic instance, ModuleSession moduleSession)
        {
            Debug.StartTimer("ThemeManager:ParseTemplate()");
            template = ParseBlocks(template, page, instance, moduleSession);
            Debug.StopTimer("ThemeManager:ParseTemplate()");

            return template;
        }

        private string EvalCode(string code, Contract.Page page, dynamic instance)
        {
            Assembly assembly = null;
            string codeHash = code.Hash();

            if (!CacheManager.Session.CachedItemExists("themeCode", codeHash))
            {
                CodeDomProvider codeDomProvider = CodeDomProvider.CreateProvider("CSharp");
                CompilerParameters compilerParameters = new CompilerParameters();

                compilerParameters.ReferencedAssemblies.AddRange(page.References.ToArray());

                compilerParameters.CompilerOptions = string.Format("/t:library /lib:{0},{1}", HttpContext.Current.Server.MapPath("./bin/"), HttpContext.Current.Server.MapPath("./bin/Libraries/"));
                compilerParameters.GenerateInMemory = false;

                #region sourceCode
                string sourceCode = @"using System;
using System.Xml;
using System.Data;
using System.Web;
using System.Text;

using BlazeSoft.Net.Web;
using BlazeSoft.Net.Web.Core;
using BlazeSoft.Net.Web.Link;

public class CodeEvaler
{
    public string Output = string.Empty;

    public dynamic Parent;

    public HttpContext Http;
    public ThemeLink Themes;
    public SettingsLink Settings;
    public dynamic Modules;
    public PageLink Pages;

    public void Write(object message, params object[] parameters)
    {
        string text = message != null ? message.ToString() : """";
        for (int parameter = 0; parameter < parameters.Length; parameter++)
            text = text.Replace(""{"" + parameter + ""}"", parameters[parameter] != null ? parameters[parameter].ToString() : ""{NULL}"");
        Output += text;
    }

    public void WriteLine(object message, params object[] parameters)
    {
        string text = message != null ? message.ToString() : """";
        for (int parameter = 0; parameter < parameters.Length; parameter++)
            text = text.Replace(""{"" + parameter + ""}"", parameters[parameter] != null ? parameters[parameter].ToString() : ""{NULL}"");
        Output += text + ""\r\n"";
    }

    public void Include(string templateFile)
    {
        Write(Themes.GetTheme().GetTemplateAndParse(templateFile, Parent));
    }

    public void Eval()
    {
        " + code + @"
    }
}";
#endregion

                CompilerResults compilerResults = codeDomProvider.CompileAssemblyFromSource(compilerParameters, sourceCode);

                if (compilerResults.Errors.Count > 0)
                    throw new Exceptions.TemplateSyntaxException(string.Format("{0} on line {1}", compilerResults.Errors[0].ErrorText, compilerResults.Errors[0].Line));

                assembly = Assembly.Load(File.ReadAllBytes(compilerResults.PathToAssembly));
                CacheManager.Session.AddCachedItem("themeCode", codeHash, assembly);
            }

            assembly = CacheManager.Session.GetCachedItem("themeCode", codeHash);

            dynamic evalInstance = assembly.CreateInstance("CodeEvaler");

            try
            {
                evalInstance.Http = instance.Http;
                evalInstance.Themes = instance.Themes;
                evalInstance.Settings = instance.Settings;
                evalInstance.Modules = instance.Modules;
                evalInstance.Pages = instance.Pages;
            }
            catch (RuntimeBinderException)
            {
                evalInstance.Http = HttpContext.Current;
            }

            evalInstance.Parent = instance;

            try
            {
                evalInstance.Eval();
            }
            catch(Exception e)
            {
                throw new Exceptions.TemplateSyntaxException(e.Message);
            }

            return evalInstance.Output;
        }

        public string ParseBlocks(string Template, Contract.Page Page, dynamic This, ModuleSession ModuleSession)
        {
            int OpenCodeTags = 0;
            int StartingCodeTag = 0;
            int StartCodeIndex = 0;
            int OpenVariableTags = 0;
            int StartingVariableTag = 0;
            int StartVariableIndex = 0;

            for (int i = 0; i < Template.Length; i++)
            {
                if (Template[i] == '{' && (i > 0 && Template[i - 1] != '\\'))
                    OpenCodeTags++;
                else if (Template[i] == '}' && (i > 0 && Template[i - 1] != '\\') && OpenCodeTags > 0)
                    OpenCodeTags--;

                if (Template[i] == '[' && (i > 0 && Template[i - 1] != '\\'))
                    OpenVariableTags++;
                else if (Template[i] == ']' && (i > 0 && Template[i - 1] != '\\') && OpenVariableTags > 0)
                    OpenVariableTags--;

                if (Template[i] == '{' && (i > 0 && Template[i - 1] == '#') && StartingCodeTag == 0)
                {
                    StartCodeIndex = i;
                    StartingCodeTag = OpenCodeTags;
                }
                if (OpenCodeTags == StartingCodeTag - 1 && Template[i] == '}')
                {
                    StartingCodeTag = 0;

                    string Code = Template.Substring(StartCodeIndex + 1, i - StartCodeIndex - 1).Replace("\\}", "}").Replace("\\{", "{");
                    string EvaledCode = this.EvalCode(Code, Page, This);

                    string tmpTemplate = Template.Substring(0, StartCodeIndex - 1) + EvaledCode + Template.Substring(i + 1);

                    i += tmpTemplate.Length - Template.Length;

                    Template = tmpTemplate;
                }

                if (Template[i] == '[' && (i > 0 && Template[i - 1] == '#') && StartingVariableTag == 0)
                {
                    StartVariableIndex = i;
                    StartingVariableTag = OpenVariableTags;
                }
                if (OpenVariableTags == StartingVariableTag - 1 && Template[i] == ']')
                {
                    StartingVariableTag = 0;

                    string Code = "Write(" + Template.Substring(StartVariableIndex + 1, i - StartVariableIndex - 1).Replace("\\]", "]").Replace("\\[", "[") + ");";
                    string EvaledCode = this.EvalCode(Code, Page, This);

                    string tmpTemplate = Template.Substring(0, StartVariableIndex - 1) + EvaledCode + Template.Substring(i + 1);

                    i += tmpTemplate.Length - Template.Length;

                    Template = tmpTemplate;
                }
            }

            return Template;
        }
    }
}

namespace BlazeSoft.Net.Web
{
    public class Theme
    {
        public string Key { get; set; }

        /// <summary>
        /// Returns a list of templates installed in the Themes directory.
        /// </summary>
        public string[] Templates
        {
            get
            {
                List<string> templateList = new List<string>();

                foreach (string templateFile in Directory.GetFiles(HttpContext.Current.Server.MapPath(SettingsManager.Session.GetSystemSetting("ThemeDirectory") + "/" + Key + "/")))
                {
                    FileInfo templateFileInfo = new FileInfo(templateFile);
                    templateList.Add(templateFileInfo.Name);
                }

                return templateList.ToArray();
            }
        }

        private Theme() { }

        public static Theme GetTheme(string ThemeKey)
        {
            DirectoryInfo ThemeDirectory = new DirectoryInfo(HttpContext.Current.Server.MapPath(string.Format("{0}/{1}", SettingsManager.Session.GetSystemSetting("ThemeDirectory"), ThemeKey)));

            if (ThemeDirectory.Exists)
                return new Theme()
                {
                    Key = ThemeKey
                };
            else
                return null;
        }

        public Asset GetAsset(string AssetKey)
        {
            return Asset.GetAsset(this, AssetKey);
        }

        public string GetTemplate(string TemplateKey)
        {
            string TemplateFile = HttpContext.Current.Server.MapPath(string.Format("{0}/{1}/{2}", SettingsManager.Session.GetSystemSetting("ThemeDirectory"), this.Key, TemplateKey));

            if (File.Exists(TemplateFile))
                return File.ReadAllText(TemplateFile).Replace("<!--{Theme:PublicDirectory}-->", string.Format("/Themes/{0}", this.Key)).Replace("{Theme:PublicDirectory}", string.Format("/Themes/{0}", this.Key));
            else
                return null;
        }

        public string GetRemoteTemplate(string templateUrl)
        {
            try
            {
                using (WebClient themeWebClient = new WebClient())
                    return themeWebClient.DownloadString(templateUrl).Replace("<!--{Theme:PublicDirectory}-->", string.Format("/Themes/{0}", this.Key)).Replace("{Theme:PublicDirectory}", string.Format("/Themes/{0}", this.Key)); ;
            }
            catch
            {
                return null;
            }
        }

        public string GetTemplateAndParse(string templateKey, dynamic page)
        {
            string templateData = GetTemplate(templateKey);
            templateData = ThemeManager.Session.ParseTemplate(templateData, page.CorePage, page, ModuleSession.Get(HttpContext.Current));
            return templateData;
        }
    }

    public class Asset
    {
        public Theme Theme;
        public string Key;
        public string FileExtention;
        public byte[] Data;
        public string MimeType { get { return CoreUtilities.GetMimeType(FileExtention); } }

        private Asset() { }

        internal static Asset GetAsset(Theme Theme, string AssetKey)
        {
            FileInfo AssetFile = new FileInfo(HttpContext.Current.Server.MapPath(string.Format("{0}/{1}/Assets/{2}", SettingsManager.Session.GetSystemSetting("ThemeDirectory"), Theme.Key, AssetKey)));

            if (!AssetFile.Directory.FullName.StartsWith(HttpContext.Current.Server.MapPath(string.Format("{0}/{1}/Assets/", SettingsManager.Session.GetSystemSetting("ThemeDirectory"), Theme.Key))))
                return null;
            else if (AssetFile.Exists)
                return new Asset()
                {
                    Theme = Theme,
                    Key = AssetKey,
                    FileExtention = AssetFile.Extension,
                    Data = File.ReadAllBytes(AssetFile.FullName)
                };
            else
                return null;
        }
    }
}