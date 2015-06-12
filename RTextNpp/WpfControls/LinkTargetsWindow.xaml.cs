using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using RTextNppPlugin.DllExport;
using RTextNppPlugin.RText;
using RTextNppPlugin.RText.Parsing;
using RTextNppPlugin.RText.Protocol;
using RTextNppPlugin.Utilities;
using RTextNppPlugin.Utilities.Settings;
using RTextNppPlugin.ViewModels;
using System.Diagnostics;

namespace RTextNppPlugin.WpfControls
{
    /**
     * @class   LinkTargetsWindow
     *
     * @brief   Interaction logic for the find references window.
     *
     */
    internal partial class LinkTargetsWindow : Window
    {
        #region [Events]
        /**
         * @fn  internal delegate void HypelinkClicked(object source, HypelinkClickedEventArgs e);
         *
         * @brief   Notifies subscribers that a hyperlinked reference was clicked.
         *
         *
         * @param   source  Source for the.
         * @param   e       Hypelink clicked event information.
         */
        internal delegate void HypelinkClicked(object source, HypelinkClickedEventArgs e);
        //!< Event queue for all listeners interested in OnHyperlinkedClicked events.
        internal event HypelinkClicked OnHyperlinkedClicked;

        /**
         * @class   HypelinkClickedEventArgs
         *
         * @brief   Additional information for hypelink clicked events.
         *
         */
        internal class HypelinkClickedEventArgs : EventArgs
        {
            internal string File { get; set; }
            internal int Line { get; set; }
        }
        #endregion

        #region [Data Members]
        private INpp _nppHelper                                    = null;  //!< Handles communication with scintilla or npp.
        private IWin32 _win32Helper                                = null;  //!< Handles low level API calls.
        private ISettings _settings                                = null;  //!< Reads or write plugin settings.
        private ReferenceRequestObserver _referenceRequestObserver = null;  //!< Handles reference requests triggers.
        #endregion

        #region [Interface]

        internal LinkTargetsWindow(INpp nppHelper, IWin32 win32Helper, ISettings settingsHelper)
        {
            InitializeComponent();
            _nppHelper   = nppHelper;
            _win32Helper = win32Helper;
            _settings    = settingsHelper;
            _referenceRequestObserver = new ReferenceRequestObserver(_nppHelper, _settings, _win32Helper);
            _referenceRequestObserver.MouseMove += OnReferenceRequestObserverMouseMove;
        }
       
        internal void CancelPendingRequest()
        {
            _referenceRequestObserver.CancelPendingRequest();
        }

        /**
         * Asynchronous call when keyboard shortcut for reference link changes.
         *
         * \param   isActive    true if this LinkTargetsWindow is active.
         *
         * \remark  If the keyboard shortcut is active the plugin will try to find the references,
         *          when the cursor is placed over a valid reference token, e.g. Identifier, reference etc.
         */
        internal void IsKeyboardShortCutActive(bool isActive)
        {
            _referenceRequestObserver.IsKeyboardShortCutActive = isActive;
            if(isActive)
            {
                var aTokenUnderCursor = Tokenizer.FindTokenUnderCursor(_nppHelper);
                Dispatcher.Invoke((MethodInvoker)(async () =>
                {
                    await TryHighlightItemUnderMouse(aTokenUnderCursor);
                }));                
            }
            else
            {
                Hide();
            }
        }
        
        /**
         * @brief   Refreshes the references link source. "new" cannot be used since the databinding will be lost.
         *
         * @param   targets The targets.
         */
        internal void refreshLinkSource(IEnumerable<Target> targets)
        {
            var aLinkTargetModels = targets.AsParallel().Select(target => new LinkTargetModel(target.display, target.desc, Int32.Parse(target.line), target.file));
            var viewModel = (ReferenceLinkViewModel)LinkTargetDatagrid.DataContext;
            viewModel.Targets.Clear();
            viewModel.Targets.AddRange(aLinkTargetModels);
        }

        /**
         * @brief   Sets popup text. This text appears when the backend is busy, not started etc., instead of the actual datagrid.
         *
         * @param   text    The text.
         */
        internal void setPopupText(string text)
        {
            ((ReferenceLinkViewModel)LinkTargetDatagrid.DataContext).BackendBusyString = text;
        }

        /**
         *
         * @brief   Sets the zoom level of the current IWpfTextView so that the reference link window also scales according to the user settings.
         *
         * @param   level   The zoom level.
         */
        internal void SetZoomLevel(double level)
        {
            ((ReferenceLinkViewModel)LinkTargetDatagrid.DataContext).ZoomLevel = level;
        }

        #endregion

        #region Helpers
        /**
         * \brief   Moves the selected index of the reference list when the user pressed the up and down arrows.
         *
         * \param   key The pressed key.
         */
        private void navigateList(System.Windows.Forms.Keys key)
        {
            var aTargets = ((ReferenceLinkViewModel)LinkTargetDatagrid.DataContext).Targets;
            if (aTargets.Count > 0)
            {
                var aIndex = LinkTargetDatagrid.SelectedIndex;
                switch (key)
                {
                    case Keys.Up:
                        if (aIndex == 0)
                        {
                            LinkTargetDatagrid.SelectedIndex = aTargets.Count - 1;
                        }
                        else
                        {
                            LinkTargetDatagrid.SelectedIndex = --LinkTargetDatagrid.SelectedIndex;
                        }
                        break;
                    case Keys.Down:
                        if (aIndex == (aTargets.Count - 1))
                        {
                            LinkTargetDatagrid.SelectedIndex = 0;
                        }
                        else
                        {
                            LinkTargetDatagrid.SelectedIndex = ++LinkTargetDatagrid.SelectedIndex;
                        }
                        break;
                    default:
                        return;
                }
                LinkTargetDatagrid.ScrollIntoView(LinkTargetDatagrid.SelectedItem);
            }
        }
        /**
         * @brief   File clicked. Occurs when a hyperlinked filepath is clicked.
         *
         * @param   sender  Source of the event.
         * @param   e       Routed event information.
         */
        private void FileClicked(LinkTargetModel target)
        {
            OnHyperlinkedClicked(this, new HypelinkClickedEventArgs { File = target.FilePath, Line = target.Line });
        }
        #endregion

