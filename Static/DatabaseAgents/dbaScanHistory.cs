using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Diagnostics;

namespace ArtContentManager.Static.DatabaseAgents
{
    static class dbaScanHistory
    {

        static SqlCommand _cmdInsertScan;
        static SqlCommand _cmdSelectScan;
        static SqlCommand _cmdUpdateScanInitialFileCounts;
        static SqlCommand _cmdUpdateScanFilesProcessed;
        static SqlCommand _cmdRecordScanAbort;
        static SqlCommand _cmdRecordScanComplete;
        static SqlCommand _cmdUpdateAll;

        public static void RecordStartScan(Actions.Scan scan)
        {
            SqlConnection DB = ArtContentManager.Static.Database.DB;

            if (_cmdInsertScan == null)
            {
                string sqlInsertScan = "INSERT INTO ScanHistory (FolderName, IsRequestRoot, Started, TotalFiles, NewFiles) Values(@FolderName, @IsRequestRoot,  @Started, @TotalFiles, @NewFiles)";

                _cmdInsertScan = new SqlCommand(sqlInsertScan, DB);
                _cmdInsertScan.Parameters.Add("@FolderName", System.Data.SqlDbType.NVarChar, 255);
                _cmdInsertScan.Parameters.Add("@IsRequestRoot", System.Data.SqlDbType.Bit);
                _cmdInsertScan.Parameters.Add("@Started", System.Data.SqlDbType.DateTime);
                _cmdInsertScan.Parameters.Add("@TotalFiles", System.Data.SqlDbType.Int);
                _cmdInsertScan.Parameters.Add("@NewFiles", System.Data.SqlDbType.Int);
            }

            _cmdInsertScan.Parameters["@FolderName"].Value = scan.FolderName;
            _cmdInsertScan.Parameters["@IsRequestRoot"].Value = scan.IsRequestRoot;
            _cmdInsertScan.Parameters["@Started"].Value = scan.StartScanTime;
            _cmdInsertScan.Parameters["@TotalFiles"].Value = scan.TotalFiles;
            _cmdInsertScan.Parameters["@NewFiles"].Value = scan.NewFiles;

            ArtContentManager.Static.Database.BeginTransaction();
            _cmdInsertScan.Transaction = ArtContentManager.Static.Database.ActiveTransaction;
            _cmdInsertScan.ExecuteScalar();
            ArtContentManager.Static.Database.CommitTransaction();
        }

        public static void SetLastCompletedScanTime(Actions.Scan scan)
        {

            SqlConnection DB = ArtContentManager.Static.Database.DB;
            scan.PreviousCompletedScanTime = DateTime.MinValue; // Assume no prior successful scan

            if (_cmdSelectScan == null)
            {
                string sqlSelectScan = "SELECT TOP 1 Completed FROM ScanHistory WHERE FolderName = @FolderName ORDER BY Completed DESC";
                _cmdSelectScan = new SqlCommand(sqlSelectScan, DB);
                _cmdSelectScan.Parameters.Add("@FolderName", System.Data.SqlDbType.NVarChar, 255);
               
            }

            _cmdSelectScan.Parameters["@FolderName"].Value = scan.FolderName;
            SqlDataReader rdrScanHistory = _cmdSelectScan.ExecuteReader();

            if (rdrScanHistory.HasRows)
            {
                while (rdrScanHistory.Read())
                {
                    if (String.IsNullOrEmpty(rdrScanHistory["Completed"].ToString()))
                    { scan.PreviousCompletedScanTime = DateTime.MinValue; }
                    else
                    { scan.PreviousCompletedScanTime = (DateTime)rdrScanHistory["Completed"]; }
                }
            }
            else
            {
                scan.PreviousCompletedScanTime = DateTime.MinValue;
            }
            rdrScanHistory.Close();
        }

