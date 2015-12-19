using RTextNppPlugin.Scintilla;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;

namespace RTextNppPlugin.Utilities
{
    public interface IWin32MessageReceptor
    {
        bool OnMessageReceived(uint msg, UIntPtr wParam, IntPtr lParam);
    }
    public class VisualUtilities
    {
        static readonly INativeHelpers _nativeHelpers = new NativeHelpers();

        public enum WM_ACTIVATE_WPARAM : int
        {
            WA_INACTIVE,
            WA_ACTIVE,
            WA_CLICKACTIVE
        }
        
        public enum HookType : int
        {
            WH_JOURNALRECORD   = 0,
            WH_JOURNALPLAYBACK = 1,
            WH_KEYBOARD        = 2,
            WH_GETMESSAGE      = 3,
            WH_CALLWNDPROC     = 4,
            WH_CBT             = 5,
            WH_SYSMSGFILTER    = 6,
            WH_MOUSE           = 7,
            WH_HARDWARE        = 8,
            WH_DEBUG           = 9,
            WH_SHELL           = 10,
            WH_FOREGROUNDIDLE  = 11,
            WH_CALLWNDPROCRET  = 12,
            WH_KEYBOARD_LL     = 13,
            WH_MOUSE_LL        = 14
        }
        
        public enum MouseMessages
        {
            WM_LBUTTONDOWN     = 0x0201,
            WM_LBUTTONUP       = 0x0202,
            WM_LBUTTONDBLCLK   = 0x0203,
            WM_MOUSEMOVE       = 0x0200,
            WM_MOUSEWHEEL      = 0x020A,
            WM_RBUTTONDOWN     = 0x0204,
            WM_RBUTTONUP       = 0x0205,
            WM_NCMOUSEMOVE     = 0x00A0,
            WM_NCLBUTTONDOWN   = 0x00A1,
            WM_NCLBUTTONDBLCLK = 0x00A3,
            WM_NCRBUTTONDOWN   = 0x00A4,
            WM_NCRBUTTONDBLCLK = 0x00A6,
            WM_NCMBUTTONDOWN   = 0x00A7,
            WM_NCMBUTTONDBLCLK = 0x00A9,
            WM_NCXBUTTONDOWN   = 0x00AB,
            WM_NCXBUTTONDBLCLK = 0x00AD
        }
        
