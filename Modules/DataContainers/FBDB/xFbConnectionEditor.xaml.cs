using System.ComponentModel;
using System.Windows.Controls;
using Booru.Core;

namespace Booru.Base.DataContainers
{
	/// <summary>
	/// Логика взаимодействия для XFbConnectionEditor.xaml
	/// </summary>
	[SettingsType(typeof(FbDatabaseSettings)), EditorControlType(ControlType.Xaml)]
    public partial class xFbConnectionEditor : UserControl, IModuleSettingsEditor
	{
		public IModuleSettings Settings { get { return (DataContext as IModuleSettings).Clone(); } }
		public xFbConnectionEditor(IModuleSettings CurrentSettings)
		{
			InitializeComponent();
			original = CurrentSettings;
            DataContext = current = CurrentSettings?.Clone() ?? new FbDatabaseSettings();
			(current as FbDatabaseSettings).PropertyChanged += XFbConnectionEditor_PropertyChanged;
        }

		private void XFbConnectionEditor_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			NotifyPropertyChanged(e.PropertyName);
        }

		IModuleSettings original;
		IModuleSettings current;
		public bool Changed => current.CompareTo(original) != 0;

		public event PropertyChangedEventHandler PropertyChanged;

		public void NotifyPropertyChanged(string propertyName = "")
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}
	}
}
