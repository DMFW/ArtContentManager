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

        private string _Name;
        private string _URIPath;

        public Skin()
        {
        }

        public string Name
        {
            get { return _Name; }
            set
            {
                if (_Name != value)
                {
                    _Name = value;
                    NotifyPropertyChanged("Name");
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