        public enum WindowsMessage
        {
            WM_NULL                        = 0x0000,
            WM_CREATE                      = 0x0001,
            WM_DESTROY                     = 0x0002,
            WM_MOVE                        = 0x0003,
            WM_SIZE                        = 0x0005,
            WM_ACTIVATE                    = 0x0006,
            WM_SETFOCUS                    = 0x0007,
            WM_KILLFOCUS                   = 0x0008,
            WM_ENABLE                      = 0x000A,
            WM_SETREDRAW                   = 0x000B,
            WM_SETTEXT                     = 0x000C,
            WM_GETTEXT                     = 0x000D,
            WM_GETTEXTLENGTH               = 0x000E,
            WM_PAINT                       = 0x000F,
            WM_CLOSE                       = 0x0010,
            WM_QUERYENDSESSION             = 0x0011,
            WM_QUERYOPEN                   = 0x0013,
            WM_ENDSESSION                  = 0x0016,
            WM_QUIT                        = 0x0012,
            WM_ERASEBKGND                  = 0x0014,
            WM_SYSCOLORCHANGE              = 0x0015,
            WM_SHOWWINDOW                  = 0x0018,
            WM_WININICHANGE                = 0x001A,
            WM_SETTINGCHANGE               = WM_WININICHANGE,
            WM_DEVMODECHANGE               = 0x001B,
            WM_ACTIVATEAPP                 = 0x001C,
            WM_FONTCHANGE                  = 0x001D,
            WM_TIMECHANGE                  = 0x001E,
            WM_CANCELMODE                  = 0x001F,
            WM_SETCURSOR                   = 0x0020,
            WM_MOUSEACTIVATE               = 0x0021,
            WM_CHILDACTIVATE               = 0x0022,
            WM_QUEUESYNC                   = 0x0023,
            WM_GETMINMAXINFO               = 0x0024,
            WM_PAINTICON                   = 0x0026,
            WM_ICONERASEBKGND              = 0x0027,
            WM_NEXTDLGCTL                  = 0x0028,
            WM_SPOOLERSTATUS               = 0x002A,
            WM_DRAWITEM                    = 0x002B,
            WM_MEASUREITEM                 = 0x002C,
            WM_DELETEITEM                  = 0x002D,
            WM_VKEYTOITEM                  = 0x002E,
            WM_CHARTOITEM                  = 0x002F,
            WM_SETFONT                     = 0x0030,
            WM_GETFONT                     = 0x0031,
            WM_SETHOTKEY                   = 0x0032,
            WM_GETHOTKEY                   = 0x0033,
            WM_QUERYDRAGICON               = 0x0037,
            WM_COMPAREITEM                 = 0x0039,
            WM_GETOBJECT                   = 0x003D,
            WM_COMPACTING                  = 0x0041,
            WM_COMMNOTIFY                  = 0x0044,
            WM_WINDOWPOSCHANGING           = 0x0046,
            WM_WINDOWPOSCHANGED            = 0x0047,
            WM_POWER                       = 0x0048,
            WM_COPYDATA                    = 0x004A,
            WM_CANCELJOURNAL               = 0x004B,
            WM_NOTIFY                      = 0x004E,
            WM_INPUTLANGCHANGEREQUEST      = 0x0050,
            WM_INPUTLANGCHANGE             = 0x0051,
            WM_TCARD                       = 0x0052,
            WM_HELP                        = 0x0053,
            WM_USERCHANGED                 = 0x0054,
            WM_NOTIFYFORMAT                = 0x0055,
            WM_CONTEXTMENU                 = 0x007B,
            WM_STYLECHANGING               = 0x007C,
            WM_STYLECHANGED                = 0x007D,
            WM_DISPLAYCHANGE               = 0x007E,
            WM_GETICON                     = 0x007F,
            WM_SETICON                     = 0x0080,
            WM_NCCREATE                    = 0x0081,
            WM_NCDESTROY                   = 0x0082,
            WM_NCCALCSIZE                  = 0x0083,
            WM_NCHITTEST                   = 0x0084,
            WM_NCPAINT                     = 0x0085,
            WM_NCACTIVATE                  = 0x0086,
            WM_GETDLGCODE                  = 0x0087,
            WM_SYNCPAINT                   = 0x0088,
            WM_NCMOUSEMOVE                 = 0x00A0,
            WM_NCLBUTTONDOWN               = 0x00A1,
            WM_NCLBUTTONUP                 = 0x00A2,
            WM_NCLBUTTONDBLCLK             = 0x00A3,
            WM_NCRBUTTONDOWN               = 0x00A4,
            WM_NCRBUTTONUP                 = 0x00A5,
            WM_NCRBUTTONDBLCLK             = 0x00A6,
            WM_NCMBUTTONDOWN               = 0x00A7,
            WM_NCMBUTTONUP                 = 0x00A8,
            WM_NCMBUTTONDBLCLK             = 0x00A9,
            WM_NCXBUTTONDOWN               = 0x00AB,
            WM_NCXBUTTONUP                 = 0x00AC,
            WM_NCXBUTTONDBLCLK             = 0x00AD,
            WM_INPUT_DEVICE_CHANGE         = 0x00FE,
            WM_INPUT                       = 0x00FF,
            WM_KEYFIRST                    = 0x0100,
            WM_KEYDOWN                     = 0x0100,
            WM_KEYUP                       = 0x0101,
            WM_CHAR                        = 0x0102,
            WM_DEADCHAR                    = 0x0103,
            WM_SYSKEYDOWN                  = 0x0104,
            WM_SYSKEYUP                    = 0x0105,
            WM_SYSCHAR                     = 0x0106,
            WM_SYSDEADCHAR                 = 0x0107,
            WM_UNICHAR                     = 0x0109,
            WM_KEYLAST                     = 0x0109,
            WM_IME_STARTCOMPOSITION        = 0x010D,
            WM_IME_ENDCOMPOSITION          = 0x010E,
            WM_IME_COMPOSITION             = 0x010F,
            WM_IME_KEYLAST                 = 0x010F,
            WM_INITDIALOG                  = 0x0110,
            WM_COMMAND                     = 0x0111,
            WM_SYSCOMMAND                  = 0x0112,
            WM_TIMER                       = 0x0113,
            WM_HSCROLL                     = 0x0114,
            WM_VSCROLL                     = 0x0115,
            WM_INITMENU                    = 0x0116,
            WM_INITMENUPOPUP               = 0x0117,
            WM_MENUSELECT                  = 0x011F,
            WM_MENUCHAR                    = 0x0120,
            WM_ENTERIDLE                   = 0x0121,
            WM_MENURBUTTONUP               = 0x0122,
            WM_MENUDRAG                    = 0x0123,
            WM_MENUGETOBJECT               = 0x0124,
            WM_UNINITMENUPOPUP             = 0x0125,
            WM_MENUCOMMAND                 = 0x0126,
            WM_CHANGEUISTATE               = 0x0127,
            WM_UPDATEUISTATE               = 0x0128,
            WM_QUERYUISTATE                = 0x0129,
            WM_CTLCOLORMSGBOX              = 0x0132,
            WM_CTLCOLOREDIT                = 0x0133,
            WM_CTLCOLORLISTBOX             = 0x0134,
            WM_CTLCOLORBTN                 = 0x0135,
            WM_CTLCOLORDLG                 = 0x0136,
            WM_CTLCOLORSCROLLBAR           = 0x0137,
            WM_CTLCOLORSTATIC              = 0x0138,
            MN_GETHMENU                    = 0x01E1,
            WM_MOUSEFIRST                  = 0x0200,
            WM_MOUSEMOVE                   = 0x0200,
            WM_LBUTTONDOWN                 = 0x0201,
            WM_LBUTTONUP                   = 0x0202,
            WM_LBUTTONDBLCLK               = 0x0203,
            WM_RBUTTONDOWN                 = 0x0204,
            WM_RBUTTONUP                   = 0x0205,
            WM_RBUTTONDBLCLK               = 0x0206,
            WM_MBUTTONDOWN                 = 0x0207,
            WM_MBUTTONUP                   = 0x0208,
            WM_MBUTTONDBLCLK               = 0x0209,
            WM_MOUSEWHEEL                  = 0x020A,
            WM_XBUTTONDOWN                 = 0x020B,
            WM_XBUTTONUP                   = 0x020C,
            WM_XBUTTONDBLCLK               = 0x020D,
            WM_MOUSEHWHEEL                 = 0x020E,
            WM_PARENTNOTIFY                = 0x0210,
            WM_ENTERMENULOOP               = 0x0211,
            WM_EXITMENULOOP                = 0x0212,
            WM_NEXTMENU                    = 0x0213,
            WM_SIZING                      = 0x0214,
            WM_CAPTURECHANGED              = 0x0215,
            WM_MOVING                      = 0x0216,
            WM_POWERBROADCAST              = 0x0218,
            WM_DEVICECHANGE                = 0x0219,
            WM_MDICREATE                   = 0x0220,
            WM_MDIDESTROY                  = 0x0221,
            WM_MDIACTIVATE                 = 0x0222,
            WM_MDIRESTORE                  = 0x0223,
            WM_MDINEXT                     = 0x0224,
            WM_MDIMAXIMIZE                 = 0x0225,
            WM_MDITILE                     = 0x0226,
            WM_MDICASCADE                  = 0x0227,
            WM_MDIICONARRANGE              = 0x0228,
            WM_MDIGETACTIVE                = 0x0229,
            WM_MDISETMENU                  = 0x0230,
            WM_ENTERSIZEMOVE               = 0x0231,
            WM_EXITSIZEMOVE                = 0x0232,
            WM_DROPFILES                   = 0x0233,
            WM_MDIREFRESHMENU              = 0x0234,
            WM_IME_SETCONTEXT              = 0x0281,
            WM_IME_NOTIFY                  = 0x0282,
            WM_IME_CONTROL                 = 0x0283,
            WM_IME_COMPOSITIONFULL         = 0x0284,
            WM_IME_SELECT                  = 0x0285,
            WM_IME_CHAR                    = 0x0286,
            WM_IME_REQUEST                 = 0x0288,
            WM_IME_KEYDOWN                 = 0x0290,
            WM_IME_KEYUP                   = 0x0291,
            WM_MOUSEHOVER                  = 0x02A1,
            WM_MOUSELEAVE                  = 0x02A3,
            WM_NCMOUSEHOVER                = 0x02A0,
            WM_NCMOUSELEAVE                = 0x02A2,
            WM_WTSSESSION_CHANGE           = 0x02B1,
            WM_TABLET_FIRST                = 0x02c0,
            WM_TABLET_LAST                 = 0x02df,
            WM_CUT                         = 0x0300,
            WM_COPY                        = 0x0301,
            WM_PASTE                       = 0x0302,
            WM_CLEAR                       = 0x0303,
            WM_UNDO                        = 0x0304,
            WM_RENDERFORMAT                = 0x0305,
            WM_RENDERALLFORMATS            = 0x0306,
            WM_DESTROYCLIPBOARD            = 0x0307,
            WM_DRAWCLIPBOARD               = 0x0308,
            WM_PAINTCLIPBOARD              = 0x0309,
            WM_VSCROLLCLIPBOARD            = 0x030A,
            WM_SIZECLIPBOARD               = 0x030B,
            WM_ASKCBFORMATNAME             = 0x030C,
            WM_CHANGECBCHAIN               = 0x030D,
            WM_HSCROLLCLIPBOARD            = 0x030E,
            WM_QUERYNEWPALETTE             = 0x030F,
            WM_PALETTEISCHANGING           = 0x0310,
            WM_PALETTECHANGED              = 0x0311,
            WM_HOTKEY                      = 0x0312,
            WM_PRINT                       = 0x0317,
            WM_PRINTCLIENT                 = 0x0318,
            WM_APPCOMMAND                  = 0x0319,
            WM_THEMECHANGED                = 0x031A,
            WM_CLIPBOARDUPDATE             = 0x031D,
            WM_DWMCOMPOSITIONCHANGED       = 0x031E,
            WM_DWMNCRENDERINGCHANGED       = 0x031F,
            WM_DWMCOLORIZATIONCOLORCHANGED = 0x0320,
            WM_DWMWINDOWMAXIMIZEDCHANGE    = 0x0321,
            WM_GETTITLEBARINFOEX           = 0x033F,
            WM_HANDHELDFIRST               = 0x0358,
            WM_HANDHELDLAST                = 0x035F,
            WM_AFXFIRST                    = 0x0360,
            WM_AFXLAST                     = 0x037F,
            WM_PENWINFIRST                 = 0x0380,
            WM_PENWINLAST                  = 0x038F,
            WM_APP                         = 0x8000,
            WM_USER                        = 0x0400,
            WM_REFLECT                     = WM_USER + 0x1C00,
        }
        
