using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Security.Cryptography;
using System.IO;
using System.IO.Compression;
using System.Data.SqlClient;

namespace ArtContentManager.Content
{
    class File
    {

        private DateTime _ScanDateTime;
        private File _parentFile;
        private int _ParentID;

        private string _Name;
        private string _ActivePathAndName;
        private string _Extension;
        private string _Location;
        private int _RoleID;
        private long _Size;
        private string _Checksum = "";
        private int _ID;
        private List<File> _ChildFiles;

        private string _RelativeInstallationPath;
        private string _WorkingExtractDirectory;
        private bool _ExtractUnreadable;

        public File(DateTime scanDateTime, File parentFile, FileInfo fi)
        {

            // The constructor, both for a primary file found directly in a scan and a secondary file embedded in a zip or manifest
            // A "primary" file can be distinguished by having a parent ID of zero.

            _ScanDateTime = scanDateTime;
            _parentFile = parentFile;
            _ParentID = _parentFile == null ? 0 : parentFile.ID;
            _ActivePathAndName = fi.FullName;
            _Name = fi.Name;
            _Extension = Path.GetExtension(fi.FullName);
            _Location = Path.GetDirectoryName(fi.FullName);
            _Size = fi.Length;
            _ExtractUnreadable = false;

            if (parentFile != null)
            {
                _RelativeInstallationPath = _ActivePathAndName.Substring(parentFile.WorkingExtractDirectory.Length + 1);
            }
            else
            {
                _RelativeInstallationPath = "";
            }

            // Make a provisional stab at deriving a role ID
            _RoleID = DeriveProvisionalRole();

            // Must save before analysing content so we have an ID to use as a parent to any children.
            bool wasAlreadyRecorded;
            Save(scanDateTime, out wasAlreadyRecorded);

            // The first time we encounter a zip file we need to analyse it "deeply", but thereafter we only care about noting its current location,
            // which the above save will have done even when wasAlreadyRecorded is true.

            if (!wasAlreadyRecorded)
            {
                if (_Extension == ".zip")
                {
                    ArtContentManager.Static.FileSystemScan.InternalZipInstance++;
                    ExtractZipContent();
                }
            }

        }

        public File(int fileID)
        {
            // The constructor for known files.

            _ID = fileID;

            ArtContentManager.Static.DatabaseAgents.dbaFile.Load(this);

        }

        public string ActivePathAndName
        {
            get { return _ActivePathAndName; }
            set { _ActivePathAndName = value; }
        }

        public string RelativeInstallationPath
        {
            get { return _RelativeInstallationPath; }
            set { _RelativeInstallationPath = value; }
        }

        public string WorkingExtractDirectory
        {
            get { return _WorkingExtractDirectory; }
            set { _WorkingExtractDirectory = value; }
        }

        public string Name
        {
            get { return _Name; }
            set { _Name = value; }
        }

        public string Extension
        {
            get { return _Extension; }
            set { _Extension = value; }
        }

        public string Location
        {
            get { return _Location; }
            set { _Location = value; }
        }

        public int RoleID
        {
            get { return _RoleID; }
            set { _RoleID = value; }
        }

        public long Size
        {
            get { return _Size; }
            set { _Size = value; }
        }

        public int ID
        {
            get { return _ID; }
            set { _ID = value; }
        }

        public int ParentID
        {
            get { return _ParentID; }
            set { _ParentID = value; }
        }

        public bool ExtractUnreadable
        {
            get { return _ExtractUnreadable; }
            set { _ExtractUnreadable = value; }
        }

        public List<File> ChildFiles
        {
            get { return _ChildFiles;  }
        }

        public string Checksum
        {
            get
            {
                if (_Checksum == "")
                {
                    _Checksum = GetChecksum(_ActivePathAndName);
                }
                return _Checksum;
            }
        }

        private string GetChecksum(string file)
        {

            if (file == null) { return ""; }

            if (file == "") { return ""; }

            using (FileStream stream = System.IO.File.OpenRead(file))
            {
                var sha = new SHA256Managed();
                byte[] checksum = sha.ComputeHash(stream);
                return BitConverter.ToString(checksum).Replace("-", String.Empty);
            }
        }

