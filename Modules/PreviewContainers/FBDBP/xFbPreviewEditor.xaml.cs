using System.ComponentModel;
using System.Windows.Controls;
using Booru.Core;

namespace Booru.Base.PreviewConnectors
{
	/// <summary>
	/// Логика взаимодействия для XFbConnectionEditor.xaml
	/// </summary>
	[SettingsType(typeof(FBPreviewsSettings)), EditorControlType(ControlType.Xaml)]
    public partial class xFbPreviewEditor : UserControl, IModuleSettingsEditor
	{
		public IModuleSettings Settings { get { return (DataContext as IModuleSettings).Clone(); } }
		public xFbPreviewEditor(IModuleSettings CurrentSettings)
		{
			InitializeComponent();
			original = CurrentSettings;
            DataContext = current = CurrentSettings?.Clone() ?? new FBPreviewsSettings();
			(current as FBPreviewsSettings).PropertyChanged += xFbPreviewEditor_PropertyChanged;
        }

		private void xFbPreviewEditor_PropertyChanged(object sender, PropertyChangedEventArgs e)
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
