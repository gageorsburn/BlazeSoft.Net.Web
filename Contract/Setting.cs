using System;
using System.Runtime.Serialization;

namespace BlazeSoft.Net.Web.Contract
{
    [DataContract]
    public enum SettingType
    {
        [EnumMember]
        System,
        [EnumMember]
        Global,
        [EnumMember]
        Module
    }

    [DataContract]
    public class Setting
    {
        public bool HasChanged = false;

        [DataMember]
        public SettingType Type { get; set; }

        [DataMember]
        public DateTime LastUpdated { get; set; }

        [DataMember]
        public string ModuleKey { get; set; }

        [DataMember]
        public string Key { get; set; }

        [DataMember]
        public dynamic Value { get; set; }

        public override string ToString()
        {
            return string.Format("{0}={1}", Key, Value != null ? Value : "");
        }
    }
}