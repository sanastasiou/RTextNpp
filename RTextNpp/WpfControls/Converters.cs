using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Media;
using RTextNppPlugin.ViewModels;

namespace RTextNppPlugin.WpfControls.Converters
{
    internal class AutoCompletionBorderBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            AutoCompletionViewModel.Completion c = value as AutoCompletionViewModel.Completion;
            if (c != null)
            {
                return Brushes.Transparent; //c.DisplayText.Equals( Brushes.Transparent;
            }
            else
            {
                return Brushes.Transparent;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }

    internal class SelectedCellBackgroundBrushConverter : IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            AutoCompletionViewModel.Completion current = values[0] as AutoCompletionViewModel.Completion;
            AutoCompletionViewModel model              = values[1] as AutoCompletionViewModel;
            if (current != null && model.SelectedCompletion != null)
            {
                if(current.DisplayText.Equals(model.SelectedCompletion.DisplayText))
                {
                    return model.SelectedCompletion.IsFuzzy ? Brushes.Transparent : Brushes.LimeGreen;
                }
                else
                {
                    return Brushes.Transparent;
                }
            }
            else
            {
                return Brushes.Transparent;
            }
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
