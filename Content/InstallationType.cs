using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtContentManager.Content
{
    public class InstallationType
    {
        private int _typeID;
        private string _softwareType;
        private string _identifyingDirectoryName;

        public class Category
        {
            public string RelativePath;
            public string Name;
        }

        private Dictionary<string, Category> _categories = new Dictionary<string, Category>();

        public int TypeID
        {
            get { return _typeID; }
            set { _typeID = value; }
        }

        public string SoftwareType
        {
            get { return _softwareType; }
            set { _softwareType = value; }
        }

        public string IdentifyingDirectoryName
        {
            get { return _identifyingDirectoryName; }
            set { _identifyingDirectoryName = value; }
        }

        public Dictionary<string, Category> Categories
        {
            get { return _categories; }
        }

    }
}
