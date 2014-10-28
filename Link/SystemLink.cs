using BlazeSoft.Net.Web.Contract;
using BlazeSoft.Net.Web.Core;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Web;

namespace BlazeSoft.Net.Web.Link
{
    public class SystemLink
    {
        internal dynamic Parent;

        internal SystemLink(dynamic Parent) { this.Parent = Parent; }

        public bool IsSigned { get { return Parent.CorePage.IsSigned; } }

        public bool IsSystemSigned { get { return Parent.CorePage.IsSystemSigned; } }

        public void RequireSystemSigned()
        {
            if (!Parent.CorePage.IsSystemSigned)
                throw new Exceptions.SystemSignatureException();
        }

        // ---------------------------------------------------------------------------------
        // -   Modules                                                                     -
        // ---------------------------------------------------------------------------------
        public List<string> ModuleIDs
        {
            get
            {
                RequireSystemSigned();

                return (from module in ModuleManager.Session.Modules select module.ID).ToList();
            }
        }

        public List<Contract.Module> Modules
        {
            get
            {
                RequireSystemSigned();

                return ModuleManager.Session.Modules.ToList();
            }
        }

        public dynamic GetModuleProperties(string moduleID)
        {
            RequireSystemSigned();

            Contract.Module module = ModuleManager.Session.Modules.Where(m => m.ID == moduleID).FirstOrDefault();

            if (module != null)
                return new
                {
                    ID = module.ID,
                    Locked = module.Locked,
                    Global = module.Global,
                    References = module.References,
                    LanguageHandler = module.LanguageHandler,
                    LatestReversion = module.LatestReversion
                };

            return null;
        }

        public bool DeleteModule(string ID)
        {
            RequireSystemSigned();

            Contract.Module Module = ModuleManager.Session.Modules.Where(M => M.ID == ID).FirstOrDefault();
            if (Module == null)
                return false;

            ModuleManager.Session.Modules.Remove(Module);

            string FilePath = HttpContext.Current.Server.MapPath(SettingsManager.Session.GetSystemSetting("ModuleDirectory") + "/" + ID + ".bmf");

            if (File.Exists(FilePath))
                File.Delete(FilePath);

            return true;
        }

        public CompilerErrorCollection UpdateModule(dynamic moduleObject)
        {
            RequireSystemSigned();

            CompilerErrorCollection compilerErrors = new CompilerErrorCollection();
            Contract.Module module = ModuleManager.Session.Modules.Where(P => P.ID == moduleObject.ID).FirstOrDefault();

            if (module == null)
            {
                module = new Contract.Module()
                {
                    ID = moduleObject.ID,
                    References = ((ThirdParty.JsonArray)moduleObject.References).Select(r => (string)r).ToList(),
                };

                ModuleReversion moduleReversion = new ModuleReversion() { Version = 1, Updated = DateTime.UtcNow, Classes = new List<ModuleClass>() };
                moduleReversion.Classes = ((ThirdParty.JsonArray)moduleObject.LatestReversion.Classes).Select(p => new ModuleClass() { Name = ((dynamic)p).Name, Data = ((dynamic)p).Data }).ToList();
                
                module.LatestReversion = moduleReversion;
                ModuleManager.Session.AppendModuleReversion(module.ID, moduleReversion);

                ModuleManager.Session.Modules.Add(module);
                module.CompileClasses(out compilerErrors);
                module.Save();
            }
            else if (!module.Locked)
            {
                module.References = ((ThirdParty.JsonArray)moduleObject.References).Select(r => (string)r).ToList();
                ModuleReversion moduleReversion = new ModuleReversion() { Version = module.LatestReversion.Version + 1, Updated = DateTime.UtcNow, Classes = new List<ModuleClass>() };
                moduleReversion.Classes = ((ThirdParty.JsonArray)moduleObject.LatestReversion.Classes).Select(p => new ModuleClass() { Name = ((dynamic)p).Name, Data = ((dynamic)p).Data }).ToList();

                module.LatestReversion = moduleReversion;
                ModuleManager.Session.AppendModuleReversion(module.ID, moduleReversion);

                module.CompileClasses(out compilerErrors);
                module.Save();
            }
            else
                compilerErrors.Add(new CompilerError("", 0, 0, "", "Module is locked"));

            return compilerErrors;
        }

