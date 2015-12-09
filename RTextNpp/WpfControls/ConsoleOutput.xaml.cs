using RTextNppPlugin.RText;
using RTextNppPlugin.Scintilla;
using RTextNppPlugin.Utilities;
using RTextNppPlugin.Utilities.Settings;
using RTextNppPlugin.ViewModels;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RTextNppPlugin.WpfControls
{
    /// <summary>
    /// Interaction logic for ConsoleOutput.xaml
    /// </summary>
    public partial class ConsoleOutput : UserControl
    {
        internal ConsoleOutput(ConnectorManager cmanager, INpp nppHelper, IStyleConfigurationObserver styleObserver, ISettings settings, Plugin plugin)
        {
            InitializeComponent();            
            var dataContext        = new ConsoleViewModel(cmanager, nppHelper, styleObserver, Dispatcher, settings, plugin);
            dataContext.Dispatcher = Dispatcher;
            DataContext            = dataContext;
            _nppHelper             = nppHelper;
        }

        private void ErrorListPreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ErrorList.IsSelected = true;
            e.Handled = false;
            ErrorList.Focus();
        }

        private void ConsolePreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Console.IsSelected = true;
            e.Handled = false;
            Console.Focus();
        }

        private void OnWorkspaceGridSizeChanged(object sender, System.Windows.SizeChangedEventArgs e)
        {
            double aWSpaceHeight = WorkspaceGrid.ActualHeight;
            double aWSpaceWidth = WorkspaceGrid.ActualWidth;
            int aNewRadius = (int)(Math.Floor(Math.Min(aWSpaceHeight, aWSpaceWidth)) * 0.80);
            aNewRadius >>= 1;
            OuterProgressBar.Radius = aNewRadius;
            InnerProgressBar.Radius = aNewRadius;
            var diameter     = aNewRadius * 2;
            var radiusSquare = (diameter * diameter);
            var halfSquare   = radiusSquare / 2;
            PercentageLabelContainer.Width = PercentageLabelContainer.Height = Math.Sqrt(halfSquare);
        }

        #region [Custom Data Members]
        INpp _nppHelper = null;
        #endregion

        private void OnErrorListPreviewKeyDown(object sender, KeyEventArgs e)
        {
            switch(e.Key)
            {
                case Key.Down:
                case Key.Up:
                case Key.Left:
                case Key.Right:
                case Key.PageDown:
                case Key.PageUp:
                case Key.Home:
                case Key.End:
                    e.Handled = true;
                    return;
                default:
                    return;
            }
        }

        private void OnErrorNodeExpanded(object sender, RoutedEventArgs e)
        {
            //focus datagrid child so that the first click gets routed to the datagrid
            Expander aExpander = sender as Expander;
            DataGrid aErrorList = aExpander.Content as DataGrid;
            aErrorList.Focus();
        }

        private void OnErrorListPreviewMouseDown(object sender, MouseButtonEventArgs e)
        {
            DataGrid aErrorListDatagrid = sender as DataGrid;
            var aCurrentItem = aErrorListDatagrid.Items[aErrorListDatagrid.SelectedIndex] as ErrorItemViewModel;
            if (aCurrentItem != null)
            {
                _nppHelper.JumpToLine(aCurrentItem.FilePath, aCurrentItem.Line);
            }
        }
    }
}