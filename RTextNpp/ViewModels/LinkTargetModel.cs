using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;
using Microsoft.VisualStudio.Language.Intellisense;
using RTextNppPlugin.WpfControls;

namespace RTextNppPlugin.ViewModels
{
    class LinkTargetModel
    {
        public string Display { get; private set; }
        public string Description { get; private set; }
        public  int Line { get; private set; }
        public string File { get; private set; }
        public string FilePath { get; private set; }
        public LinkTargetModel(string display, string description, int line, string file)
        {
            Display     = display;
            Description = description;
            Line        = line;
            File        = Path.GetFileName(file);
            FilePath    = file;
        }
    }

    class ReferenceLinkViewModel : BindableObject
    {
        private BulkObservableCollection<LinkTargetModel> _targets = new BulkObservableCollection<LinkTargetModel>();
        private string _errorMsg                                   = String.Empty;
        private string _errorTooltip                               = String.Empty;
        private double _zoomLevel                                  = 100.0;

        public BulkObservableCollection<LinkTargetModel> Targets
        {
            get
            {
                return _targets;
            }
            set
            {
                _targets = value;
            }
        }

        public double ZoomLevel
        {
            get
            {
                return _zoomLevel;
            }
            set
            {
                if (value != _zoomLevel)
                {
                    _zoomLevel = value;
                    base.RaisePropertyChanged("ZoomLevel");
                }
            }
        }

        internal void CreateWarning(string error, string tooltip)
        {
            if (!String.IsNullOrEmpty(error))
            {
                _targets.Clear();
                ErrorMsg     = error;
                ErrorTooltip = tooltip;
            }
        }

        public string ErrorTooltip
        {
            get
            {
                return _errorTooltip;
            }
            set
            {                
                if (!value.Equals(_errorTooltip))
                {
                    _errorTooltip = value;
                    base.RaisePropertyChanged("ErrorTooltip");
                }
            }
        }

        public string ErrorMsg
        {
            get
            {
                return _errorMsg.ToString();
            }
            set
            {
                if (!value.Equals(_errorMsg))
                {
                    _errorMsg = value;
                    base.RaisePropertyChanged("ErrorMsg");
                }
            }
        }

        internal void Clear()
        {
            _targets.Clear();
        }

        internal void RemoveWarning()
        {
            ErrorMsg     = String.Empty;
            ErrorTooltip = String.Empty;
        }
    }
}
