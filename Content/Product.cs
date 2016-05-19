using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Drawing;
using System.Windows.Controls;

namespace ArtContentManager.Content
{
    class Product
    {
        private int _ID;
        private string _Name;
        private Image _Thumbnail;
        private bool _IsPrimary;
        private Dictionary<int, int> _dctCreatorIDs = new Dictionary<int, int>();

        private List<File> _ComponentFiles; // Just the parent files

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

        public Image Thumbnail
        {
            get { return _Thumbnail; }
            set { _Thumbnail = value; }
        }

        public bool IsPrimary
        {
            get { return _IsPrimary; }
            set { _IsPrimary = value; }
        }

        public Dictionary<int, int> Creators
        {
            get { return _dctCreatorIDs; }
        }
    }
}
