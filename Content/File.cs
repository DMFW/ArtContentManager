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
        private string _Name;
        private string _ActivePathAndName;
        private string _Extension;
        private string _Location;
        private int _RoleID;
        private long _Size;
        private string _Checksum = "";
        private int _ID;
        private List<File> _ChildFiles;

        private int _ParentID;
        private string _RelativeInstallationPath;

        public File(FileInfo fi, string activePathAndName)
        {

            // The constructor for a primary file found directly in a scan

            _ActivePathAndName = activePathAndName;
            _Name = Path.GetFileName(activePathAndName);
            _Extension = Path.GetExtension(activePathAndName);
            _Location = Path.GetDirectoryName(activePathAndName);
            _Size = fi.Length;

            // Make a provisional stab at deriving a role ID
            _RoleID = DeriveProvisionalRole();
            
            if (_Extension == ".zip")
            {
                DeriveZipProperties();
            }

        }

        public File(string name, string relativeInstallationPath, long size)
        {

            // The constructor for a secondary file within a zip file
            _Name = name;
            _RelativeInstallationPath = relativeInstallationPath;
            _Size = size;

            _Extension = Path.GetExtension(_Name);
            _RoleID = DeriveProvisionalRole();

            // This would be a zip within a zip but this is possible...
            if (_Extension == ".zip")
            {
                DeriveZipProperties();
            }

        }

        public string ActivePathAndName
        {
            get { return _ActivePathAndName; }
        }

        public string Name
        {
            get { return _Name; }
        }

        public string Extension
        {
            get { return _Extension; }
        }

        public string Location
        {
            get { return _Location; }
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

            if (Static.Database.FileRoles.ContainsKey("Unknown"))
            {
                // Start with unknown if "unknown" is in the database and why wouldn't it be?
                roleID = (int)Static.Database.FileRoles["Unknown"]; 
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

                if (Static.Database.FileRoleExtensionsPrimary.ContainsKey(_Extension.ToLower()))
                {
                    roleID = (int)Static.Database.FileRoleExtensionsPrimary[_Extension.ToLower()];
                }
            }

            return roleID;
        }

        private void DeriveZipProperties()
        {

            _ChildFiles = new List<File>();

            using (ZipArchive archive = ZipFile.OpenRead(_ActivePathAndName))
            {
                foreach (ZipArchiveEntry entry in archive.Entries)
                {
                    File childFile = new File(entry.Name, entry.FullName, entry.Length);
                    _ChildFiles.Add(childFile);
                }
            }
        }

        public void Save(ArtContentManager.DatabaseAgents.dbaFile dbaFile, DateTime scanDateTime)
        {

            // Adds new files and also inserts or updates their location instance(s)

            dbaFile.BeginTransaction();

            if (!dbaFile.FileRecorded(this))
            {
                dbaFile.RecordFile(this);
            }

            dbaFile.RecordFileInstance(this, scanDateTime);

            dbaFile.CommitTransaction();

        }
    }
}
