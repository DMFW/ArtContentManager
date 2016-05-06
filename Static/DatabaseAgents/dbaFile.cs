using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Diagnostics;

namespace ArtContentManager.Static.DatabaseAgents
{
    static class dbaFile
    {
  
        static SqlCommand _cmdReadFiles;
        static SqlCommand _cmdInsertFiles;
        static SqlCommand _cmdReadFileLocations;
        static SqlCommand _cmdInsertFileLocations;
        static SqlCommand _cmdReadDefaultRelativeFileLocations;
        static SqlCommand _cmdInsertDefaultRelativeLocations;
        static SqlCommand _cmdUpdateFileLocationVerified;
        static SqlCommand _cmdUpdateFileLocationAntiVerified;

        public static bool FileRecorded(ArtContentManager.Content.File File)
        {
            SqlConnection DB = ArtContentManager.Static.Database.DB;

            // Set default return value and output parameters
            bool fileRecorded = false;

            if (_cmdReadFiles == null)
            {
                string readFilesSQL = "Select * from Files where FileName = @FileName and Checksum = @Checksum";
                _cmdReadFiles = new SqlCommand(readFilesSQL, DB);
                _cmdReadFiles.Parameters.Add("@FileName", System.Data.SqlDbType.NVarChar, 255);
                _cmdReadFiles.Parameters.Add("@Checksum", System.Data.SqlDbType.NChar, 256);
            }

            _cmdReadFiles.Transaction = ArtContentManager.Static.Database.ActiveTransaction;
            _cmdReadFiles.Parameters["@FileName"].Value = File.Name;
            _cmdReadFiles.Parameters["@Checksum"].Value = File.Checksum;

            SqlDataReader reader = _cmdReadFiles.ExecuteReader();

            while (reader.Read())
            {
                if (fileRecorded)
                {
                    Trace.WriteLine("Multiple file records found with a Checksum match for " + File.Name);
                }
                File.ID = (int)reader["FileID"];
                fileRecorded = true;
            }

            reader.Close();
            return fileRecorded;
        }

        public static void RecordFile(ArtContentManager.Content.File File)
        {
            SqlConnection DB = ArtContentManager.Static.Database.DB;

            if (_cmdInsertFiles == null)
            {
                string insertFilesSQL = "INSERT INTO Files (FileName, Extension, Checksum, Size, RoleID, ParentID) VALUES (@FileName, @Extension, @Checksum, @Size, @RoleID, @ParentID) SET @FileID = SCOPE_IDENTITY();";
                _cmdInsertFiles = new SqlCommand(insertFilesSQL, DB);

                _cmdInsertFiles.Parameters.Add("@FileName", System.Data.SqlDbType.NVarChar, 255);
                _cmdInsertFiles.Parameters.Add("@Extension", System.Data.SqlDbType.NVarChar, 10);
                _cmdInsertFiles.Parameters.Add("@Checksum", System.Data.SqlDbType.NChar, 256);
                _cmdInsertFiles.Parameters.Add("@Size", System.Data.SqlDbType.Int);
                _cmdInsertFiles.Parameters.Add("@RoleID", System.Data.SqlDbType.SmallInt);
                _cmdInsertFiles.Parameters.Add("@ParentID", System.Data.SqlDbType.Int);
                _cmdInsertFiles.Parameters.Add("@FileID", System.Data.SqlDbType.Int).Direction = System.Data.ParameterDirection.Output;

            }

            _cmdInsertFiles.Transaction = ArtContentManager.Static.Database.ActiveTransaction;
            _cmdInsertFiles.Parameters["@FileName"].Value = File.Name; 
            _cmdInsertFiles.Parameters["@Extension"].Value = File.Extension;
            _cmdInsertFiles.Parameters["@Checksum"].Value = File.Checksum;
            _cmdInsertFiles.Parameters["@Size"].Value = File.Size;
            _cmdInsertFiles.Parameters["@RoleID"].Value = File.RoleID;
            _cmdInsertFiles.Parameters["@ParentID"].Value = File.ParentID;

            _cmdInsertFiles.ExecuteScalar();

            File.ID = (int)_cmdInsertFiles.Parameters["@FileID"].Value;

            if (File.ChildFiles != null)
            {
                foreach (ArtContentManager.Content.File childFile in File.ChildFiles)
                {
                    childFile.ParentID = File.ID;
                    RecordFile(childFile);
                }
            }

        }


        public static void RecordFileLocation(ArtContentManager.Content.File File, DateTime scanDateTime)
        {

            // File locations behave differently if we are working with a child file.
            // For child files we don't care about the absolute location (instance) because it is just in a scratch area for zip content
            // but we do care about the relative location. We should not record an absolute location but we should write the 
            // relative one away.

            if (File.ParentID != 0)
            {
                RecordDefaultRelativeLocation(File);
            }
            else
            {
                RecordFileInstance(File, scanDateTime);
            }

        }

        private static void RecordDefaultRelativeLocation(ArtContentManager.Content.File File)
        {

            SqlConnection DB = ArtContentManager.Static.Database.DB;

            // Assume we need to record (or re-record) the relative location

            if (_cmdInsertDefaultRelativeLocations == null)
            {
                string insertDefaultRelativeLocationsSQL = "INSERT INTO DefaultRelativeLocations (FileID, RelativeLocation) VALUES (@FileID, @RelativeLocation);";
                _cmdInsertDefaultRelativeLocations = new SqlCommand(insertDefaultRelativeLocationsSQL, DB);

                _cmdInsertDefaultRelativeLocations.Parameters.Add("@FileID", System.Data.SqlDbType.Int);
                _cmdInsertDefaultRelativeLocations.Parameters.Add("@RelativeLocation", System.Data.SqlDbType.NVarChar, 255);              
            }

            _cmdInsertDefaultRelativeLocations.Transaction = ArtContentManager.Static.Database.ActiveTransaction;
            _cmdInsertDefaultRelativeLocations.Parameters["@FileID"].Value = File.ID;
            _cmdInsertDefaultRelativeLocations.Parameters["@RelativeLocation"].Value = File.RelativeInstallationPath;

            _cmdInsertDefaultRelativeLocations.ExecuteScalar();

        }

