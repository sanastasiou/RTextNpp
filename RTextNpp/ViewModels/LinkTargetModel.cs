using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Text;

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

    class ReferenceLinkViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<LinkTargetModel> mTargets = new ObservableCollection<LinkTargetModel>();
        private StringBuilder mBusyString = new StringBuilder("Bla");
        private double mZoomLevel = 100.0;

        /**
         * @fn  private void RaisePropertyChanged(string caller)
         *
         * @brief   Raises the property changed event for a specific property.
         *
         * @author  Stefanos Anastasiou
         * @date    23.01.2013
         *
         * @param   caller  The caller.
         */
        private void RaisePropertyChanged(string caller)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(caller));
            }
        }

        public ObservableCollection<LinkTargetModel> Targets
        {
            get
            {
                return mTargets;
            }
            set
            {
                mTargets = value;
            }
        }

        public double IWpfZoomLevel
        {
            get
            {
                return mZoomLevel;
            }
            set
            {
                if ( value != mZoomLevel )
                {
                    mZoomLevel = value;
                    RaisePropertyChanged("IWpfZoomLevel");
                }
            }
        }

        public string BackendBusyString
        {
            get
            {
                return mBusyString.ToString();
            }
            set
            {
                if (!value.Equals(mBusyString.ToString()))
                {
                    mBusyString.Clear();
                    mBusyString.Append(value);
                    RaisePropertyChanged("BackendBusyString");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
