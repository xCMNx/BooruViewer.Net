using System;
using System.Xml;
using System.Xml.Schema;
using Booru.Core;

namespace Booru.Base.Loader
{
    [ModuleType(typeof(HttpDataLoader))]
    public class HttpDataLoaderSettings : BindableBase, ILoaderSettings
    {
        protected String _Server;
        public String Server
        {
            get { return _Server; }
            set
            {
                _Server = value;
                NotifyPropertyChanged(nameof(Server));
            }
        }

        public bool isConfigured => true;

        public int CompareTo(object obj)
        {
            if (obj as HttpDataLoaderSettings == null) return 1;
            return string.Compare(Server, ((HttpDataLoaderSettings)obj).Server, true);
        }

        public virtual IModuleSettings Clone()
        {
            return EditableModuleHelper.Clone(this);
            //throw new NotImplementedException($"Method Clone() of IEditableModuleSettings in class {GetType().FullName} isn't implemented.");
        }

        public void Reset()
        {
            _Server = string.Empty;
            NotifyPropertyChanged(nameof(Server));
        }

        public virtual XmlSchema GetSchema()
        {
            return null;
        }

        public void WriteXml(XmlWriter writer)
        {
            writer.WriteStartElement("Server");
            writer.WriteString(Server);
            writer.WriteEndElement();
        }

        public void ReadXml(XmlReader reader)
        {
            while (reader.Read())
                switch (reader.NodeType)
                {
                    case XmlNodeType.Element:
                        {
                            if (reader.Name != "Server")
                                EditableModuleHelper.SkipElement(reader);
                            else
                            {
                                reader.Read();
                                _Server = reader.Value;
                            }
                        }
                        break;
                    case XmlNodeType.EndElement: return;
                }
        }
    }
}
