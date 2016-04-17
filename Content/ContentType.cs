using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtContentManager.Content
{
    class ContentType
    {
        private string _ContentDirectoryName;
        private string _Description;
        private int _Importance;

        public string ContentDirectoryName
        {
            get { return _ContentDirectoryName; }
            set { _ContentDirectoryName = value; }
        }

        public string Description
        {
            get { return _Description; }
            set { _Description = value; }
        }

        public int Importance
        {
            get { return _Importance; }
            set { _Importance = value; }
        }

    }
}
