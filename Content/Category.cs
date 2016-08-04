using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ArtContentManager.Content
{
    class Category
    {
        private string _directoryPath;
        private int _depth;
        private bool _isValidCategory;

        public Category(string directoryPath)
        {
            int runtimePosition = -1;
            int libraryPosition = -1;
            int workingDirectoryStart = -1;
            int nextDirectoryStart = -1;
            _isValidCategory = false;

            _directoryPath = directoryPath;

            runtimePosition = _directoryPath.IndexOf(@"Runtime\libraries\");
            _depth = 0;

            if (runtimePosition > 0)
            {

                foreach (string categoryParentFolder in ArtContentManager.Static.FileSystemScan.CategoryParentFolderNames)
                {
                    libraryPosition = _directoryPath.IndexOf(categoryParentFolder + @"\", runtimePosition);
                    if (libraryPosition > 0)
                    {
                        workingDirectoryStart = libraryPosition + categoryParentFolder.Length + 2;
                        do
                        {
                            _depth++;
                            nextDirectoryStart = _directoryPath.IndexOf(@"\", libraryPosition + categoryParentFolder.Length + 2);
                        } while (nextDirectoryStart > 0);
                      

                    }
                }
            }
            else
            {
                _isValidCategory = false;
            }

        }

        public bool IsValidCategory
        {
            get { return _isValidCategory; }
        }

    }
}
