using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtContentManager.Content
{
    class MarketPlace
    {
        private int _ID;
        private string _Name;
        private Uri _URI;

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
        public Uri Uri
        {
            get { return _URI; }
            set { _URI = value; }
        }

    }
}
