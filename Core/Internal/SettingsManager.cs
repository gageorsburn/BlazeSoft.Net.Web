using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.IO;

using BlazeSoft.Net.Web.Serialization;
using BlazeSoft.Net.Web.Contract;

namespace BlazeSoft.Net.Web.Core
{
    internal sealed class SettingsManager
    {
        private readonly string settingsPath = "./Data/Settings.bsf";
        private static SettingsManager session;
        private bool fileExceptionHasOccured = false;

        public static SettingsManager Session
        {
            get
            {
                if (session == null)
                    session = new SettingsManager();

                return session;
            }
        }

        internal List<Setting> settings = new List<Setting>();

        public void CheckForFileException()
        {
            if (fileExceptionHasOccured)
                Save();
        }

        public SettingsManager()
        {
            this.Load();
        }

        public void Load()
        {
            string settingsFile = HttpContext.Current.Server.MapPath(settingsPath);

            if (File.Exists(settingsFile))
                foreach (string setting in File.ReadAllLines(settingsFile))
                    settings.Add((Setting)Serialization.XmlSerializer.SettingsSerializer.DeserializeFromBase64(setting));
                    //settings.Add((Setting)Serialization.XmlSerializer.SettingsSerializer.DeserializeFromBase64(Encryption.DecryptString(Setting)));
        }

        public void Save()
        {
            this.fileExceptionHasOccured = false;

            string settingsFile = HttpContext.Current.Server.MapPath(settingsPath);

            List<string> settingsLines = new List<string>();

            foreach (Setting setting in settings)
                settingsLines.Add(Serialization.XmlSerializer.SettingsSerializer.SerializeToBase64(setting));
                //SettingsLines.Add(Encryption.EncryptString(Serialization.XmlSerializer.SettingsSerializer.SerializeToBase64(Setting)));


            try { File.WriteAllLines(settingsFile, settingsLines); }
            catch (IOException) { this.fileExceptionHasOccured = true; }
        }

        internal dynamic GetSetting(string Key, string ModuleKey, dynamic DefaultValue = null)
        {
            Setting ReturnSetting = settings.Where(Setting => Setting.Key.Equals(Key, StringComparison.CurrentCultureIgnoreCase) && Setting.ModuleKey == ModuleKey && Setting.Type == SettingType.Module).FirstOrDefault();
            
            if (ReturnSetting == null)
                return DefaultValue;
            else
                return ReturnSetting.Value;
        }

        internal dynamic GetGlobalSetting(string Key, dynamic DefaultValue = null)
        {
            Setting ReturnSetting = settings.Where(Setting => Setting.Key.Equals(Key, StringComparison.CurrentCultureIgnoreCase) && Setting.Type == SettingType.Global).FirstOrDefault();
            
            if (ReturnSetting == null)
                return DefaultValue;
            else
                return ReturnSetting.Value;
        }

        internal dynamic GetSystemSetting(string Key, dynamic DefaultValue = null)
        {
            Setting ReturnSetting = settings.Where(Setting => Setting.Key.Equals(Key, StringComparison.CurrentCultureIgnoreCase) && Setting.Type == SettingType.System).FirstOrDefault();
            
            if (ReturnSetting == null)
                return DefaultValue;
            else
                return ReturnSetting.Value;
        }

        internal void UpdateSetting(string Key, string ModuleKey, dynamic Value)
        {
            Setting UpdateSetting = settings.Where(Setting => Setting.Key.Equals(Key, StringComparison.CurrentCultureIgnoreCase) && Setting.ModuleKey == ModuleKey && Setting.Type == SettingType.Module).FirstOrDefault();

            if (UpdateSetting == null)
            {
                UpdateSetting = new Setting()
                {
                    Type = SettingType.Module,
                    LastUpdated = DateTime.UtcNow,
                    ModuleKey = "",
                    Key = Key,
                    Value = Value
                };

                settings.Add(UpdateSetting);
            }
            else
                UpdateSetting.Value = Value;

            this.Save();
        }

        internal void UpdateGlobalSetting(string Key, dynamic Value)
        {
            Setting UpdateSetting = settings.Where(Setting => Setting.Key.Equals(Key, StringComparison.CurrentCultureIgnoreCase) && Setting.Type == SettingType.Global).FirstOrDefault();

            if (UpdateSetting == null)
            {
                UpdateSetting = new Setting()
                {
                    Type = SettingType.Global,
                    LastUpdated = DateTime.UtcNow,
                    ModuleKey = "",
                    Key = Key,
                    Value = Value
                };

                settings.Add(UpdateSetting);
            }
            else
                UpdateSetting.Value = Value;

            this.Save();
        }

        internal void UpdateSystemSetting(string Key, dynamic Value)
        {
            Setting UpdateSetting = settings.Where(Setting => Setting.Key.Equals(Key, StringComparison.CurrentCultureIgnoreCase) && Setting.Type == SettingType.System).FirstOrDefault();

            if (UpdateSetting == null)
            {
                UpdateSetting = new Setting()
                {
                    Type = SettingType.System,
                    LastUpdated = DateTime.UtcNow,
                    ModuleKey = "",
                    Key = Key,
                    Value = Value
                };

                settings.Add(UpdateSetting);
            }
            else
                UpdateSetting.Value = Value;

            this.Save();
        }

        public override string ToString()
        {
            return XmlSerializer.SettingsSerializer.Serialize(this);
        }
    }
}