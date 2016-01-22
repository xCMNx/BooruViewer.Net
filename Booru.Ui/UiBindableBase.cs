using System;
using System.ComponentModel;

namespace Booru.Ui
{
	public class UiBindableBase : INotifyPropertyChanged
	{
		public event PropertyChangedEventHandler PropertyChanged;

		public void NotifyPropertyChanged(String propertyName = "")
		{
			if (PropertyChanged != null)
			{
				PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
			}
		}

		public void NotifyPropertiesChanged(params String[] propertyNames)
		{
			if (PropertyChanged != null)
			{
				foreach(var prop in propertyNames)
					PropertyChanged(this, new PropertyChangedEventArgs(prop));
			}
		}
	}
}
