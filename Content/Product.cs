using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Controls;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ArtContentManager.Content
{
    public class Product : INotifyPropertyChanged
    {
        private int _ID;
        private string _Name;
        private string _NameSavedToDatabase;
        private bool _IsPrimary;
        private DateTime? _DatePurchased;
        private int? _MarketPlaceID;
        private string _ProductURI;
        private string _OrderURI;

        // Just the parent files
        private List<File> _lstInstallationFiles = new List<File>();
        private List<File> _lstContentFiles = new List<File>();
        private List<File> _lstTextFiles = new List<File>();
        private List<Creator> _lstCreators = new List<Creator>();

        private Dictionary<string, string> _dctImageFiles = new Dictionary<string, string>();

        public event PropertyChangedEventHandler PropertyChanged;

        // This method is called by the Set accessor of each property.
        // The CallerMemberName attribute that is applied to the optional propertyName
        // parameter causes the property name of the caller to be substituted as an argument.
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public int ID
        {
            get { return _ID; }
            set { _ID = value; }
        }

        public string Name
        {
            get { return _Name; }
            set
            {
                // Names with spaces in the first three characters are not permitted.
                // Modify them here to use underscores.

                string newName = value;
                string nameHeader = newName.Substring(0, ArtContentManager.Static.ProductImageManager.SUBFOLDER_NAME_LENGTH);
                string NameTrailer = newName.Substring(ArtContentManager.Static.ProductImageManager.SUBFOLDER_NAME_LENGTH);

                nameHeader = nameHeader.Replace(" ", "_");
                newName = nameHeader + NameTrailer;

                if (newName != _Name)
                {
                    _Name = newName;
                    NotifyPropertyChanged();
                }
            }
        }

        public string NameSavedToDatabase
        {
            get { return _NameSavedToDatabase; }
            set { _NameSavedToDatabase = value; }
        }

        public string ProductURI
        {
            get { return _ProductURI; }
            set
            {
                if (value != _ProductURI)
                {
                    _ProductURI = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public string OrderURI
        {
            get { return _OrderURI; }
            set
            {
                if (value != _OrderURI)
                {
                    _OrderURI = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public bool IsPrimary
        {
            get { return _IsPrimary; }
            set { _IsPrimary = value; }
        }

        public DateTime? DatePurchased
        {
            get {

                return _DatePurchased; }
            set
            {
                if (value != _DatePurchased)
                {
                    _DatePurchased = value;
                    NotifyPropertyChanged();
                }
            }
        }

        public int? MarketPlaceID
        {
            get { return _MarketPlaceID; }
            set { _MarketPlaceID = value; }
        }

        public List<File> InstallationFiles
        {
            get { return _lstInstallationFiles; }
            set { _lstInstallationFiles = value; }
        }

        public List<File> ContentFiles
        {
            get { return _lstContentFiles; }
            set { _lstContentFiles = value; }
        }

        public List<File> TextFiles
        {
            get { return _lstTextFiles; }
            set { _lstTextFiles = value; }
        }

        public List<Creator> Creators
        {
            get { return _lstCreators; }
            set { _lstCreators = value; }
        }

        public Dictionary<string, string> ImageFiles(bool forceLoad)
        {
            // Returns a dictionary of images, indexed by a role key 
            // The role key is a three charcter string which is either a numeric presentation order
            // or the coded value "tbn" for a thumb nail image.

            if (forceLoad == false) { return _dctImageFiles; }

            _dctImageFiles = ArtContentManager.Static.ProductImageManager.ImageFiles(_Name);
            return _dctImageFiles;
          
        }

    }
}
