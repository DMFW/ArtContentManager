using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;

namespace ArtContentManager.Skins
{
    public class Skin : INotifyPropertyChanged
    {

        private string _SkinName;
        private string _URIPath;

        public Skin()
        {
        }

        public string SkinName
        {
            get { return _SkinName; }
            set
            {
                if (_SkinName != value)
                {
                    _SkinName = value;
                    NotifyPropertyChanged("SkinName");
                }
            }
        }

        public string URIPath
        {
            get { return _URIPath; }
            set
            {
                if (_URIPath != value)
                {
                    _URIPath = value;
                    NotifyPropertyChanged("URIPath");
                }
            }
        }

        #region INotifyPropertyChanged Members

        private void NotifyPropertyChanged(string propertyName)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        #endregion
    }
}
