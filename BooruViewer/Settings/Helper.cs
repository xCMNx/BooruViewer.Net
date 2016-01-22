using System;
using Booru.Core;

namespace BooruViewer.Settings
{
	public static class Helper
	{
		public static IModuleSettingsEditor SettingsEditor(this Type Type, IModuleSettings Settings)
		{
			var settings_type = Core.FindSettingsTypeFor(Type);
			var editor_type = Core.EditorTypeFor(settings_type, ControlType.Xaml);
			if (editor_type != null)
				return (IModuleSettingsEditor)Activator.CreateInstance(editor_type, new []{ Settings});
			return null;
		}

		public static IModuleSettingsEditor SettingsEditor(this Type Type, String SettingsName = "*")
		{
			var settings_type = Core.FindSettingsTypeFor(Type);
			var sList = Core.SettingsFor(settings_type, SettingsName);
			return SettingsEditor(settings_type, sList.Count > 0 ? sList[0] : null);
		}
	}
}