        public static void UpdateInitialFileCounts(Actions.Scan scan)
        {
            SqlConnection DB = ArtContentManager.Static.Database.DB;

            if (_cmdUpdateScanInitialFileCounts == null)
            {
                string sqlUpdateScan = "UPDATE ScanHistory Set TotalFiles = @TotalFiles, NewFiles = @NewFiles WHERE FolderName = @FolderName AND Started = @Started";

                _cmdUpdateScanInitialFileCounts = new SqlCommand(sqlUpdateScan, DB);
                _cmdUpdateScanInitialFileCounts.Parameters.Add("@FolderName", System.Data.SqlDbType.NVarChar, 255);
                _cmdUpdateScanInitialFileCounts.Parameters.Add("@Started", System.Data.SqlDbType.DateTime);
                _cmdUpdateScanInitialFileCounts.Parameters.Add("@TotalFiles", System.Data.SqlDbType.Int);
                _cmdUpdateScanInitialFileCounts.Parameters.Add("@NewFiles", System.Data.SqlDbType.Int);
            }



            _cmdUpdateScanInitialFileCounts.Parameters["@FolderName"].Value = scan.FolderName;
            _cmdUpdateScanInitialFileCounts.Parameters["@Started"].Value = scan.StartScanTime;
            _cmdUpdateScanInitialFileCounts.Parameters["@TotalFiles"].Value = scan.TotalFiles;
            _cmdUpdateScanInitialFileCounts.Parameters["@NewFiles"].Value = scan.NewFiles;

            ArtContentManager.Static.Database.BeginTransaction();
            _cmdUpdateScanInitialFileCounts.Transaction = ArtContentManager.Static.Database.ActiveTransaction;
            _cmdUpdateScanInitialFileCounts.ExecuteScalar();
            ArtContentManager.Static.Database.CommitTransaction();
        }

        public static void UpdateFilesProcessed(Actions.Scan scan)
        {
            SqlConnection DB = ArtContentManager.Static.Database.DB;

            if (_cmdUpdateScanFilesProcessed == null)
            {
                string sqlUpdateScan = "UPDATE ScanHistory Set ProcessedFiles = @ProcessedFiles WHERE FolderName = @FolderName AND Started = @Started";
                _cmdUpdateScanFilesProcessed = new SqlCommand(sqlUpdateScan, DB);
                _cmdUpdateScanFilesProcessed.Parameters.Add("@FolderName", System.Data.SqlDbType.NVarChar, 255);
                _cmdUpdateScanFilesProcessed.Parameters.Add("@Started", System.Data.SqlDbType.DateTime);
                _cmdUpdateScanFilesProcessed.Parameters.Add("@ProcessedFiles", System.Data.SqlDbType.Int);

            }

            _cmdUpdateScanFilesProcessed.Parameters["@FolderRoot"].Value = scan.FolderName;
            _cmdUpdateScanFilesProcessed.Parameters["@Started"].Value = scan.StartScanTime;
            _cmdUpdateScanFilesProcessed.Parameters["@FilesProcessed"].Value = scan.ProcessedFiles;

            ArtContentManager.Static.Database.BeginTransaction();
            _cmdUpdateScanFilesProcessed.Transaction = ArtContentManager.Static.Database.ActiveTransaction;
            _cmdUpdateScanFilesProcessed.ExecuteScalar();
            ArtContentManager.Static.Database.CommitTransaction();
        }

        public static void RecordScanAbort(Actions.Scan scan)
        {
            SqlConnection DB = ArtContentManager.Static.Database.DB;

            if (_cmdRecordScanAbort == null)
            {
                string sqlUpdateScan = "UPDATE ScanHistory Set Aborted = @Aborted, ProcessedFiles = @ProcessedFiles " +
                                       "WHERE FolderName = @FolderName AND Started = @Started";

                _cmdRecordScanAbort = new SqlCommand(sqlUpdateScan, DB);

                _cmdRecordScanAbort.Parameters.Add("@FolderName", System.Data.SqlDbType.NVarChar, 255);
                _cmdRecordScanAbort.Parameters.Add("@Started", System.Data.SqlDbType.DateTime);
                _cmdRecordScanAbort.Parameters.Add("@ProcessedFiles", System.Data.SqlDbType.Int);
                _cmdRecordScanAbort.Parameters.Add("@Aborted", System.Data.SqlDbType.DateTime);
            }

            _cmdRecordScanAbort.Parameters["@FolderRoot"].Value = scan.FolderName;
            _cmdRecordScanAbort.Parameters["@Started"].Value = scan.StartScanTime;
            _cmdRecordScanAbort.Parameters["@ProcessedFiles"].Value = scan.ProcessedFiles;
            _cmdRecordScanAbort.Parameters["@Aborted"].Value = scan.AbortScanTime;

            ArtContentManager.Static.Database.BeginTransaction();
            _cmdRecordScanAbort.Transaction = ArtContentManager.Static.Database.ActiveTransaction;
            _cmdRecordScanAbort.ExecuteScalar();
            ArtContentManager.Static.Database.CommitTransaction();

        }

