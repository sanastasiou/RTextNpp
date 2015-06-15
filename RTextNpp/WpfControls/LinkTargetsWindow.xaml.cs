using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using CSScriptIntellisense;
using RTextNppPlugin.DllExport;
using RTextNppPlugin.Logging;
using RTextNppPlugin.RText;
using RTextNppPlugin.RText.Parsing;
using RTextNppPlugin.RText.Protocol;
using RTextNppPlugin.RText.StateEngine;
using RTextNppPlugin.Utilities;
using RTextNppPlugin.Utilities.Settings;
using RTextNppPlugin.ViewModels;

namespace RTextNppPlugin.WpfControls
{
    /**
     * @class   LinkTargetsWindow
     *
     * @brief   Interaction logic for the find references window.
     *
     */
    internal partial class LinkTargetsWindow : Window, ILinkTargetsWindow
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
        private INpp _nppHelper                                    = null;                 //!< Handles communication with scintilla or npp.
        private IWin32 _win32Helper                                = null;                 //!< Handles low level API calls.
        private ISettings _settings                                = null;                 //!< Reads or write plugin settings.
        private ReferenceRequestObserver _referenceRequestObserver = null;                 //!< Handles reference requests triggers.       
        private IEnumerable<string> _cachedContext                 = null;                 //!< Holds the last context used for reference lookup request.
        private DelayedEventHandler _referenceRequestDispatcher    = null;                 //!< Debounces link reference requests and dispatches the reuqests to the backend.
        private LinkTargetsResponse _cachedReferenceLinks          = null;                 //!< Holds a cache of reference links from a previous backend request.
        private ConnectorManager _cManager                         = null;                 //!< Instance of ConnectorManager instance.
        private Connector _connector                               = null;                 //!< Connector which is relevant to the actual focused file.
        private KeyInterceptor _keyMonitor                         = new KeyInterceptor(); //!< Monitors key presses.
        private bool _isOnTop                                      = false;                //!< Indicates if the link reference window is on top of a token or not.
        private const int YPOSITION_OFFSET                         = 2;                    //!< Window needs to be placed with a Y offset, because otherwise the cursor might indicate a token between the gab of the current token and the window.
        #endregion

        #region [Interface]

        #region ILinkTargetsWindow Members

        public bool IsMouseInsidedWindow()
        {
            return (IsVisible && VisualUtilities.IsMouseInsideFrameworkElement(Content as FrameworkElement));
        }


        public void IssueReferenceLinkRequestCommand(Tokenizer.TokenTag aTokenUnderCursor)
        {
            if (String.IsNullOrWhiteSpace(aTokenUnderCursor.Context) || !_referenceRequestObserver.IsKeyboardShortCutActive)
            {
                _referenceRequestDispatcher.Cancel();
                Hide();
            }
            else
            {
                if (_referenceRequestDispatcher.IsRunning)
                {
                    _referenceRequestDispatcher.TriggerHandler(new ActionWrapper<Tokenizer.TokenTag>(TryHighlightItemUnderMouse, aTokenUnderCursor));
                }
                else
                {
                    TryHighlightItemUnderMouse(aTokenUnderCursor);
                    _referenceRequestDispatcher.TriggerHandler(null);
                }
            }
        }

        #endregion

        internal LinkTargetsWindow(INpp nppHelper, IWin32 win32Helper, ISettings settingsHelper, ConnectorManager cmanager)
        {
            InitializeComponent();
            _nppHelper                  = nppHelper;
            _win32Helper                = win32Helper;
            _settings                   = settingsHelper;
            _referenceRequestObserver   = new ReferenceRequestObserver(_nppHelper, _settings, _win32Helper, this);
            _referenceRequestDispatcher = new DelayedEventHandler(new ActionWrapper<Tokenizer.TokenTag>(TryHighlightItemUnderMouse, default(Tokenizer.TokenTag)), 500);
            _cManager                   = cmanager;
            _keyMonitor.KeyDown         += OnKeyMonitorKeyDown;
            _keyMonitor.KeysToIntercept.Add((int)System.Windows.Forms.Keys.Down);
            _keyMonitor.KeysToIntercept.Add((int)System.Windows.Forms.Keys.Up);
            _keyMonitor.KeysToIntercept.Add((int)System.Windows.Forms.Keys.PageUp);
            _keyMonitor.KeysToIntercept.Add((int)System.Windows.Forms.Keys.PageDown);
        }
       
        internal void CancelPendingRequest()
        {
            _referenceRequestDispatcher.Cancel();
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
            if(!isActive)
            {                
                _referenceRequestDispatcher.Cancel();
                Hide();
            }
        }
        
