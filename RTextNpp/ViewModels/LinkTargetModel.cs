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
        private StringBuilder _busyString                          = new StringBuilder();
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

        public string BackendBusyString
        {
            get
            {
                return _busyString.ToString();
            }
            set
            {
                if (!value.Equals(_busyString.ToString()))
                {
                    _busyString.Clear();
                    _busyString.Append(value);
                    base.RaisePropertyChanged("BackendBusyString");
                }
            }
        }
    }
}
