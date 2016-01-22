using System;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;

namespace Booru.Core
{
    public class ConfiguredPair<TSettingsType> where TSettingsType : class, IModuleSettings
    {
        public Type Type { get; private set; }
        public TSettingsType Settings { get; private set; }
        public ConfiguredPair(Type type = null, TSettingsType settings = null)
        {
            Type = type;
            Settings = settings;
        }

        public ConfiguredPair(XmlReader reader)
        {
            string typeName = reader.Name;
            Type = Core.TypesContainer.FirstOrDefault(t => t.FullName == typeName);
            if (Type == null)
                throw new Exception("Parser type not found.");
            else
            {
                typeName = reader.GetAttribute("SettingsType");
                var type = Core.FindTypeByName(typeName);
                if (type != null)
                {
                    reader.Read();
                    XmlSerializer serializer = new XmlSerializer(type);
                    Settings = (TSettingsType)serializer.Deserialize(reader);
                }
                else if (Core.IsSettingsRequiered(Type))
                    throw new Exception("Parser settings type not found.");
                else
                    Settings = null;
            }
            reader.Read();
        }

        public ConfiguredPair<TSettingsType> Clone()
        {
            return new ConfiguredPair<TSettingsType>(Type, (TSettingsType)Settings?.Clone());
        }

        #region Serialization

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement(Type.FullName);
            if (Settings != null)
            {
                var pType = Settings.GetType();
                writer.WriteAttributeString("SettingsType", pType.FullName);
                XmlSerializer serializer = new XmlSerializer(pType);
                serializer.Serialize(writer, Settings);
            }
            writer.WriteEndElement();
        }
        #endregion
    }
}
