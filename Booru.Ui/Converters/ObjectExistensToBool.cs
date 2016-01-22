using System;
using System.Globalization;
using System.Windows.Data;

namespace Booru.Ui.Converters
{
	public class ObjectExistensToBool : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value == null ? ( parameter== null ? false : bool.Parse((string)parameter)) : true;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}
