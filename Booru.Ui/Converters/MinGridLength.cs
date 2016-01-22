using System;
using System.Windows;
using System.Windows.Data;

namespace Booru.Ui.Converters
{
	public class MinGridLength : IMultiValueConverter
	{
		public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
		{
			var min = (Double)values[0];
			foreach (Double gl in values)
				if (min < gl)
					min = gl;
			return new GridLength(min);
		}
		public object[] ConvertBack(object value, Type[] targetTypes,
			   object parameter, System.Globalization.CultureInfo culture)
		{
			throw new NotSupportedException("Cannot convert back");
		}
	}
}
