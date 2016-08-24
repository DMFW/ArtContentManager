﻿using System;
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
        private int _MarketPlaceID;
        private string _ProductURI;
              
        // Just the parent files
        private List<File> _lstInstallationFiles = new List<File>();
        private List<File> _lstContentFiles = new List<File>();
        private List<File> _lstTextFiles = new List<File>();
        private List<Creator> _lstCreators = new List<Creator>();

        private const short SUBFOLDER_NAME_LENGTH = 3;
        private const short PRODUCT_NAME_KEY_LENGTH = 10;

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
                if (value != _Name)
                {
                    _Name = value;
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

        public int MarketPlaceID
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

        public Dictionary<string, string> ImageFiles
        {
            // Returns a dictionary of images, indexed by a role key 
            // The role key is a three charcter string which is either a numeric presentation order
            // or the coded value "TBN" for a thumb nail image 

            get
            {
                Dictionary<string, string> imageFiles = new Dictionary<string, string>();

                // The position where we start our three character index (000, 001 ... or TBN for Thumbnail)
                int startOfImageIndex = ImageFolder().Length + Name.Length + 1;

                System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(ImageFolder());

                if (dir.Exists)
                {
                    IEnumerable<System.IO.FileInfo> fileList = dir.GetFiles("*.*", System.IO.SearchOption.AllDirectories);

                    var queryMatchingFiles =
                    from file in fileList
                    where file.Name.Substring(0, Name.Length).ToUpperInvariant() == Name.ToUpperInvariant()
                    select file.FullName;

                    foreach (string filename in queryMatchingFiles)
                    {
                        imageFiles.Add(filename.Substring(startOfImageIndex, 3), filename);
                    }
                }
                return imageFiles;
            }
        }

        private string ImageFolder()
        {
            string subFolderName;

            if (_Name.Length >= SUBFOLDER_NAME_LENGTH)
            {
                subFolderName = _Name.Substring(0, SUBFOLDER_NAME_LENGTH);
            }
            else
            {
                subFolderName = "___";
            }

            return Static.DatabaseAgents.dbaSettings.Setting("ImageStoreFolder").Item1 + @"\" + subFolderName;

        }
    }
}
