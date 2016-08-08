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

        public bool IsInstallationCategory(string path)
        {
            // Return true if the specified directory can be classified as an organisation category for the installation

            foreach (string categoryRoot in _type.CategoryRoots.Keys)
            {
                if (path.Contains(categoryRoot))
                {
                    return true;
                }
            }

            return false;

        }

    }
}
