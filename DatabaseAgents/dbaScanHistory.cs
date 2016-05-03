using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Diagnostics;

namespace ArtContentManager.DatabaseAgents
{
    class dbaScanHistory : DatabaseAgent
    {

        private SqlConnection _DB;

        public dbaScanHistory()
        {
            _DB = ArtContentManager.Static.Database.DB;
        }

        public void RecordStartScan(ArtContentManager.Actions.Scan scan)
        {
            string sqlInsertScan;

            sqlInsertScan = "INSERT INTO ScanHistory (FolderName, IsRequestRoot, Started, TotalFiles, NewFiles) Values(@FolderName, @IsRequestRoot,  @Started, @TotalFiles, @NewFiles)";

            SqlCommand cmdInsertScan = new SqlCommand(sqlInsertScan, _DB);

            cmdInsertScan.Parameters.Add("@FolderName", System.Data.SqlDbType.NVarChar, 255);
            cmdInsertScan.Parameters.Add("@IsRequestRoot", System.Data.SqlDbType.Bit);
            cmdInsertScan.Parameters.Add("@Started", System.Data.SqlDbType.DateTime);
            cmdInsertScan.Parameters.Add("@TotalFiles", System.Data.SqlDbType.Int);
            cmdInsertScan.Parameters.Add("@NewFiles", System.Data.SqlDbType.Int);

            cmdInsertScan.Parameters["@FolderName"].Value = scan.FolderName;
            cmdInsertScan.Parameters["@IsRequestRoot"].Value = scan.IsRequestRoot;
            cmdInsertScan.Parameters["@Started"].Value = scan.StartScanTime;
            cmdInsertScan.Parameters["@TotalFiles"].Value = scan.TotalFiles;
            cmdInsertScan.Parameters["@NewFiles"].Value = scan.NewFiles;

            BeginTransaction();
            cmdInsertScan.Transaction = trnActive;
            cmdInsertScan.ExecuteScalar();
            CommitTransaction();
        }

        public void SetLastCompletedScanTime(ArtContentManager.Actions.Scan scan)
        {
            string sqlSelectScan;
            scan.PreviousCompletedScanTime = DateTime.MinValue; // Assume no prior successful scan

            sqlSelectScan = "SELECT TOP 1 Completed FROM ScanHistory WHERE FolderName = @FolderName ORDER BY Completed DESC";
            SqlCommand cmdSelectScan = new SqlCommand(sqlSelectScan, _DB);

            cmdSelectScan.Parameters.Add("@FolderName", System.Data.SqlDbType.NVarChar, 255);
            cmdSelectScan.Parameters["@FolderName"].Value = scan.FolderName;

            SqlDataReader rdrScanHistory = cmdSelectScan.ExecuteReader();
            while (rdrScanHistory.Read())
            {
                if (String.IsNullOrEmpty(rdrScanHistory["Completed"].ToString()))
                    { scan.PreviousCompletedScanTime = DateTime.MinValue; }
                else
                    { scan.PreviousCompletedScanTime = (DateTime)rdrScanHistory["Completed"]; }
            }
            rdrScanHistory.Close();
            cmdSelectScan.Dispose();
        }

        public void UpdateInitialFileCounts(ArtContentManager.Actions.Scan scan)
        {
            string sqlUpdateScan;

            sqlUpdateScan = "UPDATE ScanHistory Set TotalFiles = @TotalFiles, NewFiles = @NewFiles WHERE FolderName = @FolderName AND Started = @Started";

            SqlCommand cmdUpdateScan = new SqlCommand(sqlUpdateScan, _DB);

            cmdUpdateScan.Parameters.Add("@FolderName", System.Data.SqlDbType.NVarChar, 255);
            cmdUpdateScan.Parameters.Add("@Started", System.Data.SqlDbType.DateTime);
            cmdUpdateScan.Parameters.Add("@TotalFiles", System.Data.SqlDbType.Int);
            cmdUpdateScan.Parameters.Add("@NewFiles", System.Data.SqlDbType.Int);

            cmdUpdateScan.Parameters["@FolderName"].Value = scan.FolderName;
            cmdUpdateScan.Parameters["@Started"].Value = scan.StartScanTime;
            cmdUpdateScan.Parameters["@TotalFiles"].Value = scan.TotalFiles;
            cmdUpdateScan.Parameters["@NewFiles"].Value = scan.NewFiles;

            BeginTransaction();
            cmdUpdateScan.Transaction = trnActive;
            cmdUpdateScan.ExecuteScalar();
            CommitTransaction();
        }

        public void UpdateFilesProcessed(ArtContentManager.Actions.Scan scan)
        {
            string sqlUpdateScan;

            sqlUpdateScan = "UPDATE ScanHistory Set ProcessedFiles = @ProcessedFiles WHERE FolderName = @FolderName AND Started = @Started";

            SqlCommand cmdUpdateScan = new SqlCommand(sqlUpdateScan, _DB);

            cmdUpdateScan.Parameters.Add("@FolderName", System.Data.SqlDbType.NVarChar, 255);
            cmdUpdateScan.Parameters.Add("@Started", System.Data.SqlDbType.DateTime);
            cmdUpdateScan.Parameters.Add("@ProcessedFiles", System.Data.SqlDbType.Int);

            cmdUpdateScan.Parameters["@FolderRoot"].Value = scan.FolderName;
            cmdUpdateScan.Parameters["@Started"].Value = scan.StartScanTime;
            cmdUpdateScan.Parameters["@FilesProcessed"].Value = scan.ProcessedFiles;

            BeginTransaction();
            cmdUpdateScan.Transaction = trnActive;
            cmdUpdateScan.ExecuteScalar();
            CommitTransaction();
        }

