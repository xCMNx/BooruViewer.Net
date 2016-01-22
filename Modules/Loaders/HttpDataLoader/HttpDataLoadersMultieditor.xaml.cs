using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using Booru.Core;

namespace Booru.Base.Loader
{
    /// <summary>
    /// Логика взаимодействия для LoadersMultieditor.xaml
    /// </summary>
    [SettingsType(typeof(HttpDataLoaderSettings)), EditorControlType(ControlType.Xaml)]
    public partial class HttpDataLoadersMultieditor : TextBox, IModuleMultivalueSettingsEditor
    {
        public IList<IModuleSettings> GetSettings()
        {
            IEnumerable<string> lines = Text.Trim().Split(new[] { '\r', '\n' }, System.StringSplitOptions.RemoveEmptyEntries);
            lines = lines.Distinct();
            var lst = new List<IModuleSettings>();
            foreach (var line in lines)
                if (line.Equals("localhost", System.StringComparison.InvariantCultureIgnoreCase))
                    lst.Add(new HttpDataLoaderSettings());
                else
                    lst.Add(new HttpDataLoaderSettings() { Server = line });
            return lst;
        }

        public IList<IModuleSettings> Settings { get { return GetSettings(); } }

        public HttpDataLoadersMultieditor(params IModuleSettings[] Settings)
        {
            InitializeComponent();
            Text = original = string.Join("\r\n", Settings.Select(s => ((HttpDataLoaderSettings)s).Server).ToArray());
            TextChanged += LoadersMultieditor_TextChanged;
        }

        private void LoadersMultieditor_TextChanged(object sender, TextChangedEventArgs e)
        {
            NotifyPropertyChanged("");
        }

        string original;
        public bool Changed => original != Text;

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
