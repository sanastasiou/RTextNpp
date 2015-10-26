﻿using RTextNppPlugin.Logging;
using RTextNppPlugin.RText;
using RTextNppPlugin.Utilities;
using RTextNppPlugin.ViewModels;
using System;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Linq;
using RTextNppPlugin.Utilities.Settings;
using System.Windows.Input;
using System.Windows.Media;
namespace RTextNppPlugin.WpfControls
{
    /// <summary>
    /// Interaction logic for ConsoleOutput.xaml
    /// </summary>
    public partial class ConsoleOutput : UserControl
    {
        internal ConsoleOutput(ConnectorManager cmanager, INpp nppHelper, IStyleConfigurationObserver styleObserver)
        {
            InitializeComponent();
            var dataContext        = new ConsoleViewModel(cmanager, nppHelper, styleObserver);
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

        private void OnErrorListMouseLeftDown(object sender, MouseButtonEventArgs e)
        {
            DependencyObject dep = (DependencyObject)e.OriginalSource;
            // iteratively traverse the visual tree
            while ((dep != null) && !(dep is DataGridCell))
            {
                dep = VisualTreeHelper.GetParent(dep);
            }

            if (dep != null)
            {
                DataGridCell cell = dep as DataGridCell;
                // navigate further up the tree
                while ((dep != null) && !(dep is DataGridRow))
                {
                    dep = VisualTreeHelper.GetParent(dep);
                }
                DataGridRow row = dep as DataGridRow;
                int aSelectedIndex = FindRowIndex(row);
                ErrorItemViewModel aDataContext = DataContext as ErrorItemViewModel;
                //need the filename as well
            }
        }

        private int FindRowIndex(DataGridRow row)
        {
            DataGrid dataGrid = ItemsControl.ItemsControlFromItemContainer(row) as DataGrid;
            int index = dataGrid.ItemContainerGenerator.IndexFromContainer(row);
            return index;
        }

        private void OnDescriptionClicked(object sender, RoutedEventArgs e)
        {
            TextBlock aRow = sender as TextBlock;
            ErrorItemViewModel aRowDataContext = aRow.DataContext as ErrorItemViewModel;
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
        #region [Custom Data Members]
        INpp _nppHelper = null;
        #endregion
    }
}