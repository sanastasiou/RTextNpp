using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Threading;
using RTextNppPlugin.Utilities;

namespace RTextNppPlugin.WpfControls
{
    internal class ScrollviewerTooltipOffsetCalculator
    {
        #region [Data Members]
        ToolTip _previouslyOpenedToolTip           = null;
        DelayedEventHandler _delayedToolTipHandler = null;
        Dispatcher _dispatcher                     = null;
        ScrollViewer _scrollviewer                 = null;
        IWindowPosition _winPosition               = null;
        readonly double MAX_TOOLTIP_LENGTH; 
        #endregion

        #region [Interface]
        ScrollviewerTooltipOffsetCalculator(Dispatcher dispatcher, ScrollViewer scrollviewer, IWindowPosition winPosition, double maxLength)
        {
            _dispatcher            = dispatcher;
            _scrollviewer          = scrollviewer;
            _delayedToolTipHandler = new DelayedEventHandler(new ActionWrapper<System.Windows.Controls.ToolTip>(OnToolTipDelayedHandlerExpired, null), 1000, System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            _winPosition           = winPosition;
            MAX_TOOLTIP_LENGTH     = maxLength;
        }

        #endregion

        #region [Helpers]
        private void OnToolTipDelayedHandlerExpired(ToolTip tp)
        {
            _dispatcher.Invoke(new Action<ToolTip>(ShowDelayedToolTip), tp);
        }

        private void ShowDelayedToolTip(ToolTip tp)
        {
            tp.HorizontalOffset = CalculateTooltipOffset();
            tp.IsOpen           = true;
        }

        private double CalculateTooltipOffset()
        {
            double aCalculatedOffset = 0.0;

            if (_winPosition.IsEdgeOfScreenReached(MAX_TOOLTIP_LENGTH))
            {
                return aCalculatedOffset;
            }
            if (_scrollviewer.ComputedVerticalScrollBarVisibility == System.Windows.Visibility.Visible)
            {
                aCalculatedOffset += System.Windows.SystemParameters.ScrollWidth;
            }
            //else if (_scrollviewer.ComputedVerticalScrollBarVisibility == System.Windows.Visibility.Collapsed)
            //{
            //    //wpf bug - when a scrollbar gets collapsed due to filtering some leftover remain :s
            //    if (AutoCompletionListBorder.ActualWidth > _scrollviewer.ViewportWidth)
            //    {
            //        aCalculatedOffset += (System.Windows.SystemParameters.ScrollWidth - 11.0);
            //    }
            //}

            if (_scrollviewer.ComputedHorizontalScrollBarVisibility == System.Windows.Visibility.Visible)
            {
                //offset here is the amount of pixels that the scrollbar can move to the right
                double aCurrentOffset = _scrollviewer.HorizontalOffset;
                double aExtendedWidth = _scrollviewer.ExtentWidth;
                double aViewPortWidth = _scrollviewer.ViewportWidth;
                double aMaxOffset = aExtendedWidth - aViewPortWidth;
                aCalculatedOffset -= (aMaxOffset - aCurrentOffset);
            }

            return aCalculatedOffset;
        }
        #endregion
    }
}