        public CompilerErrorCollection TryCompileModule(dynamic moduleObject)
        {
            RequireSystemSigned();

            CompilerErrorCollection compilerErrors = new CompilerErrorCollection();
            Contract.Module module = ModuleManager.Session.Modules.Where(P => P.ID == moduleObject.ID).FirstOrDefault();

            if (module != null && !module.Locked)
            {
                module = Contract.Module.FromString(module.ToString()); // Create a copy of the module
                module.References = ((ThirdParty.JsonArray)moduleObject.References).Select(r => (string)r).ToList();

                ModuleReversion moduleReversion = new ModuleReversion() { Version = module.LatestReversion.Version + 1, Updated = DateTime.UtcNow, Classes = new List<ModuleClass>() };
                moduleReversion.Classes = ((ThirdParty.JsonArray)moduleObject.LatestReversion.Classes).Select(p => new ModuleClass() { Name = ((dynamic)p).Name, Data = ((dynamic)p).Data }).ToList();

                module.LatestReversion = moduleReversion;
                module.CompileClasses(out compilerErrors, false);
            }
            else
                compilerErrors.Add(new CompilerError("", 0, 0, "", "Module is locked."));

            return compilerErrors;
        }

        // ---------------------------------------------------------------------------------
        // -   Pages                                                                       -
        // ---------------------------------------------------------------------------------
        public List<string> PageIDs
        {
            get
            {
                RequireSystemSigned();

                return (from page in PageManager.Session.Pages select page.ID).ToList();
            }
        }

        public List<Contract.Page> Pages
        {
            get
            {
                RequireSystemSigned();

                return PageManager.Session.Pages.ToList();
            }
        }

        public dynamic GetPageProperties(string pageID)
        {
            RequireSystemSigned();

            Contract.Page page = PageManager.Session.Pages.Where(P => P.ID == pageID).FirstOrDefault();

            if (page != null)
                return new
                {
                    ID = page.ID,
                    Name = page.Name,
                    Domain = page.Domain,
                    Url = page.Url, 
                    CompilerLanguage = page.CompilerLanguage,
                    Locked = page.IsLocked,
                    References = page.References,
                    LatestReversion = page.LatestReversion
                };

            return null;
        }

        public bool DeletePage(string ID)
        {
            RequireSystemSigned();

            Contract.Page Page = PageManager.Session.Pages.Where(P => P.ID == ID).FirstOrDefault();
            if (Page == null)
                return false;

            PageManager.Session.Pages.Remove(Page);

            string FilePath = HttpContext.Current.Server.MapPath(SettingsManager.Session.GetSystemSetting("PageDirectory") + "/" + ID + ".bpf");

            if (File.Exists(FilePath))
                File.Delete(FilePath);

            return true;
        }

        public CompilerErrorCollection UpdatePage(dynamic pageObject)
        {
            RequireSystemSigned();

            CompilerErrorCollection compilerErrors = new CompilerErrorCollection();
            Contract.Page page = PageManager.Session.Pages.Where(P => P.ID == pageObject.ID).FirstOrDefault();

            if (page == null)
            {
                page = new Contract.Page()
                {
                    ID = pageObject.ID,
                    Domain = pageObject.Domain,
                    Url = pageObject.Url,
                    CompilerLanguage = pageObject.CompilerLanguage,
                    References = ((ThirdParty.JsonArray)pageObject.References).Select(r => (string)r).ToList(),
                };

                PageReversion pageReversion = new PageReversion() { Version = 1, Updated = DateTime.UtcNow, Classes = new List<PageClass>() };
                pageReversion.Classes = ((ThirdParty.JsonArray)pageObject.LatestReversion.Classes).Select(p => new PageClass() { Name = ((dynamic)p).Name, Data = ((dynamic)p).Data }).ToList();

                page.LatestReversion = pageReversion;
                PageManager.Session.AppendPageReversion(page.ID, pageReversion);

                PageManager.Session.Pages.Add(page);
                page.CompileClasses(out compilerErrors);
                page.Save();
            }
            else if (!page.IsLocked)
            {
                page.Domain = pageObject.Domain;
                page.Url = pageObject.Url;
                page.CompilerLanguage = pageObject.CompilerLanguage;
                page.References = ((ThirdParty.JsonArray)pageObject.References).Select(r => (string)r).ToList();

                PageReversion pageReversion = new PageReversion() { Version = page.LatestReversion.Version + 1, Updated = DateTime.UtcNow, Classes = new List<PageClass>() };
                pageReversion.Classes = ((ThirdParty.JsonArray)pageObject.LatestReversion.Classes).Where(p => p != null).Select(p => new PageClass() { Name = ((dynamic)p).Name, Data = ((dynamic)p).Data }).ToList();
                //page.Reversions.Add(pageReversion);

                page.LatestReversion = pageReversion;
                PageManager.Session.AppendPageReversion(page.ID, pageReversion);

                page.CompileClasses(out compilerErrors);
                page.Save();
            }
            else
                compilerErrors.Add(new CompilerError("", 0, 0, "", "Page is locked."));

            return compilerErrors;
        }

