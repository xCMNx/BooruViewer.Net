using System;
using System.Windows.Data;

namespace Booru.Base.PageGenerators
{
    public class MaskConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            if (values.Length < 2)
                return null;
            return PageEnumerator.ApplyMask((values[0] as Uri)?.Host, string.Empty, values[1] as string, values[2]);
        }
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException("Cannot convert back");
        }
    }
}
