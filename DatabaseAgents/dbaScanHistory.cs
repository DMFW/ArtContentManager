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
        SqlCommand cmdReadFiles; 
        SqlCommand cmdReadFileLocations;
        SqlCommand cmdInsertFileLocations;
        SqlCommand cmdUpdateFileLocationVerified;
        SqlCommand cmdUpdateFileLocationAntiVerified;

        public void RecordStartScan(ArtContentManager.Content.Scan scan)
        {
            string sqlInsertScan;

            sqlInsertScan = "INSERT INTO ScanHistory (FolderRoot, Started) Values(@FolderRoot, @Started)";

            SqlCommand cmdInsertScan = new SqlCommand(sqlInsertScan, Static.Database.DB);

            cmdInsertScan.Parameters.Add("@FolderRoot", System.Data.SqlDbType.NVarChar, 255);
            cmdInsertScan.Parameters.Add("@Started", System.Data.SqlDbType.DateTime);

            cmdInsertScan.Transaction = trnActive;
            cmdInsertScan.Parameters["@FolderRoot"].Value = scan.FolderRoot;
            cmdInsertScan.Parameters["@Started"].Value = scan.StartScanTime;

            cmdInsertScan.ExecuteScalar();
        }

    }
}