        /// <summary>
        /// Tries to compile a page to any possible return errors. 
        /// This method does not save the page like <see cref="SystemLink.UpdatePage"/>.
        /// </summary>
        /// <param name="pageObject"></param>
        /// <returns></returns>
        public CompilerErrorCollection TryCompilePage(dynamic pageObject)
        {
            RequireSystemSigned();

            CompilerErrorCollection compilerErrors = new CompilerErrorCollection();
            Contract.Page page = PageManager.Session.Pages.Where(P => P.ID == pageObject.ID).FirstOrDefault();

            if (page != null && !page.IsLocked)
            {
                page = Contract.Page.FromString(page.ToString()); // Create a copy of the page

                page.Domain = pageObject.Domain;
                page.Url = pageObject.Url;
                page.CompilerLanguage = pageObject.CompilerLanguage;
                page.References = ((ThirdParty.JsonArray)pageObject.References).Select(r => (string)r).ToList();

                PageReversion pageReversion = new PageReversion() { Version = page.LatestReversion.Version + 1, Updated = DateTime.UtcNow, Classes = new List<PageClass>() };
                
                pageReversion.Classes = ((ThirdParty.JsonArray)pageObject.LatestReversion.Classes).Select(p => new PageClass() { Name = ((dynamic)p).Name, Data = ((dynamic)p).Data }).ToList();
                page.LatestReversion = pageReversion;

                page.CompileClasses(out compilerErrors, false);
            }
            else
                compilerErrors.Add(new CompilerError("", 0, 0, "", "Page is locked."));

            return compilerErrors;
        }

        // ---------------------------------------------------------------------------------
        // -   Themes                                                                      -
        // ---------------------------------------------------------------------------------
        public CompilerErrorCollection TryCompileTheme(dynamic themeObject)
        {
            CompilerErrorCollection compilerErrorCollection = new CompilerErrorCollection();
            return compilerErrorCollection;
        }

        public CompilerErrorCollection UpdateTheme(dynamic themeObject)
        {
            RequireSystemSigned();

            CompilerErrorCollection compilerErrorCollection = new CompilerErrorCollection();

            string themeID = themeObject.ID;
            dynamic latestReversion = themeObject.LatestReversion;
            dynamic[] templates = ((ThirdParty.JsonArray)latestReversion.Classes).ToArray();

            string themeDirectory = HttpContext.Current.Server.MapPath(Path.Combine(SettingsManager.Session.GetSystemSetting("ThemeDirectory"), themeID));

            if (!Directory.Exists(themeDirectory))
            {
                //create theme directory
                //create asset directory
            }

            foreach (dynamic template in templates)
            {
                string templatePath = Path.Combine(themeDirectory, template.Name);
                File.WriteAllText(templatePath, template.Data);
            }

            return compilerErrorCollection;
        }

        public dynamic GetThemeProperties(string themeID)
        {
            RequireSystemSigned();

            Theme theme = ThemeManager.Session.GetTheme(themeID);

            var LatestReversion = new
            {
                Classes = new List<dynamic>()
            };

            foreach (string template in theme.Templates)
                LatestReversion.Classes.Add(new { Name = template, Data = theme.GetTemplate(template) });

            return new
            {
                ID = themeID,
                Name = "",
                CompilerLanguage = "n/a",
                LatestReversion = LatestReversion
            };
        }

