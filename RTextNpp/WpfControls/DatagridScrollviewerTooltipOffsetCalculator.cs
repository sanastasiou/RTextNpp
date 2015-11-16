using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using RTextNppPlugin.Utilities;
namespace RTextNppPlugin.WpfControls
{
    internal class DatagridScrollviewerTooltipOffsetCalculator
    {
        #region [Data Members]
        ToolTip _previouslyOpenedToolTip                   = null;
        DelayedEventHandler<object> _delayedToolTipHandler = null;
        Dispatcher _dispatcher                             = null;
        IWindowPosition _winPosition                       = null;
        DataGrid _datagrid                                 = null;
        readonly double MAX_TOOLTIP_LENGTH;
        #endregion
        #region [Interface]
        internal DatagridScrollviewerTooltipOffsetCalculator(Dispatcher dispatcher, IWindowPosition winPosition, double maxLength, DataGrid datagrid)
        {
            _dispatcher            = dispatcher;
            _delayedToolTipHandler = new DelayedEventHandler<object>(new ActionWrapper<object, System.Windows.Controls.ToolTip>(OnToolTipDelayedHandlerExpired, null), 1000, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            _winPosition           = winPosition;
            MAX_TOOLTIP_LENGTH     = maxLength;
            _datagrid              = datagrid;
        }
        internal void CancelTooltipRequest(ToolTip tp)
        {
            _delayedToolTipHandler.Cancel();
            HidePreviouslyOpenedTooltip(tp);
        }
        internal void HidePreviouslyOpenedTooltip(ToolTip tp)
        {
            if (_previouslyOpenedToolTip != null)
            {
                _previouslyOpenedToolTip.IsOpen = false;
            }
            _previouslyOpenedToolTip = tp;
        }
        internal void ShowTooltip(ToolTip tp, UIElement placementTarget)
        {
            tp.PlacementTarget = placementTarget;
            tp.Placement       = System.Windows.Controls.Primitives.PlacementMode.Right;
            HidePreviouslyOpenedTooltip(tp);
            _delayedToolTipHandler.TriggerHandler(new ActionWrapper<object, System.Windows.Controls.ToolTip>(OnToolTipDelayedHandlerExpired, tp));
        }
        internal double CalculateTooltipOffset()
        {
            double aCalculatedOffset = 0.0;
            var aScrollViewer = VisualUtilities.GetScrollViewer(_datagrid);
            if (_winPosition.IsEdgeOfScreenReached(MAX_TOOLTIP_LENGTH))
            {
                if (aScrollViewer.ComputedHorizontalScrollBarVisibility == System.Windows.Visibility.Visible)
                {
                    return aCalculatedOffset + aScrollViewer.HorizontalOffset;
                }
                return aCalculatedOffset;
            }
            var aColumnsSum = _datagrid.Columns.Sum(x => x.ActualWidth);
            if (aScrollViewer.ComputedHorizontalScrollBarVisibility == System.Windows.Visibility.Visible)
            {
                //offset here is the amount of pixels that the scrollbar can move to the right
                double aCurrentOffset = aScrollViewer.HorizontalOffset;
                double aExtendedWidth = aScrollViewer.ExtentWidth;
                double aActualWidth   = aScrollViewer.ActualWidth;
                double aMaxOffset     = aExtendedWidth - aActualWidth;
                aCalculatedOffset -= (aMaxOffset - aCurrentOffset + ((aScrollViewer.ComputedVerticalScrollBarVisibility == System.Windows.Visibility.Visible) ? System.Windows.SystemParameters.ScrollWidth : 0.0));
                aColumnsSum -= aMaxOffset;
            }
            if (aScrollViewer.ComputedVerticalScrollBarVisibility == System.Windows.Visibility.Visible)
            {
                aCalculatedOffset += System.Windows.SystemParameters.ScrollWidth;
            }
            //both scroll bars not visible - column width has to be equal to scrollviewer actual width
            //wpf bug - this is not the case , compensate to fix tooltip location
            if (aScrollViewer.ActualWidth > aColumnsSum)
            {
                if (aScrollViewer.ComputedHorizontalScrollBarVisibility != System.Windows.Visibility.Visible)
                {
                    aCalculatedOffset = aScrollViewer.ActualWidth - aColumnsSum;
                }
                else
                {
                    double aCurrentOffset = aScrollViewer.HorizontalOffset;
                    double aExtendedWidth = aScrollViewer.ExtentWidth;
                    double aActualWidth   = aScrollViewer.ActualWidth;
                    double aMaxOffset     = aExtendedWidth - aActualWidth;
                    aCalculatedOffset     = aScrollViewer.ActualWidth - aColumnsSum - (aMaxOffset - aCurrentOffset);
                }
            }
            return aCalculatedOffset;
        }
        #endregion
        #region [Helpers]
        private object OnToolTipDelayedHandlerExpired(ToolTip tp)
        {
            _dispatcher.Invoke(new Action<ToolTip>(ShowDelayedToolTip), tp);
            return null;
        }
        private void ShowDelayedToolTip(ToolTip tp)
        {
            tp.HorizontalOffset = CalculateTooltipOffset();
            tp.IsOpen           = true;
        }
        #endregion
    }
}