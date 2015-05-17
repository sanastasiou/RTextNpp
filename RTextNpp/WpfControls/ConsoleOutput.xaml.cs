using System;
using System.Windows.Controls;
using RTextNppPlugin.ViewModels;

namespace RTextNppPlugin.WpfControls
{
    /// <summary>
    /// Interaction logic for ConsoleOutput.xaml
    /// </summary>
    public partial class ConsoleOutput : UserControl
    {
        public ConsoleOutput()
        {
            InitializeComponent();
            ((ConsoleViewModel)DataContext).Dispatcher = Dispatcher;
        }

        private void ErrorListPreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ErrorList.IsSelected = true;
            ErrorList.Focus();
        }

        private void ConsolePreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            Console.IsSelected = true;
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
    }
}
