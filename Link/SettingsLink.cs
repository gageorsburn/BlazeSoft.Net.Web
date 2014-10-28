
using BlazeSoft.Net.Web.Core;

namespace BlazeSoft.Net.Web.Link
{
    public class SettingsLink
    {
        internal dynamic Parent;
        internal SettingsLink(dynamic Parent) { this.Parent = Parent; }

        public dynamic GetSetting(string Key, string ModuleKey, dynamic DefaultValue = null)
        {
            return SettingsManager.Session.GetSetting(Key, ModuleKey, DefaultValue);
        }

        public dynamic GetGlobalSetting(string Key, dynamic DefaultValue = null)
        {
            return SettingsManager.Session.GetGlobalSetting(Key, DefaultValue);
        }

        public dynamic GetSystemSetting(string Key, dynamic DefaultValue = null)
        {
            if (!Parent.CorePage.IsSystemSigned)
                throw new Exceptions.SystemSignatureException();

            return SettingsManager.Session.GetSystemSetting(Key, DefaultValue);
        }

        public void UpdateSetting(string Key, string ModuleKey, dynamic Value)
        {
            SettingsManager.Session.UpdateSetting(Key, ModuleKey, Value);
        }

        public void UpdateGlobalSetting(string Key, dynamic Value)
        {
            SettingsManager.Session.UpdateGlobalSetting(Key, Value);
        }

        public void UpdateSystemSetting(string Key, dynamic Value)
        {
            if (!Parent.CorePage.IsSystemSigned)
                throw new Exceptions.SystemSignatureException();
            
            SettingsManager.Session.UpdateSystemSetting(Key, Value);
        }
    }
}