        public static void RecordScanComplete(Actions.Scan scan)
        {
            SqlConnection DB = ArtContentManager.Static.Database.DB;

            if (_cmdRecordScanComplete == null)
            {
                string sqlUpdateScan = "UPDATE ScanHistory Set Completed = @Completed, ProcessedFiles = @ProcessedFiles " +
                                       "WHERE FolderName = @FolderName AND Started = @Started";

                _cmdRecordScanComplete = new SqlCommand(sqlUpdateScan, DB);
                _cmdRecordScanComplete.Parameters.Add("@FolderName", System.Data.SqlDbType.NVarChar, 255);
                _cmdRecordScanComplete.Parameters.Add("@Started", System.Data.SqlDbType.DateTime);
                _cmdRecordScanComplete.Parameters.Add("@ProcessedFiles", System.Data.SqlDbType.Int);
                _cmdRecordScanComplete.Parameters.Add("@Completed", System.Data.SqlDbType.DateTime);
            }

            _cmdRecordScanComplete.Parameters["@FolderName"].Value = scan.FolderName;
            _cmdRecordScanComplete.Parameters["@Started"].Value = scan.StartScanTime;
            _cmdRecordScanComplete.Parameters["@ProcessedFiles"].Value = scan.ProcessedFiles;
            _cmdRecordScanComplete.Parameters["@Completed"].Value = scan.CompleteScanTime;

            ArtContentManager.Static.Database.BeginTransaction();
            _cmdRecordScanComplete.Transaction = ArtContentManager.Static.Database.ActiveTransaction;
            _cmdRecordScanComplete.ExecuteScalar();
            ArtContentManager.Static.Database.CommitTransaction();
        }


        public static void UpdateAll(Actions.Scan scan)
        {

            SqlConnection DB = ArtContentManager.Static.Database.DB;

            if (_cmdUpdateAll == null)
            {

                string sqlUpdateScan = "UPDATE ScanHistory Set TotalFiles = @TotalFiles, NewFiles = @NewFiles, ProcessedFiles = @ProcessedFiles, Aborted = @Aborted, Completed = @Completed " +
                                       "WHERE FolderName = @FolderName AND Started = @Started";

                _cmdUpdateAll = new SqlCommand(sqlUpdateScan, DB);

                _cmdUpdateAll.Parameters.Add("@FolderName", System.Data.SqlDbType.NVarChar, 255);
                _cmdUpdateAll.Parameters.Add("@Started", System.Data.SqlDbType.DateTime);
                _cmdUpdateAll.Parameters.Add("@TotalFiles", System.Data.SqlDbType.Int);
                _cmdUpdateAll.Parameters.Add("@NewFiles", System.Data.SqlDbType.Int);
                _cmdUpdateAll.Parameters.Add("@ProcessedFiles", System.Data.SqlDbType.Int);
                _cmdUpdateAll.Parameters.Add("@Aborted", System.Data.SqlDbType.DateTime);
                _cmdUpdateAll.Parameters.Add("@Completed", System.Data.SqlDbType.DateTime);

            }

            _cmdUpdateAll.Parameters["@FolderName"].Value = scan.FolderName;
            _cmdUpdateAll.Parameters["@Started"].Value = scan.StartScanTime;
            _cmdUpdateAll.Parameters["@TotalFiles"].Value = scan.TotalFiles;
            _cmdUpdateAll.Parameters["@NewFiles"].Value = scan.NewFiles;
            _cmdUpdateAll.Parameters["@ProcessedFiles"].Value = scan.NewFiles;
            _cmdUpdateAll.Parameters["@Aborted"].Value = scan.AbortScanTime;
            _cmdUpdateAll.Parameters["@Completed"].Value = scan.CompleteScanTime;

            ArtContentManager.Static.Database.BeginTransaction();
            _cmdUpdateAll.Transaction = ArtContentManager.Static.Database.ActiveTransaction;
            _cmdUpdateAll.ExecuteScalar();
            ArtContentManager.Static.Database.CommitTransaction();
        }

    }
}
