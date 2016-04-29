using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtContentManager.Actions
{
    internal class Scan
    {

        private string _folderRoot;
        private DateTime _startScanTime;
        private DateTime? _previousCompletedScanTime;
        private int _totalFileCount;
        private int _processedFileCount;
        private DateTime? _abortScanTime;
        private DateTime? _completeScanTime;

        public Scan()
        {
            _abortScanTime = null;
            _completeScanTime = null;
        }

        public string FolderRoot
        {
            get { return _folderRoot; }
            set { _folderRoot = value; }
        }

        public DateTime StartScanTime
        {
            get { return _startScanTime; }
            set { _startScanTime = value; }
        }

        public DateTime? PreviousCompletedScanTime
        {
            get { return _previousCompletedScanTime; }
            set { _previousCompletedScanTime = value; }
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

        public DateTime? AbortScanTime
        {
            get { return _abortScanTime; }
            set { _abortScanTime = value; }
        }

        public DateTime? CompleteScanTime
        {
            get { return _completeScanTime; }
            set { _completeScanTime = value; }
        }

    }
}
