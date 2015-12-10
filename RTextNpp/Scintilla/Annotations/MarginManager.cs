using RTextNppPlugin.Utilities.Settings;
using RTextNppPlugin.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace RTextNppPlugin.Scintilla.Annotations
{
    class MarginManager : ErrorBase, IError
    {
        #region [Data Members]
        private const Settings.RTextNppSettings SETTING = Settings.RTextNppSettings.EnableErrorMarkers;
        #endregion

        #region [Interface]
        internal MarginManager(ISettings settings, INpp nppHelper, Plugin plugin, string workspaceRoot, double updateDelay = Constants.Scintilla.ANNOTATIONS_UPDATE_DELAY) :
            base(settings, nppHelper, plugin, workspaceRoot, updateDelay)
        {
            _areAnnotationEnabled = _settings.Get<bool>(Settings.RTextNppSettings.EnableErrorMarkers);
            for(int i = 0; i < 5; ++i)
            {
                Trace.WriteLine(String.Format("Margin type for n : [{0}] is {1}", i, _nppHelper.GetMarginTypeN(_nppHelper.MainScintilla, i)));
                Trace.WriteLine(String.Format("Margin width for n : [{0}] is {1}", i, _nppHelper.GetMarginWidthN(_nppHelper.MainScintilla, i)));
            }
        }

        public override void OnSettingChanged(object source, Utilities.Settings.Settings.SettingChangedEventArgs e)
        {
            if (e.Setting == SETTING)
            {
                _areAnnotationEnabled = _settings.Get<bool>(Settings.RTextNppSettings.EnableErrorMarkers);
            }
            ProcessSettingChanged();
        }

        #endregion

        #region [Helpers]

        protected override object OnBufferActivated(string file)
        {
            PreProcessOnBufferActivatedEvent();
            if (!Utilities.FileUtilities.IsRTextFile(file, _settings, _nppHelper))
            {
                //remove annotations from the view which this file belongs to
                var scintilla = _nppHelper.FindScintillaFromFilepath(file);
                _nppHelper.ClearAllTextMargins(scintilla);
                if (scintilla == _nppHelper.MainScintilla)
                {
                    _lastMainViewAnnotatedFile = string.Empty;
                }
                else
                {
                    _lastSubViewAnnotatedFile = string.Empty;
                }
            }
            return null;
        }

        private void ShowAnnotations(IntPtr scintilla)
        {
            if (_areAnnotationEnabled)
            {
                //_nppHelper.SetAnnotationVisible(scintilla, Constants.Scintilla.BOXED_ANNOTATION_STYLE);
            }
        }

        protected override void HideAnnotations(IntPtr scintilla)
        {
            var openFiles = _nppHelper.GetOpenFiles(scintilla);
            var docIndex = _nppHelper.CurrentDocIndex(scintilla);
            if (Utilities.FileUtilities.IsRTextFile(openFiles[docIndex], _settings, _nppHelper))
            {
                if ((Utilities.FileUtilities.FindWorkspaceRoot(openFiles[docIndex]) + Path.GetExtension(openFiles[docIndex])).Equals(_workspaceRoot))
                {
                    _nppHelper.ClearAllTextMargins(scintilla);
                    //_nppHelper.SetAnnotationVisible(scintilla, Constants.Scintilla.HIDDEN_ANNOTATION_STYLE);
                }
            }
        }

        protected override void DrawAnnotations(ErrorListViewModel errors, IntPtr sciPtr)
        {
            HideAnnotations(sciPtr);
            try
            {
                if (errors != null)
                {
                    //concatenate error that share the same line with \n so that they appear in the same annotation box underneath the same line
                    var aErrorGroupByLines = errors.ErrorList.GroupBy(y => y.Line).AsParallel();
                    foreach (var errorGroup in aErrorGroupByLines)
                    {
                        _nppHelper.SetMarginStyle(sciPtr, errorGroup.First().LineForScintilla, 1);
                        _nppHelper.SetMarginText(sciPtr, errorGroup.First().LineForScintilla, errorGroup.First().Severity.ToString());
                        //StringBuilder aErrorDescription = new StringBuilder(errorGroup.Count() * 50);
                        //int aErrorCounter = 0;
                        //foreach (var error in errorGroup)
                        //{
                        //    aErrorDescription.AppendFormat("{0} at line : {1} - {2}", error.Severity, error.Line, error.Message);
                        //    if (++aErrorCounter < errorGroup.Count())
                        //    {
                        //        aErrorDescription.Append("\n");
                        //    }
                        //}
                        ////npp offset for line todo - add multiple styles
                        //_nppHelper.SetAnnotationStyle((errorGroup.First().Line - 1), Constants.StyleId.ANNOTATION_ERROR);
                        //_nppHelper.AddAnnotation((errorGroup.First().Line - 1), aErrorDescription);
                    }
                    ShowAnnotations(sciPtr);
                }
            }
            catch (Exception)
            {
                Trace.WriteLine("Draw margins failed.");
            }
        }

        #endregion
    }
}