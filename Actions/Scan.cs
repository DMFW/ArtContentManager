using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtContentManager.Actions
{
    internal class Scan
    {

        private string _folderName;
        private bool _isRequestRoot;
        private DateTime _startScanTime;
        private DateTime? _abortScanTime;
        private DateTime? _completeScanTime;
        private DateTime? _previousCompletedScanTime;
        private int _totalFiles;
        private int _newFiles;
        private int _processedFiles;

        public Scan()
        {
            _abortScanTime = null;
            _completeScanTime = null;
        }

        public string FolderName
        {
            get { return _folderName; }
            set { _folderName = value; }
        }

        public bool IsRequestRoot
        {
            get { return _isRequestRoot; }
            set { _isRequestRoot = value; }
        }

        public DateTime StartScanTime
        {
            get { return _startScanTime; }
            set { _startScanTime = value; }
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

        public DateTime? PreviousCompletedScanTime
        {
            get { return _previousCompletedScanTime; }
            set { _previousCompletedScanTime = value; }
        }

        public int TotalFiles
        {
            get { return _totalFiles; }
            set { _totalFiles = value; }
        }

        public int NewFiles
        {
            get { return _newFiles; }
            set { _newFiles = value; }
        }

        public int ProcessedFiles
        {
            get { return _processedFiles; }
            set { _processedFiles = value; }
        }

    }
}
