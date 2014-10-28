using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

using BlazeSoft.Net.Web.Core;

namespace BlazeSoft.Net.Web
{
    public class ThemeLink
    {
        internal dynamic Parent;

        internal ThemeLink(dynamic Parent) { this.Parent = Parent; }

        private string pageContent = "";

        private Theme theme;
        internal string template;

        private Dictionary<string, object> variables = new Dictionary<string, object>();


        public Theme[] Themes
        {
            get
            {
                List<Theme> themeList = new List<Theme>();

                foreach (string themeFolder in Directory.GetDirectories(HttpContext.Current.Server.MapPath(SettingsManager.Session.GetSystemSetting("ThemeDirectory"))))
                {
                    DirectoryInfo themeDirectory = new DirectoryInfo(themeFolder);
                    themeList.Add(Web.Theme.GetTheme(themeDirectory.Name));
                }

                return themeList.ToArray();
            }
        }

        public Theme GetTheme()
        {
            if (this.theme == null)
                this.Theme = "Default";

            return theme;
        }

        public string Theme
        {
            set
            {
                this.theme = ThemeManager.Session.GetTheme(value);

                if (this.theme == null)
                    throw new Exceptions.ThemeNotFoundException();
            }
        }

        public string Template
        {
            set
            {
                if (this.theme == null)
                    this.Theme = "Default";

                if (value.ToLower().StartsWith("https://"))
                    this.template = this.theme.GetRemoteTemplate(value);
                else
                    this.template = this.theme.GetTemplate(value);

                if (template == null)
                    throw new Exceptions.TemplateNotFoundException(value);
            }
        }

        public void SetVariable(string key, object value)
        {
            if (variables.ContainsKey(key))
                variables[key] = value;
            else
                variables.Add(key, value);
        }

        internal string GetHtmlOutput(Contract.Page page, object instance, ModuleSession moduleSession)
        {
            if (this.theme == null)
                this.Theme = "Default";

            if (this.template == null)
                this.Template = "Default.html";

            string tmpTemplate = this.template;

            foreach (string Key in variables.Keys)
                tmpTemplate = tmpTemplate.Replace("<!--{@" + Key + "}-->", variables[Key].ToString()).Replace("{@" + Key + "}", variables[Key].ToString());

            return ThemeManager.Session.ParseTemplate(tmpTemplate, page, instance, moduleSession);
        }

        public void Write(object instance, params object[] parameters)
        {
            string tmpString = instance.ToString();

            for (int i = 0; i < parameters.Length; i++)
                tmpString.Replace("{" + i + "}", parameters[i] != null ? parameters[i].ToString() : "");

            pageContent += tmpString;
        }
    }
}