        private static void RecordFileInstance(ArtContentManager.Content.File File, DateTime scanDateTime)
        {

            SqlConnection DB = ArtContentManager.Static.Database.DB;

            

            // Set default return value and output parameters
            bool locationRecorded = false;



            if (_cmdReadFileLocations == null)
            {
                string readFileLocationsSQL = "SELECT * FROM FileLocations WHERE FileID = @FileID AND Location = @Location";
                _cmdReadFileLocations = new SqlCommand(readFileLocationsSQL, DB);
                _cmdReadFileLocations.Parameters.Add("@FileID", System.Data.SqlDbType.Int);
                _cmdReadFileLocations.Parameters.Add("@Location", System.Data.SqlDbType.NVarChar, 300);
            }   

            _cmdReadFileLocations.Transaction = ArtContentManager.Static.Database.ActiveTransaction;
            _cmdReadFileLocations.Parameters["@FileID"].Value = File.ID;
            _cmdReadFileLocations.Parameters["@Location"].Value = File.Location;

            SqlDataReader reader = _cmdReadFileLocations.ExecuteReader();

            while (reader.Read())
            {
                locationRecorded = true; 
            }

            if (locationRecorded)
            {
                if (_cmdUpdateFileLocationVerified == null)
                {
                    string updateFileLocationsSQL = "UPDATE FileLocations SET VerificationDate = @VerificationDate WHERE FileID = @FileID and Location = @Location";
                    _cmdUpdateFileLocationVerified = new SqlCommand(updateFileLocationsSQL, DB);
                    _cmdUpdateFileLocationVerified.Parameters.Add("@VerificationDate", System.Data.SqlDbType.DateTime);
                    _cmdUpdateFileLocationVerified.Parameters.Add("@FileID", System.Data.SqlDbType.Int);
                    _cmdUpdateFileLocationVerified.Parameters.Add("@Location", System.Data.SqlDbType.NVarChar, 300);
                }

                _cmdUpdateFileLocationVerified.Transaction = ArtContentManager.Static.Database.ActiveTransaction;
                _cmdUpdateFileLocationVerified.Parameters["@VerificationDate"].Value = scanDateTime;
                _cmdUpdateFileLocationVerified.Parameters["@FileID"].Value = File.ID;
                _cmdUpdateFileLocationVerified.Parameters["@Location"].Value = File.Location;
                _cmdUpdateFileLocationVerified.ExecuteNonQuery();

            }
            else
            {
                if (_cmdInsertFileLocations == null)
                {
                    string insertFileLocationsSQL = "INSERT INTO FileLocations (FileID, Location, VerificationDate) VALUES (@FileID, @Location, @VerificationDate)";
                    _cmdInsertFileLocations = new SqlCommand(insertFileLocationsSQL, DB);
                    _cmdInsertFileLocations.Parameters.Add("@FileID", System.Data.SqlDbType.Int);
                    _cmdInsertFileLocations.Parameters.Add("@Location", System.Data.SqlDbType.NVarChar, 300);
                    _cmdInsertFileLocations.Parameters.Add("@VerificationDate", System.Data.SqlDbType.DateTime);
                }

                _cmdInsertFileLocations.Transaction = ArtContentManager.Static.Database.ActiveTransaction;
                _cmdInsertFileLocations.Parameters["@FileID"].Value = File.ID;
                _cmdInsertFileLocations.Parameters["@Location"].Value = File.Location;
                _cmdInsertFileLocations.Parameters["@VerificationDate"].Value = scanDateTime;
                _cmdInsertFileLocations.ExecuteNonQuery();
            }
            reader.Close();
       }

       public static void UpdateAntiVerifiedFiles(string location, DateTime scanDateTime)
       {
            // After completing a directory scan, if a file is not verified it has implicitly failed as not found and so is "anti-verified".
            // Flag all files that were found previously in the directory but do not have the new verification date as anti-verified.

           SqlConnection DB = ArtContentManager.Static.Database.DB;

           if (_cmdUpdateFileLocationAntiVerified == null)
           {
               string updateFileLocationsSQL = "UPDATE FileLocations SET AntiVerificationDate = @AntiVerificationDate WHERE (Location = @Location) AND (VerificationDate <> @VerificationDate)";
               _cmdUpdateFileLocationAntiVerified = new SqlCommand(updateFileLocationsSQL, DB);
               _cmdUpdateFileLocationAntiVerified.Parameters.Add("@AntiVerificationDate", System.Data.SqlDbType.DateTime);
               _cmdUpdateFileLocationAntiVerified.Parameters.Add("@Location", System.Data.SqlDbType.NVarChar, 300);
               _cmdUpdateFileLocationAntiVerified.Parameters.Add("@VerificationDate", System.Data.SqlDbType.DateTime);
           }

           _cmdUpdateFileLocationAntiVerified.Transaction = ArtContentManager.Static.Database.ActiveTransaction;
           _cmdUpdateFileLocationAntiVerified.Parameters["@AntiVerificationDate"].Value = scanDateTime;
           _cmdUpdateFileLocationAntiVerified.Parameters["@Location"].Value = location;
           _cmdUpdateFileLocationAntiVerified.Parameters["@VerificationDate"].Value = scanDateTime;
           _cmdUpdateFileLocationAntiVerified.ExecuteNonQuery();

       }

    }
}
