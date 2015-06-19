using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using Microsoft.VisualStudio.Language.Intellisense;
using RTextNppPlugin.RText.Protocol;
using RTextNppPlugin.Utilities.Settings;
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
        private double _rowDetailsWidth                            = 0.0;
        private Thickness _thickness                               = new Thickness(0.0);
        private double _labelsWidth                                = Constants.INITIAL_WIDTH_LINK_REFERENCE_LABELS;
        private double _maxLinkTextSize                            = 0.0;
        private ISettings _settings                                = null;
        private double _referenceLinkColumnWidth                   = 0.0;
        
        internal ReferenceLinkViewModel(ISettings settings)
        {
            _settings = settings;
            _referenceLinkColumnWidth = _settings.Get<double>(Settings.RTextNppSettings.ReferenceLinkColumnWidth);
        }

        public double ReferenceLinkColumnWidth
        {
            get
            {
                return _referenceLinkColumnWidth;
            }
            set
            {
                if(value != _referenceLinkColumnWidth)
                {
                    _referenceLinkColumnWidth = value;
                    base.RaisePropertyChanged("ReferenceLinkColumnWidth");
                    _settings.Set(_referenceLinkColumnWidth, Settings.RTextNppSettings.ReferenceLinkColumnWidth);
                }
            }
        }

        public BulkObservableCollection<LinkTargetModel> Targets
        {
            get
            {
                return _targets;
            }
        }

        public double MaxLinkTextSize
        {
            get
            {
                return _maxLinkTextSize;
            }
            set
            {
                if (value != _maxLinkTextSize)
                {
                    _maxLinkTextSize = value;
                    base.RaisePropertyChanged("MaxLinkTextSize");
                }
            }
        }

        public double LabelsWidth
        {
            get
            {
                return _labelsWidth;
            }
            set
            {
                if(value != _labelsWidth)
                {
                    _labelsWidth = value;
                    base.RaisePropertyChanged("LabelsWidth");
                }
            }
        }

        public Thickness RowDetailsOffset
        {
            get
            {
                return _thickness;
            }
            set
            {
                if(value != _thickness)
                {
                    _thickness = value;
                    base.RaisePropertyChanged("RowDetailsOffset");
                }
            }
        }

        public double RowDetailsWidth
        {
            get
            {
                return _rowDetailsWidth;
            }
            set
            {
                if(value != _rowDetailsWidth)
                {
                    _rowDetailsWidth = value;
                    base.RaisePropertyChanged("RowDetailsWidth");
                }
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
