using Booru.Core;
using System;
using System.Xml;
using System.Xml.Serialization;

namespace Booru.Base
{
    public class ServerTemplate : BindableBase, IXmlSerializable
	{
		protected ConfiguredPair<IPageGeneratorSettings> _PageGenerator;
		protected ConfiguredPair<IParserSettings> _Parser;
		protected string _PreviewMask;
		protected string _FileMask;

		public Uri Server { get; private set; }
		public int ServerID { get; private set; }

		public ConfiguredPair<IPageGeneratorSettings> PageGenerator
		{
			get { return _PageGenerator; }
			set
			{
				_PageGenerator = value;
				NotifyPropertyChanged(nameof(PageGenerator));
			}
		}
		public ConfiguredPair<IParserSettings> Parser
		{
			get { return _Parser; }
			set
			{
				_Parser = value;
				NotifyPropertyChanged(nameof(Parser));
			}
		}
		public string PreviewMask
		{
			get { return _PreviewMask; }
			set
			{
				_PreviewMask = value;
				NotifyPropertyChanged(nameof(PreviewMask));
			}
		}
		public string FileMask
		{
			get { return _FileMask; }
			set
			{
				_FileMask = value;
				NotifyPropertyChanged(nameof(FileMask));
			}
		}

		public override string ToString()
		{
			return Server.AbsoluteUri;
		}

		public ServerTemplate(Uri Server)
		{
			ServerID = Core.Core.getServerID(Server);
			this.Server = Server;
		}

		protected ServerTemplate() { }

		public static ServerTemplate fromXml(XmlReader Reader)
		{
			var tmp = new ServerTemplate();
			((IXmlSerializable)tmp).ReadXml(Reader);
			return tmp;
		}

		#region Serialization

		System.Xml.Schema.XmlSchema IXmlSerializable.GetSchema()
		{
			return null;
		}

		void IXmlSerializable.ReadXml(XmlReader reader)
		{
			string rootName = reader.Name;
			if (rootName != nameof(ServerTemplate)) return;
            while (reader.Read())
				switch (reader.NodeType)
				{
					case XmlNodeType.Element:
						switch (reader.Name)
						{
							case "Server":
								Server = new Uri(reader.GetAttribute("Value"));
								ServerID = Core.Core.getServerID(Server);
								break;
							//case "Host": Host = reader.GetAttribute("Value"); break;
							case "PageGenerator":
								reader.Read();
								_PageGenerator = new ConfiguredPair<IPageGeneratorSettings>(reader);
								reader.Read();
								break;
							case "Parser":
								reader.Read();
								_Parser = new ConfiguredPair<IParserSettings>(reader);
								reader.Read();
								break;
							case "PreviewMask": _PreviewMask = reader.GetAttribute("Value"); break;
							case "FileMask": _FileMask = reader.GetAttribute("Value"); break;
						}
						break;
					case XmlNodeType.EndElement:
						if (reader.Name == rootName)
						{
							reader.Read();
							return;
						}
						break;
				}
		}

		void IXmlSerializable.WriteXml(XmlWriter writer)
		{
			writer.WriteStartElement("Server");
			writer.WriteAttributeString("Value", Server.AbsoluteUri);
			writer.WriteEndElement();

			writer.WriteStartElement("PreviewMask");
			writer.WriteAttributeString("Value", PreviewMask);
			writer.WriteEndElement();

			writer.WriteStartElement("FileMask");
			writer.WriteAttributeString("Value", FileMask);
			writer.WriteEndElement();

			writer.WriteStartElement("PageGenerator");
			_PageGenerator.WriteXml(writer);
			writer.WriteEndElement();

			writer.WriteStartElement("Parser");
			_Parser.WriteXml(writer);
			writer.WriteEndElement();
		}
		#endregion
	}

}