        private int DeriveProvisionalRole()
        {

            // Have a crack at assigning a file role. You never know, it may even be right?

            int roleID = 0; // default to undefined. This is even worse than unknown... 

            if (Static.Database.ProcessRoles.ContainsKey("Unknown"))
            {
                // Start with unknown if "unknown" is in the database and why wouldn't it be?
                roleID = (int)Static.Database.ProcessRoles["Unknown"]; 
            }

            if (Static.Database.ReservedFiles.ContainsKey(_Name.ToLower()))
            {
                // If we are a known file name with a specific role then set it.
                roleID = Static.Database.ReservedFiles[_Name.ToLower()];
            }
            else
            {
                // Make a guess based on extension mapping. 
                // This will be spot on in many cases but ambiguous in some.
                // We will refine the ambiguous guesses later...

                if (Static.Database.ProcessRoleExtensionsPrimary.ContainsKey(_Extension.ToLower()))
                {
                    roleID = (int)Static.Database.ProcessRoleExtensionsPrimary[_Extension.ToLower()];
                }
            }

            return roleID;
        }

        private void ExtractZipContent()
        {

            _WorkingExtractDirectory = Static.DatabaseAgents.dbaSettings.Setting("WorkFolder").Item1 + @"\ZipFile" + ArtContentManager.Static.FileSystemScan.InternalZipInstance;
            _ChildFiles = new List<File>();

            if (ArtContentManager.Static.FileSystemScan.InternalZipInstance == 1)
            {
                // This is the first zip file within the chain so clean the zip working extract area
                Debug.Assert(_ParentID == 0);  // The parentID should be zero when the internal zip instance is one
                CleanZipWorkingRootDirectory();
            }

            try
            {
                ZipFile.ExtractToDirectory(_ActivePathAndName, _WorkingExtractDirectory);
                WalkDirectoryTree(new DirectoryInfo(_WorkingExtractDirectory));
            }
            catch(System.IO.IOException e)
            {
                _ExtractUnreadable = true;
                Update();
            }
        }

        private void CleanZipWorkingRootDirectory()
        {
            System.IO.DirectoryInfo di = new DirectoryInfo(Static.DatabaseAgents.dbaSettings.Setting("WorkFolder").Item1);

            foreach (FileInfo file in di.GetFiles())
            {
                file.Delete();
            }
            foreach (DirectoryInfo dir in di.GetDirectories())
            {
                dir.Delete(true);
            }
        }

        private void WalkDirectoryTree(System.IO.DirectoryInfo root)
        {
            System.IO.FileInfo[] files = null;
            System.IO.DirectoryInfo[] subDirs = null;

            // First, process all the files directly under this folder
            try
            {
                files = root.GetFiles("*.*");
            }
            // This is thrown if even one of the files requires permissions greater
            // than the application provides.
            catch (UnauthorizedAccessException e)
            {
                // This code just writes out the message and continues to recurse.
                // You may decide to do something different here. For example, you
                // can try to elevate your privileges and access the file again.
                Trace.WriteLine(e.Message);
            }

            catch (System.IO.DirectoryNotFoundException e)
            {
                Trace.WriteLine(e.Message);
            }

            if (files != null)
            {
                foreach (System.IO.FileInfo fi in files)
                {
                    // In this example, we only access the existing FileInfo object. If we
                    // want to open, delete or modify the file, then
                    // a try-catch block is required here to handle the case
                    // where the file has been deleted since the call to TraverseTree().
                    File subFile = new File(_ScanDateTime, this, fi);
                    Trace.WriteLine(fi.FullName);
                }

                // Now find all the subdirectories under this directory.
                subDirs = root.GetDirectories();

                foreach (System.IO.DirectoryInfo dirInfo in subDirs)
                {
                    // Resursive call for each subdirectory.
                    WalkDirectoryTree(dirInfo);
                }
            }
        }

        private void Save(DateTime scanDateTime, out bool wasAlreadyRecorded)
        {

            // Adds new files and also inserts or updates their location instance(s)
            // After calling this method we will have an ID for the file (either an existing known one or a new one)

            ArtContentManager.Static.Database.BeginTransaction();

            if (ArtContentManager.Static.DatabaseAgents.dbaFile.IsFileRecorded(this))
            {
                wasAlreadyRecorded = true;
            }
            else
            {
                ArtContentManager.Static.DatabaseAgents.dbaFile.RecordFile(this);
                wasAlreadyRecorded = false;
            }

            ArtContentManager.Static.DatabaseAgents.dbaFile.RecordFileLocation(this, scanDateTime);

            if (this.Location.Contains("Documentation") & this._ParentID != 0)
            {
                // We are a child file (i.e. within a zip or some such) and we are in a document location
                // Attempt to pull the document into the database.
                ArtContentManager.Static.DatabaseAgents.dbaFile.RecordFileTextNotes(this);
            }

            ArtContentManager.Static.Database.CommitTransaction();

        }

        private void Update()
        {
            ArtContentManager.Static.Database.BeginTransaction();
            ArtContentManager.Static.DatabaseAgents.dbaFile.UpdateFile(this);
            ArtContentManager.Static.Database.CommitTransaction();
        }
    }
}
