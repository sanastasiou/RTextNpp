using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Media;
using RTextNppPlugin.RText.Protocol;
using RTextNppPlugin.ViewModels;

namespace ESRLabs.RTextEditor.Intellisense
{
    /**
     * @class   LinkTargetsWindow
     *
     * @brief   Interaction logic for the find references window.
     *
     * @author  Stefanos Anastasiou
     * @date    26.01.2013
     */
    public partial class LinkTargetsWindow : Window, IDisposable
    {
        #region [Interface]
        /**
         * @fn  public delegate void HypelinkClicked(object source, HypelinkClickedEventArgs e);
         *
         * @brief   Notifies subscribers that a hyperlinked reference was clicked.
         *
         * @author  Stefanos Anastasiou
         * @date    26.01.2013
         *
         * @param   source  Source for the.
         * @param   e       Hypelink clicked event information.
         */
        public delegate void HypelinkClicked(object source, HypelinkClickedEventArgs e);
        //!< Event queue for all listeners interested in OnHyperlinkedClicked events.
        public event HypelinkClicked OnHyperlinkedClicked;  

        /**
         * @class   HypelinkClickedEventArgs
         *
         * @brief   Additional information for hypelink clicked events.
         *
         * @author  Stefanos Anastasiou
         * @date    31.01.2013
         */
        public class HypelinkClickedEventArgs : EventArgs
        {
            public string File { get; set; }
            public int Line { get; set; }
        }

        /**
         * @fn  public LinkTargetsWindow()
         *
         * @brief   Default constructor.
         *
         * @author  Stefanos Anastasiou
         * @date    31.01.2013
         */
        public LinkTargetsWindow()
        {
            InitializeComponent();
        }

        /**
         * @fn  public bool isMouseInsideWindow(MouseEventArgs e)
         *
         * @brief   Query if mouse is inside reference window.
         *
         * @author  Stefanos Anastasiou
         * @date    03.01.2013
         *
         * @param   e   Mouse event information.
         *
         * @return  true if mouse inside window, false if not.
         */
        public bool isMouseInsideWindow(System.Windows.Input.MouseEventArgs e)
        {
            double dWidth = -1;
            double dHeight = -1;
            FrameworkElement pnlClient = this.Content as FrameworkElement;
            if (pnlClient != null)
            {
                dWidth = pnlClient.ActualWidth;
                dHeight = pnlClient.ActualHeight;
            }
            Point aPoint = e.GetPosition(this);
            double xStart = 0.0;
            double xEnd = xStart + dWidth;
            double yStart = 0.0;
            double yEnd = yStart + dHeight;
            if (aPoint.X < xStart || aPoint.X > xEnd || aPoint.Y < yStart || aPoint.Y > yEnd)
            {
                return false;
            }
            return true;
        }

        /**
         * @fn  public void refreshLinkSource(IList<Protocol.Target> targets)
         *
         * @brief   Refreshes the references link source. "new" cannot be used since the databinding will be lost.
         *
         * @author  Stefanos Anastasiou
         * @date    03.01.2013
         *
         * @param   targets The targets.
         */
        public void refreshLinkSource(IList<Target> targets)
        {
            var aLinkTargetModels = targets.Select(target => new LinkTargetModel(target.display, target.desc, Int32.Parse( target.line), target.file.Replace('/', '\\'))).ToList();
            ((ReferenceLinkViewModel)this.LinkTargetDatagrid.DataContext).Targets.Clear();
            foreach (var model in aLinkTargetModels)
            {
                ((ReferenceLinkViewModel)this.LinkTargetDatagrid.DataContext).Targets.Add(model);
            }
        }

        /**
         * @fn  public void setPopupText(string text)
         *
         * @brief   Sets popup text. This text appears when the backend is busy, not started etc., instead of the actual datagrid.
         *
         * @author  Stefanos Anastasiou
         * @date    22.01.2013
         *
         * @param   text    The text.
         */
        public void setPopupText(string text)
        {
            ((ReferenceLinkViewModel)this.LinkTargetDatagrid.DataContext).BackendBusyString = text;
        }

        /**
         * @fn  public void setZoomLevel(double level)
         *
         * @brief   Sets the zoom level of the current IWpfTextView so that the reference link window also scales according to the user settings.
         *
         * @author  Stefanos Anastasiou
         * @date    30.01.2013
         *
         * @param   level   The zoom level.
         */
        public void setZoomLevel(double level)
        {
            ((ReferenceLinkViewModel)this.LinkTargetDatagrid.DataContext).IWpfZoomLevel = level;
        }

        #endregion

