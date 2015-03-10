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
    //called once for every new item on list..
    internal class AutoCompletionBorderBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            AutoCompletionViewModel.Completion c = value as AutoCompletionViewModel.Completion;
            if (c != null)
            {
                return c.IsFuzzy ? Brushes.LimeGreen : Brushes.Transparent;
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

    //called when index changes
    internal class SelectedCellBackgroundBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            AutoCompletionViewModel model              = value as AutoCompletionViewModel;
            if (model.SelectedCompletion != null)
            {
                return model.SelectedCompletion.IsFuzzy ? Brushes.Transparent : Brushes.LimeGreen;
            }
            else
            {
                return Brushes.Transparent;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}
