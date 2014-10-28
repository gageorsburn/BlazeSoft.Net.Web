using System;
using System.IO;
using System.Text;
using System.Runtime.Serialization;

using BlazeSoft.Net.Web.Contract;

namespace BlazeSoft.Net.Web.Serialization
{
    public class XmlSerializer
    {
        public static string ToXml(object instance)
        {
            System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(instance.GetType());

            using (MemoryStream memoryStream = new MemoryStream())
            {
                serializer.Serialize(memoryStream, instance);
                return Encoding.Default.GetString(memoryStream.ToArray());
            }
        }

        public static object FromXml(Type type, string Xml)
        {
            System.Xml.Serialization.XmlSerializer serializer = new System.Xml.Serialization.XmlSerializer(type);

            using (MemoryStream memoryStream = new MemoryStream(Encoding.Default.GetBytes(Xml)))
                return serializer.Deserialize(memoryStream);
        }

        internal static readonly XmlSerializer SettingsSerializer = new XmlSerializer(typeof(Setting));
        internal static readonly XmlSerializer PageSerializer = new XmlSerializer(typeof(Contract.Page));
        internal static readonly XmlSerializer PageReversionSerializer = new XmlSerializer(typeof(Contract.PageReversion));
        internal static readonly XmlSerializer ModuleSerializer = new XmlSerializer(typeof(Contract.Module));
        internal static readonly XmlSerializer ModuleReversionSerializer = new XmlSerializer(typeof(Contract.ModuleReversion));
             
        private DataContractSerializer serializer;

        public XmlSerializer(Type type)
        {
            serializer = new DataContractSerializer(type);
        }

        public string Serialize(object instance)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                serializer.WriteObject(memoryStream, instance);
                return Encoding.Default.GetString(memoryStream.ToArray());
            }
        }

        public string SerializeToBase64(object instance)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                serializer.WriteObject(memoryStream, instance);
                return Convert.ToBase64String(memoryStream.ToArray());
            }
        }

        public object Deserialize(string data)
        {
            try
            {
                using (MemoryStream memoryStream = new MemoryStream(Encoding.Default.GetBytes(data)))
                    return serializer.ReadObject(memoryStream);
            }
            catch (SerializationException serializationException)
            {
                throw serializationException;
            }
        }

        public object DeserializeFromBase64(string data)
        {
            try
            {
                using (MemoryStream memoryStream = new MemoryStream(Convert.FromBase64String(data)))
                    return serializer.ReadObject(memoryStream);
            }
            catch (SerializationException)
            {
                return null;
            }
        }
    }
}