using System;
using System.Globalization;
using System.Windows.Data;

namespace Booru.Ui.Converters
{
	public class TypeToString : IValueConverter
	{
		public bool FullName { get; set; } = false;
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			Type t = value as Type;
			return t == null ? null : FullName ? t.FullName : t.Name;
        }

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}
