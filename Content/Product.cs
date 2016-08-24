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

        public Dictionary<string, string> ImageFiles(string productName)
        {
            // Returns a dictionary of images, indexed by a role key 
            // The role key is a three charcter string which is either a numeric presentation order
            // or the coded value "TBN" for a thumb nail image.

            // The location is determined by the productName. The routine allows the value
            // passed for the name to be specified as a parameter so that we can used the routine
            // when moving image items from one name to another. 
                        
            Dictionary<string, string> imageFiles = new Dictionary<string, string>();

            // The position where we start our three character index (000, 001 ... or TBN for Thumbnail)
            int startOfImageIndex = ImageFolder(productName).Length + Name.Length + 1;

            System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(ImageFolder(productName));

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

        private string ImageFolder(string productName)
        {
            string subFolderName;

            if (productName.Length >= SUBFOLDER_NAME_LENGTH)
            {
                subFolderName = productName.Substring(0, SUBFOLDER_NAME_LENGTH);
            }
            else
            {
                subFolderName = "___";
            }

            return Static.DatabaseAgents.dbaSettings.Setting("ImageStoreFolder").Item1 + @"\" + subFolderName;

        }

        public void MoveImageFiles()
        {

            // Checks to see if a product has been renamed and if so, renames and moves image resources

            if (_Name == _NameSavedToDatabase)
            {
                // Nothing to do so bail out...
                goto ImageMoveComplete;
            }

            if (!Directory.Exists(ImageFolder(_Name)))
            {
                Directory.CreateDirectory(ImageFolder(_Name));
            }

            foreach (string oldImageFileName in ImageFiles(_NameSavedToDatabase).Values)
            {
                // First amend the full path to point to the new directory
                string newImageFileName = oldImageFileName.Replace(ImageFolder(_Name), ImageFolder(_NameSavedToDatabase));
                // Now change the name of the file itself
                newImageFileName = newImageFileName.Replace(_NameSavedToDatabase, _Name);

                System.IO.File.Move(oldImageFileName, newImageFileName);
            }

        ImageMoveComplete:

            _NameSavedToDatabase = _Name;
            return;

        }
    }
}
