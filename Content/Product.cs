using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Controls;

namespace ArtContentManager.Content
{
    public class Product
    {
        private int _ID;
        private string _Name;
        private bool _IsPrimary;
        private DateTime _DatePurchased;
        private int _MarketPlaceID;
        private string _ProductURI;
              
        // Just the parent files
        private List<File> _lstInstallationFiles = new List<File>();
        private List<File> _lstContentFiles = new List<File>();
        private List<Creator> _lstCreators = new List<Creator>();
        private List<ImageResource> _lstSupportingImages = new List<ImageResource>();

        private ImageResource _Thumbnail = new ImageResource();

        public int ID
        {
            get { return _ID; }
            set { _ID = value; }
        }

        public string Name
        {
            get { return _Name; }
            set { _Name = value; }
        }

        public string ProductURI
        {
            get { return _ProductURI; }
            set { _ProductURI = value; }
        }

        public bool IsPrimary
        {
            get { return _IsPrimary; }
            set { _IsPrimary = value; }
        }

        public DateTime DatePurchased
        {
            get { return _DatePurchased; }
            set { _DatePurchased = value; }
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

        public List<Creator> Creators
        {
            get { return _lstCreators; }
            set { _lstCreators = value; }
        }

        public List<ImageResource> SupportingImages
        {
            get { return _lstSupportingImages; }
            set { _lstSupportingImages = value; }
        }

        public ImageResource Thumbnail
        {
            get { return _Thumbnail; }
            set { _Thumbnail = value; }
        }

        public ImageResource PrimaryImage
        {
            get {
                    if (_lstSupportingImages != null)
                    {
                        if (_lstSupportingImages.Count > 0)
                        {
                            return _lstSupportingImages[0];
                        }
                        else
                        {
                            return null;
                        }
                    }
                    else
                    {
                        return null;
                    }   
                }
        }

    }
}