        /**
         *
         * @brief   Sets the zoom level so that the reference link window also scales according to the user settings.
         *
         * @param   level   The zoom level.
         */
        internal void OnZoomLevelChanged(double level)
        {
            if (IsVisible)
            {
                //in case the form is visible - move it to the new place...
                var aCaretPoint = Npp.Instance.GetCaretScreenLocationForForm();
                if (_referenceRequestObserver.UnderlinedToken.Context != null)
                {
                    aCaretPoint = Npp.Instance.GetCaretScreenLocationRelativeToPosition(_referenceRequestObserver.UnderlinedToken.BufferPosition);
                }
                Left = aCaretPoint.X;
                Top  = aCaretPoint.Y - YPOSITION_OFFSET;
            }
            Dispatcher.BeginInvoke(new Action<double>(GetModel().OnZoomLevelChanged), level);
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
         * \brief   Resizes open auto completion list when the container's size change.
         */
        private void OnContainerSizeChanged(object sender, SizeChangedEventArgs e)
        {
            VisualUtilities.RepositionWindow(e, this, ref _isOnTop, _nppHelper, _nppHelper.GetCaretScreenLocationForFormAboveWord(_referenceRequestObserver.UnderlinedToken.BufferPosition).Y, YPOSITION_OFFSET);
        }

        private void OnKeyMonitorKeyDown(System.Windows.Forms.Keys key, int repeatCount, ref bool handled)
        {
            var collectionView = CollectionViewSource.GetDefaultView(GetModel().Targets);
            int aNewPosition = 0;
            switch (key)
            {
                case System.Windows.Forms.Keys.Up:
                case System.Windows.Forms.Keys.Down:
                    handled = true;
                    aNewPosition = VisualUtilities.ScrollList(key, collectionView, LinkTargetDatagrid);
                    break;
                case System.Windows.Forms.Keys.PageDown:
                case System.Windows.Forms.Keys.PageUp:
                    aNewPosition = VisualUtilities.ScrollList(key, collectionView, LinkTargetDatagrid, 25);
                    handled = true;
                    break;
                default:
                    return;
            }

            LinkTargetDatagrid.ScrollIntoView(collectionView.CurrentItem);
        }

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

        private async Task SendLinkReferenceRequestAsync(Tokenizer.TokenTag aTokenUnderCursor)
        {
            if (aTokenUnderCursor.CanTokenHaveReference() && FileUtilities.IsRTextFile(_settings, _nppHelper))
            {
                Task<Tuple<bool, ContextExtractor>> contextEqualityTask = new Task<Tuple<bool, ContextExtractor>>(new Func<Tuple<bool, ContextExtractor>>(() =>
                {
                    string aContextBlock = _nppHelper.GetTextBetween(0, Npp.Instance.GetLineEnd(aTokenUnderCursor.Line));
                    ContextExtractor aExtractor = new ContextExtractor(aContextBlock, Npp.Instance.GetLengthToEndOfLine(_nppHelper.GetColumn(_nppHelper.GetPositionFromMouseLocation()), aTokenUnderCursor.Line));
                    bool aAreContextEquals = false;

                    //get all tokens before the trigger token - if all previous tokens and all context lines match do not request new auto completion options
                    if (!_referenceRequestObserver.UnderlinedToken.Equals(default(Tokenizer.TokenTag)) && _cachedContext != null)
                    {
                        if (_cachedContext.Count() == 1 && aExtractor.ContextList.Count() == 1)
                        {
                            aAreContextEquals = aTokenUnderCursor.Equals(_referenceRequestObserver.UnderlinedToken);
                        }
                        else
                        {
                            //if context is identical and tokens are also identical do not trigger auto completion request
                            aAreContextEquals = (_cachedContext.Take(_cachedContext.Count() - 1).SequenceEqual(aExtractor.ContextList.Take(aExtractor.ContextList.Count() - 1)) &&
                                                 aTokenUnderCursor.Equals(_referenceRequestObserver.UnderlinedToken));
                        }
                    }
                    return new Tuple<bool, ContextExtractor>(aAreContextEquals, aExtractor);
                }));

                contextEqualityTask.Start();
                await contextEqualityTask;

                Trace.WriteLine(String.Format("Are contexts equal : {0}", contextEqualityTask.Result.Item1));

                //store cache
                _cachedContext                            = contextEqualityTask.Result.Item2.ContextList;
                _referenceRequestObserver.UnderlinedToken = aTokenUnderCursor;

                AutoCompleteAndReferenceRequest aRequest = new AutoCompleteAndReferenceRequest
                {
                    column        = contextEqualityTask.Result.Item2.ContextColumn,
                    command       = Constants.Commands.LINK_TARGETS,
                    context       = _cachedContext,
                    invocation_id = -1
                };            

                if(aRequest.context.Count() == 0)
                {
                    //prevent backend from crashing due to a bug
                    return;
                }

                //determine window position
                var aCaretPoint = _nppHelper.GetCaretScreenLocationRelativeToPosition(aTokenUnderCursor.BufferPosition);
                Left            = aCaretPoint.X;
                Top             = aCaretPoint.Y - YPOSITION_OFFSET;

                if (!contextEqualityTask.Result.Item1)
                {
                    _cachedReferenceLinks = await RequestReferenceLinksAsync(aRequest);                    
                }
                Show();
            }
            else
            {                
                Hide();
            }
        }                

        new public void Hide()
        {
            _referenceRequestDispatcher.Cancel();
            base.Hide();
        }

        new public void Show()
        {
            //update view model with new references
            if (_cachedReferenceLinks != null && _cachedReferenceLinks.targets.Count > 0)
            {
                GetModel().UpdateLinkTargets(_cachedReferenceLinks.targets);
                Utilities.VisualUtilities.SetOwnerFromNppPlugin(this);
                //token needs to be underlined as a hotspot only if the current count is 1
                if (_cachedReferenceLinks.targets.Count == 1)
                {
                    _referenceRequestObserver.UnderlineToken();
                }
                base.Show();
            }
            else if (!String.IsNullOrEmpty(GetModel().ErrorMsg))
            {
                base.Show();
            }
            else if (_cachedReferenceLinks == null || _cachedReferenceLinks.targets.Count == 0)
            {
                Hide();
            }
        }

        private void ForceRedraw()
        {
            if (!GetModel().IsEmpty())
            {                
                var collectionView = CollectionViewSource.GetDefaultView(GetModel().Targets);
                collectionView.MoveCurrentToFirst();
                LinkTargetDatagrid.ScrollIntoView(collectionView.CurrentItem);
            }
        }

        private ReferenceLinkViewModel GetModel()
        {
            return ((ReferenceLinkViewModel)DataContext);
        }

        private async Task<LinkTargetsResponse> RequestReferenceLinksAsync(AutoCompleteAndReferenceRequest request)
        {
            _connector = _cManager.Connector;
            if (_connector != null)
            {
                switch (_connector.CurrentState.State)
                {
                    case ConnectorStates.Disconnected:
                        GetModel().CreateWarning(Properties.Resources.ERR_BACKEND_CONNECTING, Properties.Resources.ERR_BACKEND_CONNECTING_DESC);
                        await _connector.ExecuteAsync<AutoCompleteAndReferenceRequest>(request, Constants.SYNCHRONOUS_COMMANDS_TIMEOUT, Command.Execute);
                        break;
                    case ConnectorStates.Busy:
                    case ConnectorStates.Loading:
                    case ConnectorStates.Connecting:
                        GetModel().CreateWarning(Properties.Resources.ERR_BACKEND_BUSY, Properties.Resources.ERR_BACKEND_BUSY_DESC);                        
                        break;
                    case ConnectorStates.Idle:
                        var aResponse = await _connector.ExecuteAsync<AutoCompleteAndReferenceRequest>(request, Constants.SYNCHRONOUS_COMMANDS_TIMEOUT, Command.Execute) as LinkTargetsResponse;
                        if (aResponse == null)
                        {
                            if (_connector.IsCommandCancelled)
                            {
                                return null;
                            }
                            else
                            {
                                GetModel().CreateWarning(Properties.Resources.ERR_REF_LINK_NULL_RESPONSE, Properties.Resources.ERR_REF_LINK_NULL_RESPONSE_DESC);
                            }
                        }
                        else
                        {
                            GetModel().Clear();
                            GetModel().RemoveWarning();
                            return aResponse;
                        }
                        break;
                    default:
                        Logger.Instance.Append(Logger.MessageType.FatalError, _connector.Workspace, "Undefined connector state reached. Please notify support.");
                        break;
                }
            }
            else
            {
                GetModel().CreateWarning(Properties.Resources.CONNECTOR_INSTANCE_NULL, Properties.Resources.CONNECTOR_INSTANCE_NULL_DESC);
            }
            return null;
        }

        private void TryHighlightItemUnderMouse(Tokenizer.TokenTag aTokenUnderCursor)
        {
            Dispatcher.Invoke((MethodInvoker)(async () =>
            {
                await SendLinkReferenceRequestAsync(aTokenUnderCursor);
            }));          
        }

        /**
         * \brief   Raises the dependency property changed event when visibility is changed.
         *
         * \param   sender  Source of the event.
         * \param   e       Event information to send to registered event handlers.              
         */
        private void OnReferenceLinksVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (Visibility == System.Windows.Visibility.Visible)
            {
                _keyMonitor.Install();
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
                _isOnTop                         = false;
                _keyMonitor.Uninstall();
                _referenceRequestObserver.HideUnderlinedToken();
            }
        }

        private void OnWindowLocationChanged(object sender, EventArgs e)
        {
            ForceRedraw();
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

        private void OnLinkTargetsWindowMouseLeave(object sender, System.Windows.Input.MouseEventArgs e)
        {
            //only hide the window if the mouse has also left the actual underlined token
            var aTokenUnderCursor = Tokenizer.FindTokenUnderCursor(_nppHelper);
            if (!aTokenUnderCursor.Equals(_referenceRequestObserver.UnderlinedToken))
            {
                Hide();
                IssueReferenceLinkRequestCommand(aTokenUnderCursor);
                System.Diagnostics.Trace.WriteLine("IssueReferenceLinksCommand - OnLinkTargetsWindowMouseLeave");
            }
        }        
    }
}
