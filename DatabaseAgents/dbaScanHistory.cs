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

        public void RecordStartScan(ArtContentManager.Actions.Scan scan)
        {
            string sqlInsertScan;

            sqlInsertScan = "INSERT INTO ScanHistory (FolderRoot, Started) Values(@FolderRoot, @Started)";

            SqlCommand cmdInsertScan = new SqlCommand(sqlInsertScan, Static.Database.DB);

            cmdInsertScan.Parameters.Add("@FolderRoot", System.Data.SqlDbType.NVarChar, 255);
            cmdInsertScan.Parameters.Add("@Started", System.Data.SqlDbType.DateTime);

            cmdInsertScan.Parameters["@FolderRoot"].Value = scan.FolderRoot;
            cmdInsertScan.Parameters["@Started"].Value = scan.StartScanTime;

            BeginTransaction();
            cmdInsertScan.Transaction = trnActive;
            cmdInsertScan.ExecuteScalar();
            CommitTransaction();
        }

        public void SetLastCompletedScanTime(ArtContentManager.Actions.Scan scan)
        {
            string sqlSelectScan;

            scan.PreviousCompletedScanTime = DateTime.MinValue; // Assume no prior successful scan

            sqlSelectScan = "SELECT TOP 1 Completed FROM ScanHistory WHERE FolderRoot = @FolderRoot ORDER BY Completed DESC";
            SqlCommand cmdSelectScan = new SqlCommand(sqlSelectScan, Static.Database.DB);

            cmdSelectScan.Parameters.Add("@FolderRoot", System.Data.SqlDbType.NVarChar, 255);
            cmdSelectScan.Parameters["@FolderRoot"].Value = scan.FolderRoot;

            SqlDataReader rdrScanHistory = cmdSelectScan.ExecuteReader();
            while (rdrScanHistory.Read())
            {
                scan.PreviousCompletedScanTime = (DateTime)rdrScanHistory["Completed"];
            }

        }

        public void UpdateFilesCounted(ArtContentManager.Actions.Scan scan)
        {
            string sqlUpdateScan;

            sqlUpdateScan = "UPDATE ScanHistory Set FilesCounted = @FilesCounted WHERE FolderRoot = @FolderRoot AND Started = @Started";

            SqlCommand cmdUpdateScan = new SqlCommand(sqlUpdateScan, Static.Database.DB);

            cmdUpdateScan.Parameters.Add("@FolderRoot", System.Data.SqlDbType.NVarChar, 255);
            cmdUpdateScan.Parameters.Add("@Started", System.Data.SqlDbType.DateTime);
            cmdUpdateScan.Parameters.Add("@FilesCounted", System.Data.SqlDbType.Int);

            cmdUpdateScan.Parameters["@FolderRoot"].Value = scan.FolderRoot;
            cmdUpdateScan.Parameters["@Started"].Value = scan.StartScanTime;
            cmdUpdateScan.Parameters["@FilesCounted"].Value = scan.TotalFileCount;

            BeginTransaction();
            cmdUpdateScan.Transaction = trnActive;
            cmdUpdateScan.ExecuteScalar();
            CommitTransaction();
        }

        public void UpdateFilesProcessed(ArtContentManager.Actions.Scan scan)
        {
            string sqlUpdateScan;

            sqlUpdateScan = "UPDATE ScanHistory Set FilesProcessed = @FilesProcessed WHERE FolderRoot = @FolderRoot AND Started = @Started";

            SqlCommand cmdUpdateScan = new SqlCommand(sqlUpdateScan, Static.Database.DB);

            cmdUpdateScan.Parameters.Add("@FolderRoot", System.Data.SqlDbType.NVarChar, 255);
            cmdUpdateScan.Parameters.Add("@Started", System.Data.SqlDbType.DateTime);
            cmdUpdateScan.Parameters.Add("@FilesProcessed", System.Data.SqlDbType.Int);

            cmdUpdateScan.Parameters["@FolderRoot"].Value = scan.FolderRoot;
            cmdUpdateScan.Parameters["@Started"].Value = scan.StartScanTime;
            cmdUpdateScan.Parameters["@FilesProcessed"].Value = scan.ProcessedFileCount;

            BeginTransaction();
            cmdUpdateScan.Transaction = trnActive;
            cmdUpdateScan.ExecuteScalar();
            CommitTransaction();
        }

        public void RecordScanAbort(ArtContentManager.Actions.Scan scan)
        {
            string sqlUpdateScan;

            sqlUpdateScan = "UPDATE ScanHistory Set Aborted = @Aborted, FilesProcessed = @FilesProcessed " +
                            "WHERE FolderRoot = @FolderRoot AND Started = @Started";

            SqlCommand cmdUpdateScan = new SqlCommand(sqlUpdateScan, Static.Database.DB);

            cmdUpdateScan.Parameters.Add("@FolderRoot", System.Data.SqlDbType.NVarChar, 255);
            cmdUpdateScan.Parameters.Add("@Started", System.Data.SqlDbType.DateTime);
            cmdUpdateScan.Parameters.Add("@FilesProcessed", System.Data.SqlDbType.Int);
            cmdUpdateScan.Parameters.Add("@Aborted", System.Data.SqlDbType.DateTime);

            cmdUpdateScan.Parameters["@FolderRoot"].Value = scan.FolderRoot;
            cmdUpdateScan.Parameters["@Started"].Value = scan.StartScanTime;
            cmdUpdateScan.Parameters["@FilesProcessed"].Value = scan.ProcessedFileCount;
            cmdUpdateScan.Parameters["@Aborted"].Value = scan.AbortScanTime;

            BeginTransaction();
            cmdUpdateScan.Transaction = trnActive;
            cmdUpdateScan.ExecuteScalar();
            CommitTransaction();

        }

        public void RecordScanComplete(ArtContentManager.Actions.Scan scan)
        {
            string sqlUpdateScan;

            sqlUpdateScan = "UPDATE ScanHistory Set Completed = @Completed, FilesProcessed = @FilesProcessed " +
                            "WHERE FolderRoot = @FolderRoot AND Started = @Started";

            SqlCommand cmdUpdateScan = new SqlCommand(sqlUpdateScan, Static.Database.DB);

            cmdUpdateScan.Parameters.Add("@FolderRoot", System.Data.SqlDbType.NVarChar, 255);
            cmdUpdateScan.Parameters.Add("@Started", System.Data.SqlDbType.DateTime);
            cmdUpdateScan.Parameters.Add("@FilesProcessed", System.Data.SqlDbType.Int);
            cmdUpdateScan.Parameters.Add("@Completed", System.Data.SqlDbType.DateTime);

            cmdUpdateScan.Parameters["@FolderRoot"].Value = scan.FolderRoot;
            cmdUpdateScan.Parameters["@Started"].Value = scan.StartScanTime;
            cmdUpdateScan.Parameters["@FilesProcessed"].Value = scan.ProcessedFileCount;
            cmdUpdateScan.Parameters["@Completed"].Value = scan.CompleteScanTime;

            BeginTransaction();
            cmdUpdateScan.Transaction = trnActive;
            cmdUpdateScan.ExecuteScalar();
            CommitTransaction();
        }


        public void UpdateAll(ArtContentManager.Actions.Scan scan)
        {
            string sqlUpdateScan;

            sqlUpdateScan = "UPDATE ScanHistory Set FilesProcessed = @FilesProcessed, FilesCounted = @FilesCounted, Aborted = @Aborted, Completed = @Completed " + 
                            "WHERE FolderRoot = @FolderRoot AND Started = @Started";

            SqlCommand cmdUpdateScan = new SqlCommand(sqlUpdateScan, Static.Database.DB);

            cmdUpdateScan.Parameters.Add("@FolderRoot", System.Data.SqlDbType.NVarChar, 255);
            cmdUpdateScan.Parameters.Add("@Started", System.Data.SqlDbType.DateTime);
            cmdUpdateScan.Parameters.Add("@FilesProcessed", System.Data.SqlDbType.Int);
            cmdUpdateScan.Parameters.Add("@FilesCounted", System.Data.SqlDbType.Int);
            cmdUpdateScan.Parameters.Add("@Aborted", System.Data.SqlDbType.DateTime);
            cmdUpdateScan.Parameters.Add("@Completed", System.Data.SqlDbType.DateTime);

            cmdUpdateScan.Parameters["@FolderRoot"].Value = scan.FolderRoot;
            cmdUpdateScan.Parameters["@Started"].Value = scan.StartScanTime;
            cmdUpdateScan.Parameters["@FilesCounted"].Value = scan.TotalFileCount;
            cmdUpdateScan.Parameters["@FilesProcessed"].Value = scan.ProcessedFileCount;
            cmdUpdateScan.Parameters["@Aborted"].Value = scan.AbortScanTime;
            cmdUpdateScan.Parameters["@Completed"].Value = scan.CompleteScanTime;

            BeginTransaction();
            cmdUpdateScan.Transaction = trnActive;
            cmdUpdateScan.ExecuteScalar();
            CommitTransaction();
        }

    }
}