        #region Helpers
        /**
         * \fn  internal void navigateList(System.Windows.Forms.Keys key)
         *
         * \brief   Moves the selected index of the reference list when the user pressed the up and down arrows.
         *
         * \author  Stefanos Anastasiou
         * \date    24.01.2013
         *
         * \param   key The pressed key.
         */
        private void navigateList(System.Windows.Forms.Keys key)
        {
            var aTargets = ((ReferenceLinkViewModel)this.LinkTargetDatagrid.DataContext).Targets;
            if (aTargets.Count > 0)
            {
                var aIndex = this.LinkTargetDatagrid.SelectedIndex;
                switch (key)
                {
                    case Keys.Up:
                        if (aIndex == 0)
                        {
                            this.LinkTargetDatagrid.SelectedIndex = aTargets.Count - 1;
                        }
                        else
                        {
                            this.LinkTargetDatagrid.SelectedIndex = --this.LinkTargetDatagrid.SelectedIndex;
                        }
                        break;
                    case Keys.Down:
                        if (aIndex == (aTargets.Count - 1))
                        {
                            this.LinkTargetDatagrid.SelectedIndex = 0;
                        }
                        else
                        {
                            this.LinkTargetDatagrid.SelectedIndex = ++this.LinkTargetDatagrid.SelectedIndex;
                        }
                        break;
                    default:
                        return;
                }
                this.LinkTargetDatagrid.ScrollIntoView(this.LinkTargetDatagrid.SelectedItem);
            }
        }
        /**
         * @fn  private void FileClicked(object sender, RoutedEventArgs e)
         *
         * @brief   File clicked. Occurs when a hyperlinked filepath is clicked.
         *
         * @author  Stefanos Anastasiou
         * @date    03.01.2013
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
         * @fn  private void OnRowMouseEnter(object sender, RoutedEventArgs e)
         *
         * @brief   Occurs when the user enter the area of a datagrid row.
         *
         * @author  Stefanos Anastasiou
         * @date    26.01.2013
         *
         * @param   sender  Source of the event.
         * @param   e       Event information to send to registered event handlers.
         */
        private void OnRowMouseEnter(object sender, RoutedEventArgs e)
        {
            System.Windows.Input.Mouse.OverrideCursor = System.Windows.Input.Cursors.Hand;
        }

        /**
         * @fn  private void OnRowMouseLeave(object sender, RoutedEventArgs e)
         *
         * @brief   Occurs when the user leave the area of a datagrid row.
         *
         * @author  Stefanos Anastasiou
         * @date    26.01.2013
         *
         * @param   sender  Source of the event.
         * @param   e       Event information to send to registered event handlers.
         */
        private void OnRowMouseLeave(object sender, RoutedEventArgs e)
        {
            System.Windows.Input.Mouse.OverrideCursor = null;
        }
        #endregion

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
                var aTargets = ((ReferenceLinkViewModel)this.LinkTargetDatagrid.DataContext).Targets;
                if (aTargets != null && aTargets.Count > 0)
                {
                    //select first element
                    this.LinkTargetDatagrid.SelectedIndex = -1;
                    this.LinkTargetDatagrid.Focus();
                }
            }
            else
            {
                this.LinkTargetDatagrid.SelectedIndex = -1;
            }
        }

        #region Overriden Window Members
        /**
         * @fn  protected override void OnActivated(EventArgs e)
         *
         * @brief   Raises the activated event.
         *
         * @author  Stefanos Anastasiou
         * @date    26.01.2013
         *
         * @param   e   An <see cref="T:System.EventArgs" /> that contains the event data.
         *
         * ### summary  Raises the <see cref="E:System.Windows.Window.Activated" /> event.
         */
        protected override void OnActivated(EventArgs e)
        {
            
        }

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
            Point pt = System.Windows.Input.Mouse.GetPosition(this.LinkTargetDatagrid);
            System.Windows.Controls.DataGridRow aRow = null;
            //Do the hittest to find the DataGridCell
            VisualTreeHelper.HitTest(this.LinkTargetDatagrid, null, (result) =>
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
                this.FileClicked(((ReferenceLinkViewModel)this.LinkTargetDatagrid.DataContext).Targets[aRow.GetIndex()]);
                e.Handled = true;
            }
        }

        private void navigateList(Key key)
        {
            var aTargets = ((ReferenceLinkViewModel)this.LinkTargetDatagrid.DataContext).Targets;
            if (aTargets.Count > 0)
            {
                var aIndex = this.LinkTargetDatagrid.SelectedIndex;
                switch (key)
                {
                    case Key.Up:
                        if (aIndex == 0)
                        {
                            this.LinkTargetDatagrid.SelectedIndex = aTargets.Count - 1;
                        }
                        else
                        {
                            this.LinkTargetDatagrid.SelectedIndex = --this.LinkTargetDatagrid.SelectedIndex;
                        }
                        break;
                    case Key.Down:
                        if (aIndex == (aTargets.Count - 1))
                        {
                            this.LinkTargetDatagrid.SelectedIndex = 0;
                        }
                        else
                        {
                            this.LinkTargetDatagrid.SelectedIndex = ++this.LinkTargetDatagrid.SelectedIndex;
                        }
                        break;
                    default:
                        return;
                }
                this.LinkTargetDatagrid.ScrollIntoView(this.LinkTargetDatagrid.SelectedItem);
            }
        }

        #endregion

        #region IDisposable Members
        /**
         * @fn  public void Dispose()
         *
         * @brief   Performs application-defined tasks associated with freeing, releasing, or resetting
         *          unmanaged resources.
         *
         * @author  Stefanos Anastasiou
         * @date    26.01.2013
         *
         * ### summary  Performs application-defined tasks associated with freeing, releasing, or
         *              resetting unmanaged resources.
         */
        public void Dispose()
        {
           // UnhookWindowsHookEx(keyboardHookId);
        }
        #endregion
    }
}
