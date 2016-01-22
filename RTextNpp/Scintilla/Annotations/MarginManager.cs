using RTextNppPlugin.DllExport;
using RTextNppPlugin.Properties;
using RTextNppPlugin.Utilities.Settings;
using RTextNppPlugin.ViewModels;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace RTextNppPlugin.Scintilla.Annotations
{
    internal sealed class Pixmap
    {
        private const string XPM_FORMAT =
@"/* XPM */
static char* image[] = {{
""{0} {1} {2} 1"",
{3}
{4}
}};";
        private const string COLOR_FORMAT = "\"{0} c {1}\",\r\n";
        private const string CHARS = ",<.>/?;:'[{]}~!@#$%^&*()_`-+=1234567890qwertyuiopasdfghjklzxcvbnmQWERTYUIOPASDFGHJKLZXCVBNM|";
        private const string TRANSPARENT = "None";
        private const char QUOTE = '"';
        private const char COMMA = ',';
        private const string CRLF = "\r\n";

        private Bitmap bmp;
        private string xpm;
        private object syncRoot = new Object();

        private Pixmap(Bitmap bmp)
        {
            this.bmp = bmp;
        }

        public static Pixmap FromBitmap(Bitmap bmp)
        {
            return new Pixmap(bmp);
        }

        public string GetPixmap()
        {
            if (xpm == null)
                lock (syncRoot)
                    if (xpm == null)
                    {
                        using (bmp)
                            xpm = ConvertBitmap(bmp);
                        bmp = null;
                    }

            return xpm;
        }

        private string ConvertBitmap(Bitmap bmp)
        {
            var colorBuilder = new StringBuilder();
            var mapBuilder = new StringBuilder();
            var colors = new Dictionary<Color, Char>();
            var colIndex = 0;

            for (var y = 0; y < bmp.Height; y++)
            {
                mapBuilder.Append(QUOTE);

                for (var x = 0; x < bmp.Width; x++)
                {
                    var c = bmp.GetPixel(x, y);
                    var ch = default(Char);

                    if (!colors.TryGetValue(c, out ch))
                    {
                        var white = c.R == 255 && c.G == 255 && c.B == 255;

                        if (!white && colIndex > CHARS.Length - 1)
                            throw new Exception(String.Format("Image can have up to {0} colors", CHARS.Length));

                        ch = white ? ' ' : CHARS[colIndex];
                        colors.Add(c, ch);
                        colorBuilder.AppendFormat(COLOR_FORMAT, ch,
                            white ? TRANSPARENT : ColorTranslator.ToHtml(c));
                        colIndex++;
                    }

                    mapBuilder.Append(ch);
                }

                mapBuilder.Append(QUOTE);

                if (y < bmp.Height - 1)
                {
                    mapBuilder.Append(COMMA);
                    mapBuilder.Append(CRLF);
                }
            }

            return String.Format(XPM_FORMAT, bmp.Width, bmp.Height, colors.Count,
                colorBuilder.ToString(), mapBuilder.ToString());
        }

        public static explicit operator String(Pixmap xpm)
        {
            return xpm.GetPixmap();
        }
    }

    class MarginManager : ErrorBase, IError
    {
        #region [Data Members]
        private enum MarkerId : int
        {
            MarkerId_Debug,
            MarkerId_Info,
            MarkerId_Warning,
            MarkerId_Error,
            MarkerId_FatalError
        }

        private const Settings.RTextNppSettings SETTING   = Settings.RTextNppSettings.EnableErrorMarkers;
        private const int ERROR_DESCRIPTION_MARGIN        = Constants.Scintilla.SC_MAX_MARGIN - 1;
        private int _lineNumberStyleBackground            = int.MinValue;
        private const int MARGIN_MASK                     = ((1 << (int)MarkerId.MarkerId_Info) | (1 << (int)MarkerId.MarkerId_Debug) | (1 << (int)MarkerId.MarkerId_Warning) | (1 << (int)MarkerId.MarkerId_Error) | (1 << (int)MarkerId.MarkerId_FatalError));
        private const int MARGIN_WIDTH                    = 16;

        #region [XPM]
        /* XPM */
        readonly string XMP_16X16_ERROR_ICON = string.Empty;

        #endregion

        #endregion

        #region [Interface]

        internal MarginManager(ISettings settings, INpp nppHelper, Plugin plugin, string workspaceRoot, ILineVisibilityObserver lineVisibilityObserver) :
            base(settings, nppHelper, plugin, workspaceRoot, lineVisibilityObserver)
        {
            _areAnnotationEnabled      = _settings.Get<bool>(Settings.RTextNppSettings.EnableErrorMarkers);
            _lineNumberStyleBackground = _nppHelper.GetStyleBackground(_nppHelper.MainScintilla, (int)SciMsg.STYLE_LINENUMBER);
            Bitmap bmp = new Bitmap(Resources.marker_error);
            bmp.MakeTransparent(Color.Black);
            XMP_16X16_ERROR_ICON = Pixmap.FromBitmap(bmp).GetPixmap();
            bmp.Dispose();
        }

        public override void OnSettingChanged(object source, Utilities.Settings.Settings.SettingChangedEventArgs e)
        {
            if (e.Setting == SETTING)
            {
                bool aNewSettingValue = _settings.Get<bool>(Settings.RTextNppSettings.EnableErrorMarkers);
                if (aNewSettingValue != _areAnnotationEnabled)
                {
                    ProcessSettingChanged(aNewSettingValue);
                }
            }
        }

        protected override Constants.StyleId ConvertSeverityToStyleId(ErrorItemViewModel.SeverityType severity)
        {
            switch (severity)
            {
                case ErrorItemViewModel.SeverityType.Debug:
                    return Constants.StyleId.MARGIN_DEBUG;
                case ErrorItemViewModel.SeverityType.Info:
                    return Constants.StyleId.MARGIN_INFO;
                case ErrorItemViewModel.SeverityType.Warning:
                    return Constants.StyleId.MARGIN_WARNING;
                case ErrorItemViewModel.SeverityType.Error:
                    return Constants.StyleId.MARGIN_ERROR;
                case ErrorItemViewModel.SeverityType.Fatal:
                    return Constants.StyleId.MARGIN_FATAL_ERROR;
                default:
                    return Constants.StyleId.MARGIN_ERROR;
            }
        }

        protected override void OnVisibilityInfoUpdated(VisibilityInfo info, IntPtr sciPtr)
        {
            base.OnVisibilityInfoUpdated(info, sciPtr);
            if (IsWorkspaceFile(info.File) && _areAnnotationEnabled)
            {
                //update current annotations
                PlaceAnnotations(info.ScintillaHandle, true);
            }
        }

        #endregion

        #region [Event Handlers]      
        
        protected override void OnBufferActivated(object source, string file, View view)
        {
            PreProcessOnBufferActivatedEvent(file, view);
            if (!Utilities.FileUtilities.IsRTextFile(file, _settings, _nppHelper))
            {
                //remove annotations from the view which this file belongs to
                var scintilla = _nppHelper.ScintillaFromView(view);
                HideAnnotations(scintilla);
                SetActiveFile(scintilla, string.Empty);
            }
        }
        
        #endregion

        #region [Helpers]

        protected override void HideAnnotations(IntPtr scintilla)
        {
            _nppHelper.DeleteMarkers(scintilla, (int)MarkerId.MarkerId_Info);
            _nppHelper.DeleteMarkers(scintilla, (int)MarkerId.MarkerId_Debug);
            _nppHelper.DeleteMarkers(scintilla, (int)MarkerId.MarkerId_Warning);
            _nppHelper.DeleteMarkers(scintilla, (int)MarkerId.MarkerId_Error);
            _nppHelper.DeleteMarkers(scintilla, (int)MarkerId.MarkerId_FatalError);
            _nppHelper.SetMarginMaskN(scintilla, ERROR_DESCRIPTION_MARGIN, 0);
            _nppHelper.SetMarginWidthN(scintilla, ERROR_DESCRIPTION_MARGIN, 0);
        }

        protected override bool DrawAnnotations(ErrorListViewModel errors, IntPtr sciPtr)
        {
            bool aSuccess = true;
            if (!IsNotepadShutingDown && _areAnnotationEnabled)
            {
                try
                {
                    //cancel any pending task
                    var task = GetDrawingTask(sciPtr);
                    var cts = GetCts(sciPtr);
                    if (task != null && !(task.IsCanceled || task.IsCompleted))
                    {
                        cts.Cancel();
                        try
                        {
                            task.Wait();
                        }
                        catch (AggregateException ex)
                        {
                            ex.Handle(ae => true);
                        }
                        SetAnnotations<IEnumerable>(sciPtr, null);
                    }
                    HideAnnotations(sciPtr);
                    //start new task
                    var newCts                     = new CancellationTokenSource();
                    ConcurrentBag<int> annotations = new ConcurrentBag<int>();
                    if(errors == null || errors.ErrorList == null || errors.ErrorList.Count == 0)
                    {
                        SetAnnotations(sciPtr, annotations);
                        return true;
                    }
                    string activeFile = errors.FilePath.Replace("/", "\\");
                    SetCts(sciPtr, newCts);
                    SetDrawingFile(sciPtr, activeFile);
                    var newTask = Task.Factory.StartNew(() =>
                    {
                        var aErrorGroupByLines = errors.ErrorList.GroupBy(y => y.Line);
                        //concatenate error that share the same line with \n so that they appear in the same annotation box underneath the same line
                        foreach (var aErrorGroup in aErrorGroupByLines)
                        {
                            bool hasActiveFileChanged = GetActiveFile(sciPtr) != activeFile;
                            //if file is no longer active in this scintilla we have to break!
                            if (newCts.Token.IsCancellationRequested || hasActiveFileChanged || IsNotepadShutingDown)
                            {
                                if (!newCts.Token.IsCancellationRequested)
                                {
                                    //ensure that subsequent task won't run
                                    newCts.Cancel();
                                }
                                aSuccess = false;
                                break;
                            }
                            annotations.Add(aErrorGroup.First().LineForScintilla);
                        }
                    }, newCts.Token).ContinueWith((x) =>
                    {
                        SetAnnotations(sciPtr, annotations.OrderBy( y => y));
                        PlaceAnnotations(sciPtr);
                    }, newCts.Token, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.Default);;
                    SetDrawingTask(sciPtr, newTask);
                }
                catch (Exception)
                {
                    Trace.WriteLine("DrawAnnotations failed.");
                    return false;
                }
            }
            else
            {
                return false;
            }
            return aSuccess;
        }

        protected override void PlaceAnnotations(IntPtr sciPtr, bool waitForTask = false)
        {
            if (!IsNotepadShutingDown)
            {
                var aIndicatorRanges = (IEnumerable<int>)GetAnnotations(sciPtr);
                var aVisibilityInfo  = GetVisibilityInfo(sciPtr);
                var runningTask      = GetDrawingTask(sciPtr);
                var activeFile       = GetDrawingFile(sciPtr);
                HideAnnotations(sciPtr);
                if (aIndicatorRanges != null && (!waitForTask || (runningTask == null || runningTask.IsCompleted)))
                {
                    var markerLines = from range in aIndicatorRanges
                                      where range >= aVisibilityInfo.FirstLine && range <= aVisibilityInfo.LastLine
                                      select range;
                    _nppHelper.SetMarginMaskN(sciPtr, ERROR_DESCRIPTION_MARGIN, MARGIN_MASK);
                    _nppHelper.SetMarginWidthN(sciPtr, ERROR_DESCRIPTION_MARGIN, MARGIN_WIDTH);

                    _nppHelper.DefineXpmSymbol(sciPtr, (int)MarkerId.MarkerId_Info, XMP_16X16_ERROR_ICON);
                    _nppHelper.DefineXpmSymbol(sciPtr, (int)MarkerId.MarkerId_Debug, XMP_16X16_ERROR_ICON);
                    _nppHelper.DefineXpmSymbol(sciPtr, (int)MarkerId.MarkerId_Warning, XMP_16X16_ERROR_ICON);
                    _nppHelper.DefineXpmSymbol(sciPtr, (int)MarkerId.MarkerId_Error, XMP_16X16_ERROR_ICON);
                    _nppHelper.DefineXpmSymbol(sciPtr, (int)MarkerId.MarkerId_FatalError, XMP_16X16_ERROR_ICON);
                    NormalizeMarginsBackground(sciPtr);
                    foreach (var markerLine in markerLines)
                    {
                        if (IsNotepadShutingDown || (activeFile != GetActiveFile(sciPtr)))
                        {
                            return;
                        }
                        _nppHelper.AddMarker(sciPtr, markerLine, (int)MarkerId.MarkerId_Error);
                    }
                }
            }
        }

        private void NormalizeMarginsBackground(IntPtr sciPtr)
        {
            var aLineNumberBackground = _nppHelper.GetStyleBackground(sciPtr, (int)SciMsg.STYLE_LINENUMBER);
            for(int i = 0; i < (int)Enum.GetValues(typeof(MarkerId)).Length; ++i)
            {
                _nppHelper.SetMarkerBackground(sciPtr, i, aLineNumberBackground);
                _nppHelper.SetMarkerForeground(sciPtr, i, aLineNumberBackground);
            }
            
            
        }
        #endregion
    }
}