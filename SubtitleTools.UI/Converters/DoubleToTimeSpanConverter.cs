using System;
using System.Globalization;
using System.Windows.Data;

namespace SubtitleTools.UI.Converters
{
    public class DoubleToTimeSpanConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var underlyingType = Nullable.GetUnderlyingType(targetType);

            if (targetType == typeof(TimeSpan) || underlyingType == typeof(TimeSpan))
            {
                if (value == null) return TimeSpan.Zero;

                if (value is string str)
                {
                    var val = Utils.TimeMs(str);
                    return TimeSpan.FromMilliseconds(val);
                }
                else if (value is long lval)
                {
                    return new TimeSpan(lval);
                }
                else if (value is double dval)
                {
                    return TimeSpan.FromMilliseconds(dval);
                }
            }

            return TimeSpan.Zero;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            var underlyingType = Nullable.GetUnderlyingType(targetType);

            if (targetType == typeof(double) || underlyingType == typeof(double))
            {
                if (value == null) return 0d;

                if (value is string str)
                {
                    return Utils.TimeMs(str);
                }
                else if (value is TimeSpan span)
                {
                    return span.TotalMilliseconds;
                }

                return 0d;
            }

            return null;
        }
    }
}