        // ---------------------------------------------------------------------------------
        // -   Debug                                                                       -
        // ---------------------------------------------------------------------------------
        public void OutputCompileInformation()
        {
            //RequireSystemSigned();

            var Response = HttpContext.Current.Response;

            Dictionary<string, CompilerErrorCollection> moduleErrors = Core.ModuleManager.Session.CompileModules();
            Dictionary<string, CompilerErrorCollection> pageErrors = Core.PageManager.Session.CompilePages();

            if (pageErrors.Count != 0 || moduleErrors.Count != 0)
            {
                string output = string.Empty;

                foreach (string module in moduleErrors.Keys)
                {
                    output += string.Format("<h4>{0} has {1} errors.</h4>\r\n", module, moduleErrors[module].Count);

                    output += "<pre style='color: #980000;'>";
                    foreach (CompilerError compilerError in moduleErrors[module])
                        output += string.Format("{0} {1} on line {2}.\r\n", (compilerError.IsWarning ? "Warning" : "Error"), compilerError.ErrorText, compilerError.Line);
                    output += "</pre>\r\n\r\n";
                }

                foreach (string page in pageErrors.Keys)
                {
                    output += string.Format("<h4>{0} has {1} errors.</h4>\r\n", page, pageErrors[page].Count);

                    output += "<pre style='color: #980000;'>";
                    foreach (CompilerError compilerError in pageErrors[page])
                        output += string.Format("{0} {1} on line {2}.\r\n", (compilerError.IsWarning ? "Warning" : "Error"), compilerError.ErrorText, compilerError.Line);
                    output += "</pre>\r\n\r\n";
                }

                Response.Write(output);
            }
            else
            {
                Core.ModuleManager.Session.SaveModules();
                Core.PageManager.Session.SavePages();
            }
        }

        public void OutputDebugInformation()
        {
            RequireSystemSigned();

            var Response = HttpContext.Current.Response;

            Response.Write(string.Format("<hr />Debug<br />Uptime: {0:%d} days {0:%h} hours {0:%m} minutes {0:%s} seconds.<hr />", (DateTime.Now - Core.Web.StartUpTime)));

            Response.Write(string.Format("<hr />{0} settings(s) are loaded into memory.<hr />", Core.SettingsManager.Session.settings.Count));
            Response.Write("<pre style='color:#D3D3D3;'>");
            Response.Write(string.Format("{0,-10}{1,-25}{2,-25}{3}\r\n", "Type", "Key", "Value", "Last Updated"));
            foreach (Contract.Setting setting in Core.SettingsManager.Session.settings)
                Response.Write(string.Format("{0,-10}{1,-25}{2,-25}{3}\r\n",
                    setting.Type,
                    setting.Key,
                    setting.Value,
                    setting.LastUpdated));
            Response.Write("</pre>");

            Response.Write(string.Format("<hr />{0} module(s) are loaded into memory.<hr />", Core.ModuleManager.Session.Modules.Count));
            Response.Write("<pre style='color:#D3D3D3;'>");
            foreach (Contract.Module module in Core.ModuleManager.Session.Modules)
                Response.Write(string.Format("{0, -25}{1}\r\n", module.ID, module.DependencyPath));
            Response.Write("</pre>");

            Response.Write(string.Format("<hr />{0} pages(s) are loaded into memory.<hr />", Core.PageManager.Session.Pages.Count));
            Response.Write("<pre style='color:#D3D3D3;'>");
            Response.Write(string.Format("{0,-25}{1}\r\n", "Name", "Url"));
            foreach (Contract.Page page in Core.PageManager.Session.Pages)
                Response.Write(string.Format("{0,-25}{1}\r\n", page.ID, page.Url));
            Response.Write("</pre>");

            Response.Write(string.Format("<hr />{0} scheduled tasks.<hr />", Scheduler.ScheduledItems.Count));
            Response.Write("<pre style='color:#D3D3D3;'>");
            Response.Write(string.Format("{0,-25}{1,-25}{2}\r\n", "Name", "Last Run", "Next Run"));
            foreach (SchedulerItem schedulerItem in Scheduler.ScheduledItems)
                Response.Write(string.Format("{0,-25}{1,-25}{2}\r\n", schedulerItem.ID, DateTime.UtcNow - schedulerItem.LastRan, schedulerItem.Delay - (DateTime.UtcNow - schedulerItem.LastRan)));
            Response.Write("</pre>");
        }

        public void RunCommand(string commandText)
        {
            System.Diagnostics.ProcessStartInfo cmdProcessInfo = new System.Diagnostics.ProcessStartInfo("cmd", "/c " + commandText)
            {
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = HttpContext.Current.Server.MapPath("./bin/"),
            };

            System.Diagnostics.Process cmdProcess = new System.Diagnostics.Process() { StartInfo = cmdProcessInfo };
            cmdProcess.Start();

            HttpContext.Current.Response.ContentType = "text/plain";
            HttpContext.Current.Response.Write(cmdProcess.StandardOutput.ReadToEnd());
            HttpContext.Current.Response.Flush();
        }

