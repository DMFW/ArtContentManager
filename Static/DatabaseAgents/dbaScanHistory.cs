﻿using System;
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
            SqlConnection DB = ArtContentManager.Static.Database.DBActive;

            if (_cmdInsertScan == null)
            {
                string sqlInsertScan = "INSERT INTO ScanHistory (FolderName, IsRequestRoot, Started, TotalFiles, NewFiles) Values(@FolderName, @IsRequestRoot, @Started, @TotalFiles, @NewFiles) SET @ScanID = SCOPE_IDENTITY();";

                _cmdInsertScan = new SqlCommand(sqlInsertScan, DB);
                _cmdInsertScan.Parameters.Add("@FolderName", System.Data.SqlDbType.NVarChar, 255);
                _cmdInsertScan.Parameters.Add("@IsRequestRoot", System.Data.SqlDbType.Bit);
                _cmdInsertScan.Parameters.Add("@Started", System.Data.SqlDbType.DateTime);
                _cmdInsertScan.Parameters.Add("@TotalFiles", System.Data.SqlDbType.Int);
                _cmdInsertScan.Parameters.Add("@NewFiles", System.Data.SqlDbType.Int);
                _cmdInsertScan.Parameters.Add("@ScanID", System.Data.SqlDbType.Int).Direction = System.Data.ParameterDirection.Output;

            }

            _cmdInsertScan.Parameters["@FolderName"].Value = scan.FolderName;
            _cmdInsertScan.Parameters["@IsRequestRoot"].Value = scan.IsRequestRoot;
            _cmdInsertScan.Parameters["@Started"].Value = scan.StartScanTime;
            _cmdInsertScan.Parameters["@TotalFiles"].Value = scan.TotalFiles;
            _cmdInsertScan.Parameters["@NewFiles"].Value = scan.NewFiles;

            ArtContentManager.Static.Database.BeginTransaction(Database.TransactionType.Active);
            _cmdInsertScan.Transaction = ArtContentManager.Static.Database.CurrentTransaction(Database.TransactionType.Active);
            _cmdInsertScan.ExecuteScalar();
            ArtContentManager.Static.Database.CommitTransaction(Database.TransactionType.Active);

            scan.ID = (int)_cmdInsertScan.Parameters["@ScanID"].Value;

        }

        public static void SetLastCompletedScanTime(Actions.Scan scan)
        {

            SqlConnection DB = ArtContentManager.Static.Database.DBReadOnly;
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
            SqlConnection DB = ArtContentManager.Static.Database.DBActive;

            if (_cmdUpdateScanInitialFileCounts == null)
            {
                string sqlUpdateScan = "UPDATE ScanHistory Set TotalFiles = @TotalFiles, NewFiles = @NewFiles WHERE ScanID = @ScanID";

                _cmdUpdateScanInitialFileCounts = new SqlCommand(sqlUpdateScan, DB);
                _cmdUpdateScanInitialFileCounts.Parameters.Add("@ScanID", System.Data.SqlDbType.Int);
                _cmdUpdateScanInitialFileCounts.Parameters.Add("@TotalFiles", System.Data.SqlDbType.Int);
                _cmdUpdateScanInitialFileCounts.Parameters.Add("@NewFiles", System.Data.SqlDbType.Int);
            }

            _cmdUpdateScanInitialFileCounts.Parameters["@ScanID"].Value = scan.ID;
            _cmdUpdateScanInitialFileCounts.Parameters["@TotalFiles"].Value = scan.TotalFiles;
            _cmdUpdateScanInitialFileCounts.Parameters["@NewFiles"].Value = scan.NewFiles;

            ArtContentManager.Static.Database.BeginTransaction(Database.TransactionType.Active);
            _cmdUpdateScanInitialFileCounts.Transaction = ArtContentManager.Static.Database.CurrentTransaction(Database.TransactionType.Active);
            _cmdUpdateScanInitialFileCounts.ExecuteScalar();
            ArtContentManager.Static.Database.CommitTransaction(Database.TransactionType.Active);
        }

        public static void UpdateFilesProcessed(Actions.Scan scan)
        {
            SqlConnection DB = ArtContentManager.Static.Database.DBActive;

            if (_cmdUpdateScanFilesProcessed == null)
            {
                string sqlUpdateScan = "UPDATE ScanHistory Set ProcessedFiles = @ProcessedFiles WHERE ScanID = @ScanID";
                _cmdUpdateScanFilesProcessed = new SqlCommand(sqlUpdateScan, DB);
                _cmdUpdateScanFilesProcessed.Parameters.Add("@ScanID", System.Data.SqlDbType.Int);
                _cmdUpdateScanFilesProcessed.Parameters.Add("@ProcessedFiles", System.Data.SqlDbType.Int);
            }

            _cmdUpdateScanFilesProcessed.Parameters["@ScanID"].Value = scan.ID;
            _cmdUpdateScanFilesProcessed.Parameters["@FilesProcessed"].Value = scan.ProcessedFiles;

            ArtContentManager.Static.Database.BeginTransaction(Database.TransactionType.Active);
            _cmdUpdateScanFilesProcessed.Transaction = ArtContentManager.Static.Database.CurrentTransaction(Database.TransactionType.Active);
            _cmdUpdateScanFilesProcessed.ExecuteScalar();
            ArtContentManager.Static.Database.CommitTransaction(Database.TransactionType.Active);
        }

        public static void RecordScanAbort(Actions.Scan scan)
        {
            SqlConnection DB = ArtContentManager.Static.Database.DBActive;

            if (_cmdRecordScanAbort == null)
            {
                string sqlUpdateScan = "UPDATE ScanHistory Set Aborted = @Aborted, ProcessedFiles = @ProcessedFiles " +
                                       "WHERE ScanID = @ScanID";

                _cmdRecordScanAbort = new SqlCommand(sqlUpdateScan, DB);

                _cmdRecordScanAbort.Parameters.Add("@ScanID", System.Data.SqlDbType.Int);
                _cmdRecordScanAbort.Parameters.Add("@ProcessedFiles", System.Data.SqlDbType.Int);
                _cmdRecordScanAbort.Parameters.Add("@Aborted", System.Data.SqlDbType.DateTime);
            }

            _cmdRecordScanAbort.Parameters["@ScanID"].Value = scan.ID;
            _cmdRecordScanAbort.Parameters["@ProcessedFiles"].Value = scan.ProcessedFiles;
            _cmdRecordScanAbort.Parameters["@Aborted"].Value = scan.AbortScanTime;
    
            _cmdRecordScanAbort.Transaction = ArtContentManager.Static.Database.CurrentTransaction(Database.TransactionType.Active);
            _cmdRecordScanAbort.ExecuteScalar();
        
        }

        public static void RecordScanComplete(Actions.Scan scan)
        {
            SqlConnection DB = ArtContentManager.Static.Database.DBActive;

            if (_cmdRecordScanComplete == null)
            {
                string sqlUpdateScan = "UPDATE ScanHistory Set Completed = @Completed, ProcessedFiles = @ProcessedFiles " +
                                       "WHERE ScanID = @ScanID";

                _cmdRecordScanComplete = new SqlCommand(sqlUpdateScan, DB);
                _cmdRecordScanComplete.Parameters.Add("@ScanID", System.Data.SqlDbType.Int);
                _cmdRecordScanComplete.Parameters.Add("@ProcessedFiles", System.Data.SqlDbType.Int);
                _cmdRecordScanComplete.Parameters.Add("@Completed", System.Data.SqlDbType.DateTime);
            }

            _cmdRecordScanComplete.Parameters["@ScanID"].Value = scan.ID;
            _cmdRecordScanComplete.Parameters["@ProcessedFiles"].Value = scan.ProcessedFiles;
            _cmdRecordScanComplete.Parameters["@Completed"].Value = scan.CompleteScanTime;

            _cmdRecordScanComplete.Transaction = ArtContentManager.Static.Database.CurrentTransaction(Database.TransactionType.Active);
            _cmdRecordScanComplete.ExecuteScalar();

        }


        public static void UpdateAll(Actions.Scan scan)
        {

            SqlConnection DB = ArtContentManager.Static.Database.DBActive;

            if (_cmdUpdateAll == null)
            {

                string sqlUpdateScan = "UPDATE ScanHistory Set FolderName = @FolderName, Started = @Started, TotalFiles = @TotalFiles, NewFiles = @NewFiles, ProcessedFiles = @ProcessedFiles, Aborted = @Aborted, Completed = @Completed " +
                                       "WHERE ScanID = @ScanID";

                _cmdUpdateAll = new SqlCommand(sqlUpdateScan, DB);

                _cmdUpdateAll.Parameters.Add("@ScanID", System.Data.SqlDbType.Int);
                _cmdUpdateAll.Parameters.Add("@FolderName", System.Data.SqlDbType.NVarChar, 255);
                _cmdUpdateAll.Parameters.Add("@Started", System.Data.SqlDbType.DateTime);
                _cmdUpdateAll.Parameters.Add("@TotalFiles", System.Data.SqlDbType.Int);
                _cmdUpdateAll.Parameters.Add("@NewFiles", System.Data.SqlDbType.Int);
                _cmdUpdateAll.Parameters.Add("@ProcessedFiles", System.Data.SqlDbType.Int);
                _cmdUpdateAll.Parameters.Add("@Aborted", System.Data.SqlDbType.DateTime);
                _cmdUpdateAll.Parameters.Add("@Completed", System.Data.SqlDbType.DateTime);

            }

            _cmdUpdateAll.Parameters["@ScanID"].Value = scan.ID;
            _cmdUpdateAll.Parameters["@FolderName"].Value = scan.FolderName;
            _cmdUpdateAll.Parameters["@Started"].Value = scan.StartScanTime;
            _cmdUpdateAll.Parameters["@TotalFiles"].Value = scan.TotalFiles;
            _cmdUpdateAll.Parameters["@NewFiles"].Value = scan.NewFiles;
            _cmdUpdateAll.Parameters["@ProcessedFiles"].Value = scan.NewFiles;
            _cmdUpdateAll.Parameters["@Aborted"].Value = scan.AbortScanTime;
            _cmdUpdateAll.Parameters["@Completed"].Value = scan.CompleteScanTime;

            ArtContentManager.Static.Database.BeginTransaction(Database.TransactionType.Active);
            _cmdUpdateAll.Transaction = ArtContentManager.Static.Database.CurrentTransaction(Database.TransactionType.Active);
            _cmdUpdateAll.ExecuteScalar();
            ArtContentManager.Static.Database.CommitTransaction(Database.TransactionType.Active);
        }

    }
}
