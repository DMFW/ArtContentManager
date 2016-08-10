using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtContentManager.Content
{
    public class Installation
    {
        private int _rootID;
        private string _rootPath;
        private InstallationType _type;

        public int RootID
        {
            get { return _rootID; }
            set { _rootID = value; }
        }

        public string RootPath
        {
            get { return _rootPath; }
            set { _rootPath = value; }
        }

        public InstallationType Type
        {
            get { return _type; }
            set { _type = value; }
        }

        public InstallationType.Category InstallationCategory(string path)
        {
            // Return a category object if the specified directory
            // can be classified as an organisation category for the installation.
            // Otherwise return null.

            foreach (KeyValuePair<string, InstallationType.Category> kvpCategory in _type.Categories)
            {
                if (path.Contains(kvpCategory.Key))
                {
                    return kvpCategory.Value;
                }
            }

            return null;

        }

    }
}