        #region EventHandlers
        /**
         * @brief   Occurs when the user enter the area of a datagrid row.
         *
         * @param   sender  Source of the event.
         * @param   e       Event information to send to registered event handlers.
         */
        private void OnRowMouseEnter(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = System.Windows.Input.Cursors.Hand;
        }

        /**
         * @brief   Occurs when the user leave the area of a datagrid row.
         *
         * @param   sender  Source of the event.
         * @param   e       Event information to send to registered event handlers.
         */
        private void OnRowMouseLeave(object sender, RoutedEventArgs e)
        {
            Mouse.OverrideCursor = null;
        }

        private async Task TryHighlightItemUnderMouse(Tokenizer.TokenTag aTokenUnderCursor)
        {
            Trace.WriteLine("Trying to find references...");
        }

        private void OnReferenceRequestObserverMouseMove()
        {
            var aTokenUnderCursor = Tokenizer.FindTokenUnderCursor(_nppHelper);
            Dispatcher.Invoke((MethodInvoker)(async () =>
            {
                await TryHighlightItemUnderMouse(aTokenUnderCursor);
            }));
        }

        /**
         * \brief   Raises the dependency property changed event when visibility is changed.
         *
         * \param   sender  Source of the event.
         * \param   e       Event information to send to registered event handlers.
         * \todo    Install, uninstall keyboard hook.                  
         */
        private void OnReferenceLinksVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (Visibility == System.Windows.Visibility.Visible)
            {
                var aTargets = ((ReferenceLinkViewModel)LinkTargetDatagrid.DataContext).Targets;
                if (aTargets != null && aTargets.Count > 0)
                {
                    //select first element
                    LinkTargetDatagrid.SelectedIndex = -1;
                    LinkTargetDatagrid.Focus();
                }
            }
            else
            {
                LinkTargetDatagrid.SelectedIndex = -1;
            }
        }
        #endregion

        #region Overriden Window Members

        /**
         * @fn  protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
         *
         * @brief   Raises the preview left mouse button event.
         *
         * @author  Stefanos Anastasiou
         * @date    26.01.2013
         *
         * @param   e   Event information to send to registered event handlers.
         */
        protected override void OnPreviewMouseLeftButtonDown(MouseButtonEventArgs e)
        {
            base.OnPreviewMouseLeftButtonDown(e);
            Point pt = Mouse.GetPosition(LinkTargetDatagrid);
            System.Windows.Controls.DataGridRow aRow = null;
            //Do the hittest to find the DataGridCell
            VisualTreeHelper.HitTest(LinkTargetDatagrid, null, (result) =>
            {
                // Find the ancestor element form the hittested element
                // e.g., find the DataGridCell if we hittest on the inner TextBlock
                System.Windows.Controls.DataGridRow aCcurrentRow = RTextNppPlugin.Utilities.VisualUtilities.FindVisualParent<System.Windows.Controls.DataGridRow>(result.VisualHit);

                if (aCcurrentRow != null)
                {
                    aRow = aCcurrentRow;
                    return HitTestResultBehavior.Stop;
                }
                else
                    return HitTestResultBehavior.Continue;
            }, new PointHitTestParameters(pt));
            if (aRow != null)
            {
                FileClicked(((ReferenceLinkViewModel)LinkTargetDatagrid.DataContext).Targets[aRow.GetIndex()]);
                e.Handled = true;
            }
        }
        
        #endregion

        #region [Helpers]

        private void navigateList(Key key)
        {
            var aTargets = ((ReferenceLinkViewModel)LinkTargetDatagrid.DataContext).Targets;
            if (aTargets.Count > 0)
            {
                var aIndex = LinkTargetDatagrid.SelectedIndex;
                switch (key)
                {
                    case Key.Up:
                        if (aIndex == 0)
                        {
                            LinkTargetDatagrid.SelectedIndex = aTargets.Count - 1;
                        }
                        else
                        {
                            LinkTargetDatagrid.SelectedIndex = --LinkTargetDatagrid.SelectedIndex;
                        }
                        break;
                    case Key.Down:
                        if (aIndex == (aTargets.Count - 1))
                        {
                            LinkTargetDatagrid.SelectedIndex = 0;
                        }
                        else
                        {
                            LinkTargetDatagrid.SelectedIndex = ++LinkTargetDatagrid.SelectedIndex;
                        }
                        break;
                    default:
                        return;
                }
                LinkTargetDatagrid.ScrollIntoView(LinkTargetDatagrid.SelectedItem);
            }
        }

        #endregion

        private async void OnLinkTargetsWindowMouseLeaveAsync(object sender, System.Windows.Input.MouseEventArgs e)
        {
            //only hide the window if the mouse has also left the actual underlined token
            var aTokenUnderCursor = Tokenizer.FindTokenUnderCursor(_nppHelper);
            if (!aTokenUnderCursor.Equals(_referenceRequestObserver.UnderlinedToken))
            {
                Hide();
                await TryHighlightItemUnderMouse(aTokenUnderCursor);
            }
        }
    }
}
