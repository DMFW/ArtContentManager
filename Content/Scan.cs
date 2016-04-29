using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtContentManager.Content
{
    internal class Scan
    {

        private string _folderRoot;
        private DateTime _startScanTime;
        private int _totalFileCount;
        private int _processedFileCount;

        public string FolderRoot
        {
            get { return _folderRoot; }
            set { _folderRoot = value; }
        }

        public DateTime StartScanTime
        {
            get { return _startScanTime; }
        }

        public int TotalFileCount
        {
            get { return _totalFileCount; }
            set { _totalFileCount = value; }
        }

        public int ProcessedFileCount
        {
            get { return _processedFileCount; }
            set { _processedFileCount = value; }
        }

    }
}
