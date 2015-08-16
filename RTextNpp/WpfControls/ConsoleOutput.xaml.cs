using System;
using System.Windows.Controls;
using RTextNppPlugin.ViewModels;
using RTextNppPlugin.RText;
using System.Windows;
using System.Windows.Documents;

namespace RTextNppPlugin.WpfControls
{
    /// <summary>
    /// Interaction logic for ConsoleOutput.xaml
    /// </summary>
    public partial class ConsoleOutput : UserControl
    {
        internal ConsoleOutput(ConnectorManager cmanager)
        {
            InitializeComponent();
            var dataContext        = new ConsoleViewModel(cmanager);
            dataContext.Dispatcher = Dispatcher;
            DataContext            = dataContext;
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

        private void OnDescriptionClicked(object sender, RoutedEventArgs e)
        {
            Hyperlink link = e.OriginalSource as Hyperlink;
            //RTextEditorPluginPackage.OpenOrFocusDocument(link.NavigateUri.LocalPath);
            ////move cursor to first error - get workspace
            //WorkspaceModel aSelectedWs = WorkspaceSelector.SelectedItem as WorkspaceModel;
            //var aErroneousFile = (from aErrorGroup in ErrorListManager.getInstance[aSelectedWs.WorkspaceName].problems
            //                      where aErrorGroup.file.Replace("/", "\\").Equals(link.NavigateUri.LocalPath)
            //                      select aErrorGroup.problems).First();
            //int aLine = (from lines in aErroneousFile
            //             orderby lines.line ascending
            //             select lines).ToArray()[0].line;
            ////jump to first error in file
            //NavigateToFile(link.NavigateUri.LocalPath, aLine);
        }
    }
}
