using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.Language.Intellisense;
using RTextNppPlugin.RText.Protocol;
using RTextNppPlugin.WpfControls;

namespace RTextNppPlugin.ViewModels
{
    internal class LinkTargetModel
    {
        public string Display { get; private set; }
        public string Description { get; private set; }
        public string Line { get; private set; }
        public string File { get; private set; }
        public string FilePath { get; private set; }
        public LinkTargetModel(string display, string description, string line, string file)
        {
            Display     = display;
            Description = description;
            Line        = line;
            File        = Path.GetFileName(file);
            FilePath    = file;
        }
    }

    internal class ReferenceLinkViewModel : BindableObject
    {
        private BulkObservableCollection<LinkTargetModel> _targets = new BulkObservableCollection<LinkTargetModel>();
        private string _errorMsg                                   = String.Empty;
        private string _errorTooltip                               = String.Empty;
        private double _zoomLevel                                  = 1.0;

        public BulkObservableCollection<LinkTargetModel> Targets
        {
            get
            {
                return _targets;
            }
        }

        internal bool IsEmpty()
        {
            return _targets.Count == 0;
        }

        internal void UpdateLinkTargets(IEnumerable<Target> targets)
        {
            _targets.Clear();
            var aLinkTargetModels = targets.Select(target => new LinkTargetModel(target.display, target.desc, target.line, target.file));
            _targets.AddRange(aLinkTargetModels.OrderBy( x => x.File));
        }

        internal void OnZoomLevelChanged(double newZoomLevel)
        {
            //calculate actual zoom level , based on Scintilla zoom factors...

            //try 8% increments / decrements
            ZoomLevel = (1.0 + (Constants.ZOOM_FACTOR * newZoomLevel));
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
                    if(!String.IsNullOrEmpty(value))
                    {
                        Clear();
                    }
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
