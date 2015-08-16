using Microsoft.VisualStudio.Language.Intellisense;
using RTextNppPlugin.RText.Protocol;
using RTextNppPlugin.WpfControls;
using System.Collections.Generic;
using System.IO;

namespace RTextNppPlugin.ViewModels
{
    internal class ErrorItemViewModel : BindableObject
    {
        internal enum SeverityType
        {
            Debug,
            Info,
            Warning,
            Error,
            Fatal
        }
        #region [Interface]
        
        public string Message { get; private set; }
        public SeverityType Severity { get; private set; }
        public int Line { get; private set; }
        public string File { get; private set; }
        public string FilePath { get; private set; }
        public ErrorItemViewModel(SpecificError error, string filepath)
        {
            Message     = error.message;
            Severity    = ConvertStringToSeverity(error.severity);
            Line        = error.line;
            File        = Path.GetFileName(filepath);
            FilePath    = filepath;
        }
        
        #endregion

        #region [Helpers]
        SeverityType ConvertStringToSeverity(string severity)
        {
            switch(severity)
            {
                case Constants.SEVERITY_DEBUG:
                    return SeverityType.Debug;
                case Constants.SEVERITY_INFO:
                    return SeverityType.Info;
                case Constants.SEVERITY_WARNING:
                    return SeverityType.Warning;
                case Constants.SEVERITY_ERROR:
                    return SeverityType.Error;
                default:
                    return SeverityType.Error;
            }
        }
        #endregion
    }

    internal class ErrorListViewModel : BindableObject
    {
        #region [Interface]
        public string FilePath { get; private set; }
        public BulkObservableCollection<ErrorItemViewModel> ErrorList
        { 
            get
            {
                return _errorList;
            }
        }
        public bool IsFileOpened
        {
            get
            {
                return _isFileOpened;
            }
            set
            {
                if (value != _isFileOpened)
                {
                    _isFileOpened = value;
                    base.RaisePropertyChanged("IsFileOpened");
                    System.Diagnostics.Trace.WriteLine(System.String.Format("Is expanded file : {0}, {1}", FilePath, value));
                }
            }
        }

        public ErrorListViewModel(string filepath, IEnumerable<ErrorItemViewModel> errors, bool isFileOpened)
        {
            FilePath     = filepath;
            _errorList.AddRange(errors);
            IsFileOpened = isFileOpened;
        }
        #endregion

        #region [Data Members]
        private bool _isFileOpened = false;
        private BulkObservableCollection<ErrorItemViewModel> _errorList = new BulkObservableCollection<ErrorItemViewModel>();
        #endregion
    }
}