        public void RecordScanAbort(ArtContentManager.Actions.Scan scan)
        {
            string sqlUpdateScan;

            sqlUpdateScan = "UPDATE ScanHistory Set Aborted = @Aborted, ProcessedFiles = @ProcessedFiles " +
                            "WHERE FolderName = @FolderName AND Started = @Started";

            SqlCommand cmdUpdateScan = new SqlCommand(sqlUpdateScan, _DB);

            cmdUpdateScan.Parameters.Add("@FolderName", System.Data.SqlDbType.NVarChar, 255);
            cmdUpdateScan.Parameters.Add("@Started", System.Data.SqlDbType.DateTime);
            cmdUpdateScan.Parameters.Add("@ProcessedFiles", System.Data.SqlDbType.Int);
            cmdUpdateScan.Parameters.Add("@Aborted", System.Data.SqlDbType.DateTime);

            cmdUpdateScan.Parameters["@FolderRoot"].Value = scan.FolderName;
            cmdUpdateScan.Parameters["@Started"].Value = scan.StartScanTime;
            cmdUpdateScan.Parameters["@ProcessedFiles"].Value = scan.ProcessedFiles;
            cmdUpdateScan.Parameters["@Aborted"].Value = scan.AbortScanTime;

            BeginTransaction();
            cmdUpdateScan.Transaction = trnActive;
            cmdUpdateScan.ExecuteScalar();
            CommitTransaction();

        }

        public void RecordScanComplete(ArtContentManager.Actions.Scan scan)
        {
            string sqlUpdateScan;

            sqlUpdateScan = "UPDATE ScanHistory Set Completed = @Completed, ProcessedFiles = @ProcessedFiles " +
                            "WHERE FolderName = @FolderName AND Started = @Started";

            SqlCommand cmdUpdateScan = new SqlCommand(sqlUpdateScan, _DB);

            cmdUpdateScan.Parameters.Add("@FolderName", System.Data.SqlDbType.NVarChar, 255);
            cmdUpdateScan.Parameters.Add("@Started", System.Data.SqlDbType.DateTime);
            cmdUpdateScan.Parameters.Add("@ProcessedFiles", System.Data.SqlDbType.Int);
            cmdUpdateScan.Parameters.Add("@Completed", System.Data.SqlDbType.DateTime);

            cmdUpdateScan.Parameters["@FolderRoot"].Value = scan.FolderName;
            cmdUpdateScan.Parameters["@Started"].Value = scan.StartScanTime;
            cmdUpdateScan.Parameters["@ProcessedFiles"].Value = scan.ProcessedFiles;
            cmdUpdateScan.Parameters["@Completed"].Value = scan.CompleteScanTime;

            BeginTransaction();
            cmdUpdateScan.Transaction = trnActive;
            cmdUpdateScan.ExecuteScalar();
            CommitTransaction();
        }


        public void UpdateAll(ArtContentManager.Actions.Scan scan)
        {
            string sqlUpdateScan;

            sqlUpdateScan = "UPDATE ScanHistory Set TotalFiles = @TotalFiles, NewFiles = @NewFiles, ProcessedFiles = @ProcessedFiles, Aborted = @Aborted, Completed = @Completed " + 
                            "WHERE FolderName = @FolderName AND Started = @Started";

            SqlCommand cmdUpdateScan = new SqlCommand(sqlUpdateScan, _DB);

            cmdUpdateScan.Parameters.Add("@FolderName", System.Data.SqlDbType.NVarChar, 255);
            cmdUpdateScan.Parameters.Add("@Started", System.Data.SqlDbType.DateTime);
            cmdUpdateScan.Parameters.Add("@TotalFiles", System.Data.SqlDbType.Int);
            cmdUpdateScan.Parameters.Add("@NewFiles", System.Data.SqlDbType.Int);
            cmdUpdateScan.Parameters.Add("@ProcessedFiles", System.Data.SqlDbType.Int);
            cmdUpdateScan.Parameters.Add("@Aborted", System.Data.SqlDbType.DateTime);
            cmdUpdateScan.Parameters.Add("@Completed", System.Data.SqlDbType.DateTime);

            cmdUpdateScan.Parameters["@FolderName"].Value = scan.FolderName;
            cmdUpdateScan.Parameters["@Started"].Value = scan.StartScanTime;
            cmdUpdateScan.Parameters["@TotalFiles"].Value = scan.TotalFiles;
            cmdUpdateScan.Parameters["@NewFiles"].Value = scan.NewFiles;
            cmdUpdateScan.Parameters["@ProcessedFiles"].Value = scan.NewFiles;
            cmdUpdateScan.Parameters["@Aborted"].Value = scan.AbortScanTime;
            cmdUpdateScan.Parameters["@Completed"].Value = scan.CompleteScanTime;

            BeginTransaction();
            cmdUpdateScan.Transaction = trnActive;
            cmdUpdateScan.ExecuteScalar();
            CommitTransaction();
        }

    }
}
