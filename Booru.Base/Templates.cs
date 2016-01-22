using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Xml.Serialization;

namespace Booru.Base
{
    public static class Templates
    {
        static ObservableCollection<ServerTemplate> _Templates = new ObservableCollection<ServerTemplate>();
        public static ObservableCollection<ServerTemplate> List => _Templates;

        public static string TemplatesPath { get; private set; }

        static Templates()
        {
            TemplatesPath = Core.Utils.Helpers.ReadFromConfig("TemplatesPath") ?? System.IO.Path.Combine(Core.Utils.Helpers.ProgramPath, @"Settings\Templates.xml");
            Load();
        }

        public static int TemplateIndex(Uri Server)
        {
            for (int i = 0; i < _Templates.Count; i++)
                if (List[i].Server.Host.Equals(Server.Host))
                    return i;
            return -1;
        }

        public static bool TemplateExists(Uri Server)
        {
            return TemplateIndex(Server) > -1;
        }

        public static bool TemplateExists(string Server)
        {
            return TemplateExists(new Uri(Server));
        }

        public static ServerTemplate Add(Uri Server)
        {
            if (TemplateExists(Server))
                throw new Exception("Template already exists.");
            var s = new ServerTemplate(Server);
            _Templates.Add(s);
            Core.Core.getServerID(Server);
            Save();
            return s;
        }

        public static ServerTemplate Add(ServerTemplate Template)
        {
            if (TemplateExists(Template.Server))
                throw new Exception("Template already exists.");
            _Templates.Add(Template);
            Save();
            return Template;
        }

        public static void Remove(Uri Server)
        {
            var idx = TemplateIndex(Server);
            if (idx > -1)
                _Templates.RemoveAt(idx);
        }

        public static void Remove(ServerTemplate Template)
        {
            Remove(Template.Server);
        }

        public static void Save(string filename)
        {
            XmlSerializer serializer = new XmlSerializer(typeof(ServerTemplate[]));
            using (Stream writer = new FileStream(filename, FileMode.Create))
                serializer.Serialize(writer, _Templates.ToArray());
        }

        public static void Save()
        {
            Save(TemplatesPath);
        }

        public static void Load(string filename)
        {
            if (File.Exists(filename))
            {
                XmlSerializer serializer = new XmlSerializer(typeof(ServerTemplate[]));
                using (Stream reader = new FileStream(filename, FileMode.Open))
                    _Templates = new ObservableCollection<ServerTemplate>((ServerTemplate[])serializer.Deserialize(reader));
                foreach (var t in _Templates)
                    Core.Core.getServerID(t.Server);
            }
        }

        public static void Load()
        {
            Load(TemplatesPath);
        }

        public static ServerTemplate getTemplate(string Server)
        {
            lock (List)
            {
                foreach (var t in List)
                    if (t.Server.Host.Equals(Server, StringComparison.CurrentCultureIgnoreCase))
                        return t;
            }
            return null;
        }
    }
}