        internal static void SetOwnerFromNppPlugin(System.Windows.Window window)
        {
            WindowInteropHelper helper = new WindowInteropHelper(window);
            helper.Owner               = Plugin.Instance.NppData._nppHandle;
        }
        
        internal static HwndSource HwndSourceFromIntPtr(IntPtr handle)
        {
            return HwndSource.FromHwnd(handle);
        }
        
        internal static IntPtr HwndFromWpfWindow(System.Windows.Window window)
        {
            var wih = new WindowInteropHelper(window);
            return wih.Handle;
        }
        
        /**
         *
         * \brief   Finds the parent of this item.
         *
         *
         * \tparam  T   Generic type parameter.
         * \param   child   The child.
         *
         * \return  The found visual parent&lt; t&gt;
         */
        internal static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;
            T parent = parentObject as T;
            if (parent != null)
                return parent;
            else
                return FindVisualParent<T>(parentObject);
        }
        
        internal static T GetVisualChild<T>(Visual parent) where T : Visual
        {
            T child = default(T);
            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++)
            {
                Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);
                child = v as T;
                if (child == null)
                {
                    child = GetVisualChild<T>(v);
                }
                if (child != null)
                {
                    break;
                }
            }
            return child;
        }
        
        /**
         * \brief   Query if the mouse pointer is inside an UI element.
         *
         * \param   element The element.
         *
         * \return  true if mouse is inside framework element, false if not.
         */
        internal static bool IsMouseInsideFrameworkElement(System.Windows.FrameworkElement element)
        {
            double dWidth = -1;
            double dHeight = -1;
            if (element != null)
            {
                dWidth = element.ActualWidth;
                dHeight = element.ActualHeight;
            }
            System.Windows.Point aPoint = System.Windows.Input.Mouse.GetPosition(element);
            double xStart = 0.0;
            double xEnd = xStart + dWidth;
            double yStart = 0.0;
            double yEnd = yStart + dHeight;
            if (aPoint.X < xStart || aPoint.X >= xEnd || aPoint.Y < yStart || aPoint.Y >= yEnd)
            {
                return false;
            }
            return true;
        }
        
        /**
         * \brief   Scroll list.
         *
         * \param   key         The key which is pressed.
         * \param   view        The view.
         * \param   container   The container.
         * \param   offset      (Optional) the offset with which to scroll.
         *
         * \return  An int indicating the new container position after the scroll has been done.
         */
        internal static int ScrollList(System.Windows.Forms.Keys key, ICollectionView view, ItemsControl container, int offset = 1)
        {
            int aNewPosition = 0;
            switch (key)
            {
                case System.Windows.Forms.Keys.PageDown:
                case System.Windows.Forms.Keys.Down:
                    if (view.CurrentPosition + offset < container.Items.Count)
                    {
                        aNewPosition = view.CurrentPosition + offset;
                        view.MoveCurrentToPosition(aNewPosition);
                    }
                    else
                    {
                        aNewPosition = container.Items.Count - 1;
                        view.MoveCurrentToLast();
                    }
                    break;
                case System.Windows.Forms.Keys.PageUp:
                case System.Windows.Forms.Keys.Up:
                    if (view.CurrentPosition - offset >= 0)
                    {
                        aNewPosition = view.CurrentPosition - offset;
                        view.MoveCurrentToPosition(view.CurrentPosition - offset);
                    }
                    else
                    {
                        aNewPosition = 0;
                        view.MoveCurrentToFirst();
                    }
                    break;
            }
            return aNewPosition;
        }
        
        internal static void RepositionWindow(SizeChangedEventArgs e, Window w, ref bool isOnTop, INpp nppHelper, double wordY, int offset = 0)
        {
            bool isTopChanged  = false;
            bool isLeftChanged = false;
            double newTop      = w.Top;
            double newLeft     = w.Left;
            if (isOnTop)
            {
                var aHeightDiff = e.PreviousSize.Height - e.NewSize.Height;
                if (aHeightDiff > 0)
                {
                    newTop += aHeightDiff + offset;
                    isTopChanged = true;
                }
            }
            else
            {
                if (!((e.NewSize.Height + w.Top) <= _nativeHelpers.GetClientRectFromControl(nppHelper.NppHandle).Bottom))
                {
                    newTop = wordY - (e.NewSize.Height - offset);
                    if (newTop >= _nativeHelpers.GetClientRectFromControl(nppHelper.NppHandle).Top)
                    {
                        isOnTop = true;
                        isTopChanged = true;
                    }
                }
            }
            //position list in such a way that it doesn't get split into two monitors
            var rectFromPoint = _nativeHelpers.GetClientRectFromPoint(new System.Drawing.Point((int)w.Left, (int)w.Top));
            //if the width of the auto completion window overlaps the right edge of the screen, then move the window at the left until no overlap is present
            if (rectFromPoint.Right < w.Left + e.NewSize.Width)
            {
                double dif = (w.Left + e.NewSize.Width) - rectFromPoint.Right;
                newLeft -= dif;
                isLeftChanged = true;
            }
            if (isTopChanged && !isLeftChanged)
            {
                w.Top = newTop;
            }
            else if (isLeftChanged && !isTopChanged)
            {
                w.Left = newLeft;
            }
            else if (isLeftChanged && isTopChanged)
            {
                w.Top = newTop;
                w.Left = newLeft;
            }
        }
        
        /**
         * Find the scrollbar out of a wpf control, e.g. DataGrid if it exists.
         *
         * \param   dep The dependency object.
         *
         * \return  The scrollbar.
         */
        internal static ScrollViewer GetScrollViewer(DependencyObject dep)
        {
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(dep); i++)
            {
                var child = VisualTreeHelper.GetChild(dep, i);
                if (child != null && child is ScrollViewer)
                    return child as ScrollViewer;
                else
                {
                    ScrollViewer sub = GetScrollViewer(child);
                    if (sub != null)
                        return sub;
                }
            }
            return null;
        }

        #region [DPI Scaling]
        /// <summary>
        ///        Creates a memory device context (DC) compatible with the specified device.
        /// </summary>
        /// <param name="hdc">A handle to an existing DC. If this handle is NULL,
        ///        the function creates a memory DC compatible with the application's current screen.</param>
        /// <returns>
        ///        If the function succeeds, the return value is the handle to a memory DC.
        ///        If the function fails, the return value is <see cref="System.IntPtr.Zero"/>.
        /// </returns>
        [DllImport("gdi32.dll", EntryPoint = "CreateCompatibleDC", SetLastError = true)]
        private static extern IntPtr CreateCompatibleDC([In] IntPtr hdc);

        [DllImport("gdi32.dll")]
        private static extern int GetDeviceCaps(IntPtr hdc, int nIndex);

        [DllImport("user32.dll")]
        private static extern IntPtr MonitorFromWindow(IntPtr hwnd, uint dwFlags);

        [DllImport("shcore.dll")]
        private static extern int GetDpiForMonitor(IntPtr monitor, int dpiType, out uint newDpiX, out uint newDpiY);

        private enum DeviceCap : int
        {
            /// <summary>
            /// Device driver version
            /// </summary>
            DRIVERVERSION = 0,
            /// <summary>
            /// Device classification
            /// </summary>
            TECHNOLOGY = 2,
            /// <summary>
            /// Horizontal size in millimeters
            /// </summary>
            HORZSIZE = 4,
            /// <summary>
            /// Vertical size in millimeters
            /// </summary>
            VERTSIZE = 6,
            /// <summary>
            /// Horizontal width in pixels
            /// </summary>
            HORZRES = 8,
            /// <summary>
            /// Vertical height in pixels
            /// </summary>
            VERTRES = 10,
            /// <summary>
            /// Number of bits per pixel
            /// </summary>
            BITSPIXEL = 12,
            /// <summary>
            /// Number of planes
            /// </summary>
            PLANES = 14,
            /// <summary>
            /// Number of brushes the device has
            /// </summary>
            NUMBRUSHES = 16,
            /// <summary>
            /// Number of pens the device has
            /// </summary>
            NUMPENS = 18,
            /// <summary>
            /// Number of markers the device has
            /// </summary>
            NUMMARKERS = 20,
            /// <summary>
            /// Number of fonts the device has
            /// </summary>
            NUMFONTS = 22,
            /// <summary>
            /// Number of colors the device supports
            /// </summary>
            NUMCOLORS = 24,
            /// <summary>
            /// Size required for device descriptor
            /// </summary>
            PDEVICESIZE = 26,
            /// <summary>
            /// Curve capabilities
            /// </summary>
            CURVECAPS = 28,
            /// <summary>
            /// Line capabilities
            /// </summary>
            LINECAPS = 30,
            /// <summary>
            /// Polygonal capabilities
            /// </summary>
            POLYGONALCAPS = 32,
            /// <summary>
            /// Text capabilities
            /// </summary>
            TEXTCAPS = 34,
            /// <summary>
            /// Clipping capabilities
            /// </summary>
            CLIPCAPS = 36,
            /// <summary>
            /// Bitblt capabilities
            /// </summary>
            RASTERCAPS = 38,
            /// <summary>
            /// Length of the X leg
            /// </summary>
            ASPECTX = 40,
            /// <summary>
            /// Length of the Y leg
            /// </summary>
            ASPECTY = 42,
            /// <summary>
            /// Length of the hypotenuse
            /// </summary>
            ASPECTXY = 44,
            /// <summary>
            /// Shading and Blending caps
            /// </summary>
            SHADEBLENDCAPS = 45,

            /// <summary>
            /// Logical pixels inch in X
            /// </summary>
            LOGPIXELSX = 88,
            /// <summary>
            /// Logical pixels inch in Y
            /// </summary>
            LOGPIXELSY = 90,

            /// <summary>
            /// Number of entries in physical palette
            /// </summary>
            SIZEPALETTE = 104,
            /// <summary>
            /// Number of reserved entries in palette
            /// </summary>
            NUMRESERVED = 106,
            /// <summary>
            /// Actual color resolution
            /// </summary>
            COLORRES = 108,

            // Printing related DeviceCaps. These replace the appropriate Escapes
            /// <summary>
            /// Physical Width in device units
            /// </summary>
            PHYSICALWIDTH = 110,
            /// <summary>
            /// Physical Height in device units
            /// </summary>
            PHYSICALHEIGHT = 111,
            /// <summary>
            /// Physical Printable Area x margin
            /// </summary>
            PHYSICALOFFSETX = 112,
            /// <summary>
            /// Physical Printable Area y margin
            /// </summary>
            PHYSICALOFFSETY = 113,
            /// <summary>
            /// Scaling factor x
            /// </summary>
            SCALINGFACTORX = 114,
            /// <summary>
            /// Scaling factor y
            /// </summary>
            SCALINGFACTORY = 115,

            /// <summary>
            /// Current vertical refresh rate of the display device (for displays only) in Hz
            /// </summary>
            VREFRESH = 116,
            /// <summary>
            /// Vertical height of entire desktop in pixels
            /// </summary>
            DESKTOPVERTRES = 117,
            /// <summary>
            /// Horizontal width of entire desktop in pixels
            /// </summary>
            DESKTOPHORZRES = 118,
            /// <summary>
            /// Preferred blt alignment
            /// </summary>
            BLTALIGNMENT = 119
        }

        private enum MonitorFromWindowProps : int
        {
            MONITOR_DEFAULTTONULL    = 0,
            MONITOR_DEFAULTTOPRIMARY = 1,
            MONITOR_DEFAULTTONEAREST = 2
        }

        public static double GetDpiScalingFactor()
        {
            double dpiScale = double.NaN;
            if ((System.Environment.OSVersion.Version.Major == 6 && System.Environment.OSVersion.Version.Minor > 1 ||
                System.Environment.OSVersion.Version.Major > 6))
            {
                Logging.Logger.Instance.Append("Current DPI is : {0}", GetDpiForWindowsGreatedThan7(Plugin.Instance.NppData._nppHandle));
                dpiScale = GetDpiForWindowsGreatedThan7(Plugin.Instance.NppData._nppHandle);
            }
            else
            {
                Logging.Logger.Instance.Append("Current DPI is : {0}", GetDpiForWindowsSmallerOrEqualThan7());
                dpiScale = GetDpiForWindowsSmallerOrEqualThan7();
            }
            return dpiScale;
        }

        private static double GetDpiForWindowsGreatedThan7(IntPtr hwnd)
        {
            var monitor = MonitorFromWindow(hwnd, (int)MonitorFromWindowProps.MONITOR_DEFAULTTONEAREST);
            uint newDpiX;
            uint newDpiY;
            if (0 != (GetDpiForMonitor(monitor, 0, out newDpiX, out newDpiY)))
            {
                newDpiX = 96;
                newDpiY = 96;
            }
            return (double)(newDpiX) / 96.0;
        }

        private static double GetDpiForWindowsSmallerOrEqualThan7()
        {
            var hdcMeasure = CreateCompatibleDC(IntPtr.Zero);
            var newDpiX = GetDeviceCaps(hdcMeasure, (int)DeviceCap.LOGPIXELSX) / 96.0;
            return newDpiX;
        }
        #endregion
    }
}