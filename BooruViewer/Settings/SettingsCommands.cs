using System.Windows.Input;

namespace BooruViewer.Settings
{
	public static class SettingsCommands
	{
		private static RoutedUICommand _Apply;
		public static RoutedUICommand Apply { get { return _Apply; } }

		private static RoutedUICommand _Reset;
		public static RoutedUICommand Reset { get { return _Reset; } }

		private static RoutedUICommand _OpenMenu;
		public static RoutedUICommand OpenMenu { get { return _OpenMenu; } }

		private static RoutedUICommand _Close;
		public static RoutedUICommand Close { get { return _Close; } }

		static SettingsCommands()
		{
			//InputGestureCollection inputs = new InputGestureCollection();
			//inputs.Add(new KeyGesture(Key.R, ModifierKeys.Control, "Ctrl + R"));
			_Apply = new RoutedUICommand("Apply", "Apply", typeof(SettingsCommands));
			_Reset = new RoutedUICommand("Reset", "Reset", typeof(SettingsCommands));
			_OpenMenu = new RoutedUICommand("Open menu", "OpenMenu", typeof(SettingsCommands));
			_Close = new RoutedUICommand("Close", "Close", typeof(SettingsCommands));
		}
	}
}