        // ---------------------------------------------------------------------------------
        // -   Core Members                                                                -
        // ---------------------------------------------------------------------------------
        internal CoreMember[] CoreMembers
        {
            get
            {
                //return "root:ee801a5d735d9eff775912a97ba46dce582e1e15818c89dc961bb502d9a28f82:jd3di89g".SplitByNewLine().Select(m => CoreMember.FromString(m)).ToArray();
                return ((string)BlazeSoft.Net.Web.Core.SettingsManager.Session.GetSystemSetting("CoreMembers", "root:ee801a5d735d9eff775912a97ba46dce582e1e15818c89dc961bb502d9a28f82:jd3di89g")).SplitByNewLine().Select(m => CoreMember.FromString(m)).ToArray();
            }
        }

        private static Dictionary<Guid, CoreMember> MemberSessions = new Dictionary<Guid, CoreMember>();

        internal void UpdateCoreMembers(CoreMember[] coreMembers)
        {
            string coreMembersSettingString = string.Join("\r\n", coreMembers.Select(m => m.ToString()).ToArray());
            BlazeSoft.Net.Web.Core.SettingsManager.Session.UpdateSystemSetting("CoreMembers", coreMembersSettingString);
        }

        public bool TryAuthenticateCoreMember(string login, string password)
        {
            return this.GetCoreMember(login, password) != null;
        }

        public bool TryGenerateCoreMemberSession(string login, string password, out Guid sessionId)
        {
            var member = this.GetCoreMember(login, password);
            sessionId = Guid.NewGuid();

            if (member != null)
                MemberSessions.Add(sessionId, member);

            return member != null;
        }

        public bool TryAuthenticateCoreMemberSession(Guid sessionId, out CoreMember member)
        {
            member = null;

            if (MemberSessions.ContainsKey(sessionId))
                member = MemberSessions[sessionId];

            return member != null;
        }

        public CoreMember GetCoreMember(string login, string password, string getLogin = null)
        {
            var coreMembers = this.CoreMembers;

            var member = coreMembers.Where(m => m.Login == login).FirstOrDefault();
            var returnMember = member;

            if (member == null)
                return null;

            if (getLogin != null)
                returnMember = coreMembers.Where(m => m.Login == getLogin).FirstOrDefault();

            string passwordHash = password.SecureHash(member.PasswordSalt);
            if (member.PasswordHash != passwordHash)
                return null;

            return returnMember;
        }

        public bool AddCoreMember(string login, string password, string newLogin, string newPassword)
        {
            if(!this.TryAuthenticateCoreMember(login, password))
                return false;

            var coreMembers = this.CoreMembers.ToList();
            if (coreMembers.Where(m => m.Login == newLogin).Count() > 0)
                return false;

            if(!Utilities.Verification.VerifyCharacters(login, null, ":"))
                return false;

            string newPasswordSalt = Utilities.NewRandomString(8);
            string newPasswordHash = newPassword.SecureHash(newPasswordSalt);

            coreMembers.Add(new CoreMember() { Login = newLogin, PasswordHash = newPasswordHash, PasswordSalt = newPasswordSalt });
            this.UpdateCoreMembers(coreMembers.ToArray());
            return true;
        }

        public bool RemoveCoreMember(string login, string password, string removeLogin)
        {
            if (!this.TryAuthenticateCoreMember(login, password))
                return false;

            var coreMembers = this.CoreMembers.ToList();
            if (coreMembers.Where(m => m.Login == removeLogin).Count() == 0)
                return false;

            coreMembers.Remove(coreMembers.Where(m => m.Login == removeLogin).FirstOrDefault());
            this.UpdateCoreMembers(coreMembers.ToArray());
            return true;
        }

        public class CoreMember
        {
            public string Login { get; set; }
            public string PasswordHash { get; set; }
            public string PasswordSalt { get; set; }

            public override string ToString()
            {
                return string.Format("{0}:{1}:{2}", this.Login, this.PasswordHash, this.PasswordSalt);
            }

            public static CoreMember FromString(string memberString)
            {
                string[] memberParts = memberString.Split(':');

                if (memberParts.Length >= 3)
                    return new CoreMember() { Login = memberParts[0], PasswordHash = memberParts[1], PasswordSalt = memberParts[2] };
                else
                    return null;
            }
        }
    }
}