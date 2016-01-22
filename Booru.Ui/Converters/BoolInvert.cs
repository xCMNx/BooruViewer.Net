﻿using System;
using System.Globalization;
using System.Windows.Data;

namespace Booru.Ui.Converters
{
	public class BoolInvert : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return !(bool)value;
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotSupportedException();
		}
	}
}
