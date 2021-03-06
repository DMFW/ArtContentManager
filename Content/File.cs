﻿using System;
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
    public class File
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
        private string _StoredChecksum = "";
        private string _CalculatedChecksum = "";
        private int _ID;
        private List<File> _ChildFiles;

        private string _DefaultRelativeInstallationPath;
        private string _WorkingExtractDirectory;
        private bool _ExtractUnreadable;
        private bool _reAnalyseZipFiles;

        private string _Text; 

        public File(DateTime scanDateTime, File parentFile, FileInfo fi, bool reAnalyseZipFiles)
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
            _Text = string.Empty;

            if (parentFile != null)
            {
                _DefaultRelativeInstallationPath = fi.DirectoryName.Substring(parentFile.WorkingExtractDirectory.Length + 1);
            }
            else
            {
                _DefaultRelativeInstallationPath = "";
            }

            // Make a provisional stab at deriving a role ID
            _RoleID = DeriveProvisionalRole();

            // Must save before analysing content so we have an ID to use as a parent to any children.
            bool wasAlreadyRecorded;
            Save(scanDateTime, out wasAlreadyRecorded);

            // The first time we encounter a zip file we need to analyse it "deeply".
            // We must also analyse it deeply if instructed to do so by the user. 
            // Otherwise we only care about noting its current location,
            // which the above save will have done even when wasAlreadyRecorded is true.

            if ((!wasAlreadyRecorded) | (reAnalyseZipFiles))
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

        public File ParentFile
        {
            get { return _parentFile; }
            set { _parentFile = value; }
        }

        public string ActivePathAndName
        {
            get { return _ActivePathAndName; }
            set { _ActivePathAndName = value; }
        }

        public string DefaultRelativeInstallationPath
        {
            get { return _DefaultRelativeInstallationPath; }
            set { _DefaultRelativeInstallationPath = value; }
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

        public string Text
        {
            get { return _Text; }
            set { _Text = value; }
        }

        public List<File> ChildFiles
        {
            get { return _ChildFiles;  }
        }
 
        public string StoredChecksum
        {
            get { return _StoredChecksum; }
            set { _StoredChecksum = value; }
        }

        public string CalculatedChecksum
        {
            get
            {
                if (_CalculatedChecksum == "")
                {
                    _CalculatedChecksum = CalculateChecksum(_ActivePathAndName);
                }
                return _CalculatedChecksum;
            }
        }

        private string CalculateChecksum(string file)
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
            catch(System.IO.InvalidDataException)
            {
                _ExtractUnreadable = true;
                Update();
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
                    File subFile = new File(_ScanDateTime, this, fi, true);
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

            // Adds new files and also inserts or updates their location instance(s) and imports text data
            // After calling this method we will have an ID for the file (either an existing known one or a new one)

            ArtContentManager.Static.Database.BeginTransaction(Static.Database.TransactionType.Active);

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

            if ((_ParentID != 0) & (_Extension == ".txt"))
            {
               ArtContentManager.Static.DatabaseAgents.dbaFile.RecordFileTextNotes(this);   
            }

            ArtContentManager.Static.Database.CommitTransaction(Static.Database.TransactionType.Active);

        }

        private void Update()
        {
            ArtContentManager.Static.Database.BeginTransaction(Static.Database.TransactionType.Active);
            ArtContentManager.Static.DatabaseAgents.dbaFile.UpdateFile(this);
            ArtContentManager.Static.Database.CommitTransaction(Static.Database.TransactionType.Active);
        }
    }
}
