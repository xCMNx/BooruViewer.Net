using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reflection;
using System.Xml;
using System.Xml.Serialization;

namespace Booru.Core
{
	public static class EditableModuleHelper
	{

		public static void RemoveModulesByType(this IList<IModule> Modules, Type ModuleType)
		{
			for (int i = Modules.Count - 1; i >= 0; i--)
				if (Modules[i].GetType() == ModuleType)
					Modules.RemoveAt(i);
		}

		public static void RemoveModulesByTypeO(this ObservableCollection<IModule> Modules, Type ModuleType)
		{
			for (int i = Modules.Count - 1; i >= 0; i--)
				if (Modules[i].GetType() == ModuleType)
					Modules.RemoveAt(i);
		}

		public static bool ContainsSettings(this IEnumerable<IModule> Modules, IModuleSettings Settings)
		{
			foreach (IModule mod in Modules)
			{
				if (mod is IEditableModule)
				{
					var emod = mod as IEditableModule;
					if (((emod as IEditableModule).CurrentSettings == null && Settings == null) || (emod.CurrentSettings != null && emod.CurrentSettings.CompareTo(Settings) == 0))
						return true;
				}
			}
			return false;
		}

		public static void AddModuleToList(this IList<IModule> Modules, IModule Module)
		{
			if (Module is IEditableModule)
				if (ContainsSettings(Modules, (Module as IEditableModule).CurrentSettings))
					return;
			Modules.Add(Module);
		}

		public static void CloneProperties(this object dst, object Source)
		{
			var type = dst.GetType();
			var flds = type.GetFields(BindingFlags.Instance | BindingFlags.GetField | BindingFlags.SetField | BindingFlags.NonPublic | BindingFlags.Public);
			foreach (var fld in flds)
				fld.SetValue(dst, fld.GetValue(Source));
		}

		public static IModuleSettings Clone(this IModuleSettings module)
		{
			var _tmp = (IModuleSettings)Activator.CreateInstance(module.GetType());
			_tmp.CloneProperties(module);
			return _tmp;
		}

		public static void WriteProperties(object obj, XmlWriter Writer)
		{
			var type = obj.GetType();
			var props = type.GetProperties(BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.SetProperty | BindingFlags.Public);
			foreach (var prop in props)
				if (prop.CanRead && prop.CanWrite && !Attribute.IsDefined(prop, typeof(XmlIgnoreAttribute), true))
				{
					var val = prop.GetValue(obj);
					if (val != null)
					{
						Writer.WriteStartElement(prop.Name);
						XmlSerializer serializer = new XmlSerializer(val.GetType());
						serializer.Serialize(Writer, val);
						Writer.WriteEndElement();
					}
				}
		}

		public static void SkipElement(XmlReader Reader)
		{
			while (Reader.Read())
				switch (Reader.NodeType)
				{
					case XmlNodeType.Element:
						SkipElement(Reader);
						break;
					case XmlNodeType.EndElement: return;
				}
		}

		public static void ReadProperties(object obj, XmlReader Reader)
		{
			var type = obj.GetType();
			if (type.Name != Reader.Name) return;
			var props = type.GetProperties(BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.SetProperty | BindingFlags.Public);
			while (Reader.Read())
				switch (Reader.NodeType)
				{
					case XmlNodeType.Element:
						{
							var elementName = Reader.Name;
							var prop = props.FirstOrDefault(p => p.Name == elementName);
							if (prop == null || !prop.CanWrite || Attribute.IsDefined(prop, typeof(XmlIgnoreAttribute), true))
							{
								SkipElement(Reader);
								continue;
							}
							XmlSerializer serializer = new XmlSerializer(prop.PropertyType);
							Reader.Read();
							if (Reader.NodeType == XmlNodeType.Element)
								prop.SetValue(obj, serializer.Deserialize(Reader));
							else
								throw new SettingsReadException($"Settings property {prop.Name} read error.");
						}
						break;
					case XmlNodeType.EndElement: return;
				}
		}
	}
}
