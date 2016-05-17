using System;
using System.IO;
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
  
        // Handles database related updates and reads for the File and closely related extension objects

        static SqlCommand _cmdReadFiles;
        static SqlCommand _cmdReadFileByID;
        static SqlCommand _cmdInsertFile;
        static SqlCommand _cmdReadFileLocations;
        static SqlCommand _cmdInsertFileLocations;
        static SqlCommand _cmdInsertDefaultRelativeLocations;
        static SqlCommand _cmdUpdateFileLocationVerified;
        static SqlCommand _cmdUpdateFileLocationAntiVerified;
        static SqlCommand _cmdUpdateFile;

        static SqlCommand _cmdReadFileTextNotes;
        static SqlCommand _cmdInsertFileTextNotes;

        public static void Load(ArtContentManager.Content.File File)
        {

            // This function assumes the ID is set within the File object and on that basis loads all the rest of the object properties

            SqlConnection DB = ArtContentManager.Static.Database.DB;

            if (_cmdReadFileByID == null)
            {
                string readFilesByID_SQL = "Select * from Files where FileID = @FileID";
                _cmdReadFileByID = new SqlCommand(readFilesByID_SQL, DB);
                _cmdReadFileByID.Parameters.Add("@FileID", System.Data.SqlDbType.Int);
            }

            _cmdReadFileByID.Parameters["@FileID"].Value = File.ID;

            _cmdReadFileByID.Transaction = ArtContentManager.Static.Database.ActiveTransaction;
            SqlDataReader reader = _cmdReadFiles.ExecuteReader();

            while (reader.Read())
            {
                File.Name = reader["FileName"].ToString();
                File.Extension = reader["Extension"].ToString();
                File.Size = (long)reader["Size"];
                File.RoleID = (int)reader["RoleID"];
                File.ParentID = (int)reader["ParentID"];
                File.ExtractUnreadable = (bool)reader["ExtractUnreadable"];
            }

            reader.Close();

        }



        public static bool IsFileRecorded(ArtContentManager.Content.File File)
        {
            SqlConnection DB = ArtContentManager.Static.Database.DB;

            // Set default return value and output parameters
            bool isFileRecorded = false;

            if (_cmdReadFiles == null)
            {
                string readFilesSQL = "Select * from Files where FileName = @FileName and Checksum = @Checksum";
                _cmdReadFiles = new SqlCommand(readFilesSQL, DB);
                _cmdReadFiles.Parameters.Add("@FileName", System.Data.SqlDbType.NVarChar, 255);
                _cmdReadFiles.Parameters.Add("@Checksum", System.Data.SqlDbType.NChar, 256);
            }

            _cmdReadFiles.Parameters["@FileName"].Value = File.Name;
            _cmdReadFiles.Parameters["@Checksum"].Value = File.Checksum;

            _cmdReadFiles.Transaction = ArtContentManager.Static.Database.ActiveTransaction;
            SqlDataReader reader = _cmdReadFiles.ExecuteReader();

            while (reader.Read())
            {
                if (isFileRecorded)
                {
                    Trace.WriteLine("Multiple file records found with a Checksum match for " + File.Name);
                }
                File.ID = (int)reader["FileID"];
                isFileRecorded = true;
            }

            reader.Close();
            return isFileRecorded;
        }

        public static void RecordFile(ArtContentManager.Content.File File)
        {
            SqlConnection DB = ArtContentManager.Static.Database.DB;

            if (_cmdInsertFile == null)
            {
                string insertFileSQL = "INSERT INTO Files (FileName, Extension, Checksum, Size, RoleID, ParentID, ExtractUnreadable) VALUES (@FileName, @Extension, @Checksum, @Size, @RoleID, @ParentID, @ExtractUnreadable) SET @FileID = SCOPE_IDENTITY();";
                _cmdInsertFile = new SqlCommand(insertFileSQL, DB);

                _cmdInsertFile.Parameters.Add("@FileName", System.Data.SqlDbType.NVarChar, 255);
                _cmdInsertFile.Parameters.Add("@Extension", System.Data.SqlDbType.NVarChar, 10);
                _cmdInsertFile.Parameters.Add("@Checksum", System.Data.SqlDbType.NChar, 256);
                _cmdInsertFile.Parameters.Add("@Size", System.Data.SqlDbType.Int);
                _cmdInsertFile.Parameters.Add("@RoleID", System.Data.SqlDbType.SmallInt);
                _cmdInsertFile.Parameters.Add("@ParentID", System.Data.SqlDbType.Int);
                _cmdInsertFile.Parameters.Add("@ExtractUnreadable", System.Data.SqlDbType.Bit);
                _cmdInsertFile.Parameters.Add("@FileID", System.Data.SqlDbType.Int).Direction = System.Data.ParameterDirection.Output;

            }

            _cmdInsertFile.Transaction = ArtContentManager.Static.Database.ActiveTransaction;
            _cmdInsertFile.Parameters["@FileName"].Value = File.Name; 
            _cmdInsertFile.Parameters["@Extension"].Value = File.Extension;
            _cmdInsertFile.Parameters["@Checksum"].Value = File.Checksum;
            _cmdInsertFile.Parameters["@Size"].Value = File.Size;
            _cmdInsertFile.Parameters["@RoleID"].Value = File.RoleID;
            _cmdInsertFile.Parameters["@ExtractUnreadable"].Value = File.ExtractUnreadable;
            _cmdInsertFile.Parameters["@ParentID"].Value = File.ParentID;

            _cmdInsertFile.ExecuteScalar();

            File.ID = (int)_cmdInsertFile.Parameters["@FileID"].Value;

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

            try
            {
                _cmdInsertDefaultRelativeLocations.ExecuteScalar();
            }
            catch(System.Data.SqlClient.SqlException e)
            {
                // Failure to insert due to a duplicate key is OK because some vendors put a readme file 
                // which is exactly the same in multiple zip files installing to the same location
            }

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

       public static void RecordFileTextNotes(ArtContentManager.Content.File File)
       {

            if (File.Extension != ".txt")
            {
                Trace.Write("Bypassing non-text file in document folder for text file save");
                return;
            }

            SqlConnection DB = ArtContentManager.Static.Database.DB;
            string textNotes;

            if (_cmdInsertFileTextNotes == null)
            {
                string insertFileTextNotesSQL = "INSERT INTO FileTextNotes (FileID, Text) VALUES (@FileID, @Text);";
                _cmdInsertFileTextNotes = new SqlCommand(insertFileTextNotesSQL, DB);
                _cmdInsertFileTextNotes.Parameters.Add("@FileID", System.Data.SqlDbType.Int);
                _cmdInsertFileTextNotes.Parameters.Add("@Text", System.Data.SqlDbType.NVarChar);
               
            }

            using (var streamReader = new StreamReader(File.ActivePathAndName, Encoding.UTF8))
            {
                textNotes = streamReader.ReadToEnd();
            }

            _cmdInsertFileTextNotes.Transaction = ArtContentManager.Static.Database.ActiveTransaction;
            _cmdInsertFileTextNotes.Parameters["@FileID"].Value = File.ID;
            _cmdInsertFileTextNotes.Parameters["@Text"].Value = textNotes;

            try
            {
                _cmdInsertFileTextNotes.ExecuteScalar();
            }
            catch(SqlException ex)
            {
                if (ex.Number == 2627)
                {
                    // Ignore duplicate inserts
                    // This can happen if exactly the same licence or read me file is stored in multiple zip files.
                }
            }
        }

       public static ArtContentManager.Content.FileTextData DeriveFileTextData(Content.File parentFile)
       {

            string textFileName;
            Content.File textFile;
            Content.FileTextData fileTextData = null;

            SqlConnection DB = ArtContentManager.Static.Database.DB;

            if (_cmdReadFileTextNotes == null)
            {
                string sqlReadFileTextNotes = "SELECT * from TextNotesForParent WHERE ParentID = @ParentFileID";
                _cmdReadFileByID = new SqlCommand(sqlReadFileTextNotes, DB);
                _cmdReadFileTextNotes.Parameters.Add("@ParentFileID", System.Data.SqlDbType.Int);
            }

            _cmdReadFileTextNotes.Transaction = ArtContentManager.Static.Database.ActiveTransaction;
            _cmdReadFileTextNotes.Parameters["@ParentFileID"].Value = parentFile.ID;

            SqlDataReader reader = _cmdReadFiles.ExecuteReader();

            while (reader.Read())
            {
                textFileName = reader["FileName"].ToString();

                if (textFileName.Contains("EULA") | textFileName.Contains("Licence"))
                {
                    // Ignore licence files
                }
                else
                {
                    textFile = new Content.File((int)reader["FileID"]);
                    fileTextData = ExtractTextData(textFile);
                }
            }

            reader.Close();

            return fileTextData;

       }

       public static ArtContentManager.Content.FileTextData ExtractTextData(ArtContentManager.Content.File File)
       {

            string textData = "";

            SqlConnection DB = ArtContentManager.Static.Database.DB;

            if (_cmdReadFileTextNotes == null)
            {
                string selectFileTextNotesSQL = "SELECT * FROM FileTextNotes WHERE @FileID = FileID";
                _cmdReadFileTextNotes = new SqlCommand(selectFileTextNotesSQL, DB);
                _cmdReadFileTextNotes.Parameters.Add("@FileID", System.Data.SqlDbType.Int);
            }

            _cmdReadFileTextNotes.Transaction = ArtContentManager.Static.Database.ActiveTransaction;
            _cmdReadFileTextNotes.Parameters["@FileID"].Value = File.ID;

            SqlDataReader reader = _cmdReadFileTextNotes.ExecuteReader();

            while (reader.Read())
            {
                textData = reader["Text"].ToString();
            }

            reader.Close();

            ArtContentManager.Content.FileTextData fileTextData = new Content.FileTextData();
            fileTextData.ParseText(textData);

            return fileTextData;

       }

       public static void UpdateFile(ArtContentManager.Content.File File)
       {
            SqlConnection DB = ArtContentManager.Static.Database.DB;

            if (_cmdUpdateFile == null)
            {
                string updateFilesSQL = " UPDATE Files Set FileName = @FileName, Extension = @Extension, Checksum = @Checksum, Size = @Size, RoleID = @RoleID, ParentID = @ParentID, ExtractUnreadable = @ExtractUnreadable " +
                                        " WHERE FileID = @FileID";
                _cmdUpdateFile = new SqlCommand(updateFilesSQL, DB);

                _cmdUpdateFile.Parameters.Add("@FileID", System.Data.SqlDbType.Int);
                _cmdUpdateFile.Parameters.Add("@FileName", System.Data.SqlDbType.NVarChar, 255);
                _cmdUpdateFile.Parameters.Add("@Extension", System.Data.SqlDbType.NVarChar, 10);
                _cmdUpdateFile.Parameters.Add("@Checksum", System.Data.SqlDbType.NChar, 256);
                _cmdUpdateFile.Parameters.Add("@Size", System.Data.SqlDbType.Int);
                _cmdUpdateFile.Parameters.Add("@RoleID", System.Data.SqlDbType.SmallInt);
                _cmdUpdateFile.Parameters.Add("@ParentID", System.Data.SqlDbType.Int);
                _cmdUpdateFile.Parameters.Add("@ExtractUnreadable", System.Data.SqlDbType.Bit);
                
            }

            _cmdUpdateFile.Transaction = ArtContentManager.Static.Database.ActiveTransaction;
            _cmdUpdateFile.Parameters["@FileID"].Value = File.ID;
            _cmdUpdateFile.Parameters["@FileName"].Value = File.Name;
            _cmdUpdateFile.Parameters["@Extension"].Value = File.Extension;
            _cmdUpdateFile.Parameters["@Checksum"].Value = File.Checksum;
            _cmdUpdateFile.Parameters["@Size"].Value = File.Size;
            _cmdUpdateFile.Parameters["@RoleID"].Value = File.RoleID;
            _cmdUpdateFile.Parameters["@ParentID"].Value = File.ParentID;
            _cmdUpdateFile.Parameters["@ExtractUnreadable"].Value = File.ExtractUnreadable;

            _cmdUpdateFile.ExecuteScalar();
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
