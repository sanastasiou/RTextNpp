using System;
using System.Windows.Data;

namespace RTextNppPlugin.WpfControls.Converters
{
    [ValueConversion(typeof(object), typeof(string))]
    internal class GreaterThanValueConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            try
            {
                return ((Int32)value > Int32.Parse(parameter as string));
            }
            catch
            {
                return true;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter,
                        System.Globalization.CultureInfo culture)
        {
            return null;
        }
    }

    internal class PercentageConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double perc = (double)value;
            return perc.ToString() + " %";
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    internal class ProgressLabelSizeConverter : IValueConverter
    {
        #region IValueConverter Members

        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            return 1.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        #endregion
    }

    internal class CommandConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            switch (value as string)
            {
                case Constants.Commands.CONTENT_COMPLETION:
                    return "Calculating auto completion options...";
                case Constants.Commands.CONTEXT_INFO:
                    return "Retrieving context information...";
                case Constants.Commands.FIND_ELEMENTS:
                    return "Searcing for elements...";
                case Constants.Commands.LINK_TARGETS:
                    return "Calculating references...";
                case Constants.Commands.LOAD_MODEL:
                    return "Loading model...";
                case Constants.Commands.STOP:
                    return "Offline.";
                default:
                    return "Ready.";
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
    
}
