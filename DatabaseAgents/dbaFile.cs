using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Diagnostics;

namespace ArtContentManager.DatabaseAgents
{
    class dbaFile
    {

        SqlCommand cmdReadFiles; // Read all files by name and sum 
        SqlCommand cmdInsertFiles;
        SqlCommand cmdReadFileLocations;
        SqlCommand cmdInsertFileLocations;
        SqlCommand cmdUpdateFileLocationVerified;
        SqlCommand cmdUpdateFileLocationAntiVerified;

        SqlTransaction trnActive;

        public dbaFile()
        {

        }

        public void BeginTransaction()
        {
            trnActive = ArtContentManager.Static.Database.DB.BeginTransaction(System.Data.IsolationLevel.ReadUncommitted);
        }

        public void CommitTransaction()
        {
            trnActive.Commit();
            trnActive.Dispose();
        }

        public void RollbackTransaction()
        {
            trnActive.Rollback();
            trnActive.Dispose();
        }

        public bool FileRecorded(ArtContentManager.Content.File File)
        {

            // Set default return value and output parameters
            bool fileRecorded = false;

            if (cmdReadFiles == null)
            {
                string readFilesSQL = "Select * from Files where FileName = @FileName and Checksum = @Checksum";
                cmdReadFiles = new SqlCommand(readFilesSQL, Static.Database.DB);
                cmdReadFiles.Parameters.Add("@FileName", System.Data.SqlDbType.NVarChar, 255);
                cmdReadFiles.Parameters.Add("@Checksum", System.Data.SqlDbType.NChar, 256);
            }

            cmdReadFiles.Transaction = trnActive;
            cmdReadFiles.Parameters["@FileName"].Value = File.Name;
            cmdReadFiles.Parameters["@Checksum"].Value = File.Checksum;

            SqlDataReader reader = cmdReadFiles.ExecuteReader();

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

        public void RecordFile(ArtContentManager.Content.File File)
        {
            if (cmdInsertFiles == null)
            {
                string insertFilesSQL = "INSERT INTO Files (FileName, Extension, Checksum, Size, RoleID, ParentID) VALUES (@FileName, @Extension, @Checksum, @Size, @RoleID, @ParentID) SET @FileID = SCOPE_IDENTITY();";
                cmdInsertFiles = new SqlCommand(insertFilesSQL, Static.Database.DB);

                cmdInsertFiles.Parameters.Add("@FileName", System.Data.SqlDbType.NVarChar, 255);
                cmdInsertFiles.Parameters.Add("@Extension", System.Data.SqlDbType.NVarChar, 10);
                cmdInsertFiles.Parameters.Add("@Checksum", System.Data.SqlDbType.NChar, 256);
                cmdInsertFiles.Parameters.Add("@Size", System.Data.SqlDbType.Int);
                cmdInsertFiles.Parameters.Add("@RoleID", System.Data.SqlDbType.SmallInt);
                cmdInsertFiles.Parameters.Add("@ParentID", System.Data.SqlDbType.Int);
                cmdInsertFiles.Parameters.Add("@FileID", System.Data.SqlDbType.Int).Direction = System.Data.ParameterDirection.Output;

            }

            cmdInsertFiles.Transaction = trnActive;
            cmdInsertFiles.Parameters["@FileName"].Value = File.Name; 
            cmdInsertFiles.Parameters["@Extension"].Value = File.Extension;
            cmdInsertFiles.Parameters["@Checksum"].Value = File.Checksum;
            cmdInsertFiles.Parameters["@Size"].Value = File.Size;
            cmdInsertFiles.Parameters["@RoleID"].Value = File.RoleID;
            cmdInsertFiles.Parameters["@ParentID"].Value = File.ParentID;

            cmdInsertFiles.ExecuteScalar();

            File.ID = (int)cmdInsertFiles.Parameters["@FileID"].Value;

            if (File.ChildFiles != null)
            {
                foreach (ArtContentManager.Content.File childFile in File.ChildFiles)
                {
                    childFile.ParentID = File.ID;
                    RecordFile(childFile);
                }
            }

        }

        public void RecordFileInstance(ArtContentManager.Content.File File, DateTime scanDateTime)
        {
            // Set default return value and output parameters
            bool locationRecorded = false;

            if (cmdReadFileLocations == null)
            {
                string readFileLocationsSQL = "SELECT * FROM FileLocations WHERE FileID = @FileID AND Location = @Location";
                cmdReadFileLocations = new SqlCommand(readFileLocationsSQL, Static.Database.DB);
                cmdReadFileLocations.Parameters.Add("@FileID", System.Data.SqlDbType.Int);
                cmdReadFileLocations.Parameters.Add("@Location", System.Data.SqlDbType.NVarChar, 300);
            }

            cmdReadFileLocations.Transaction = trnActive;
            cmdReadFileLocations.Parameters["@FileID"].Value = File.ID;
            cmdReadFileLocations.Parameters["@Location"].Value = File.Location;

            SqlDataReader reader = cmdReadFileLocations.ExecuteReader();

            while (reader.Read())
            {
                locationRecorded = true; 
            }

            if (locationRecorded)
            {
                if (cmdUpdateFileLocationVerified == null)
                {
                    string updateFileLocationsSQL = "UPDATE FileLocations SET VerificationDate = @VerificationDate WHERE FileID = @FileID and Location = @Location";
                    cmdUpdateFileLocationVerified = new SqlCommand(updateFileLocationsSQL, Static.Database.DB);
                    cmdUpdateFileLocationVerified.Parameters.Add("@VerificationDate", System.Data.SqlDbType.DateTime);
                    cmdUpdateFileLocationVerified.Parameters.Add("@FileID", System.Data.SqlDbType.Int);
                    cmdUpdateFileLocationVerified.Parameters.Add("@Location", System.Data.SqlDbType.NVarChar, 300);
                }

                cmdUpdateFileLocationVerified.Transaction = trnActive;
                cmdUpdateFileLocationVerified.Parameters["@VerificationDate"].Value = scanDateTime;
                cmdUpdateFileLocationVerified.Parameters["@FileID"].Value = File.ID;
                cmdUpdateFileLocationVerified.Parameters["@Location"].Value = File.Location;
                cmdUpdateFileLocationVerified.ExecuteNonQuery();

            }
            else
            {
                if (cmdInsertFileLocations == null)
                {
                    string insertFileLocationsSQL = "INSERT INTO FileLocations (FileID, Location, VerificationDate) VALUES (@FileID, @Location, @VerificationDate)";
                    cmdInsertFileLocations = new SqlCommand(insertFileLocationsSQL, Static.Database.DB);
                    cmdInsertFileLocations.Parameters.Add("@FileID", System.Data.SqlDbType.Int);
                    cmdInsertFileLocations.Parameters.Add("@Location", System.Data.SqlDbType.NVarChar, 300);
                    cmdInsertFileLocations.Parameters.Add("@VerificationDate", System.Data.SqlDbType.DateTime);
                }

                cmdInsertFileLocations.Transaction = trnActive;
                cmdInsertFileLocations.Parameters["@FileID"].Value = File.ID;
                cmdInsertFileLocations.Parameters["@Location"].Value = File.Location;
                cmdInsertFileLocations.Parameters["@VerificationDate"].Value = scanDateTime;
                cmdInsertFileLocations.ExecuteNonQuery();
            }
            reader.Close();
       }

       public void UpdateAntiVerifiedFiles(string location, DateTime scanDateTime)
       {
           // After completing a directory scan, if a file is not verified it has implicitly failed as not found and so is "anti-verified".
           // Flag all files that were found previously in the directory but do not have the new verification date as anti-verified.

           if (cmdUpdateFileLocationAntiVerified == null)
           {
               string updateFileLocationsSQL = "UPDATE FileLocations SET AntiVerificationDate = @AntiVerificationDate WHERE (Location = @Location) AND (VerificationDate <> @VerificationDate)";
               cmdUpdateFileLocationAntiVerified = new SqlCommand(updateFileLocationsSQL, Static.Database.DB);
               cmdUpdateFileLocationAntiVerified.Parameters.Add("@AntiVerificationDate", System.Data.SqlDbType.DateTime);
               cmdUpdateFileLocationAntiVerified.Parameters.Add("@Location", System.Data.SqlDbType.NVarChar, 300);
               cmdUpdateFileLocationAntiVerified.Parameters.Add("@VerificationDate", System.Data.SqlDbType.DateTime);
           }

           cmdUpdateFileLocationAntiVerified.Transaction = trnActive;
           cmdUpdateFileLocationAntiVerified.Parameters["@AntiVerificationDate"].Value = scanDateTime;
           cmdUpdateFileLocationAntiVerified.Parameters["@Location"].Value = location;
           cmdUpdateFileLocationAntiVerified.Parameters["@VerificationDate"].Value = scanDateTime;
           cmdUpdateFileLocationAntiVerified.ExecuteNonQuery();

       }

    }
}
