using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Text.RegularExpressions;

namespace ArtContentManager.Static.DatabaseAgents
{
    static class dbaInstallations
    {
        static SqlCommand _cmdInsertFile;

        public static void RecordInstallation(ArtContentManager.Content.Installation Installation)
        {
            SqlConnection DB = ArtContentManager.Static.Database.DBActive;

            if (_cmdInsertFile == null)
            {
                string insertFileSQL = "INSERT INTO Installations (InstallationRootPath, InstallationTypeID) VALUES (@InstallationRootPath, @InstallationTypeID) SET @InstallationRootID = SCOPE_IDENTITY();";
                _cmdInsertFile = new SqlCommand(insertFileSQL, DB);

                _cmdInsertFile.Parameters.Add("@InstallationRootPath", System.Data.SqlDbType.NVarChar, 255);
                _cmdInsertFile.Parameters.Add("@InstallationTypeID", System.Data.SqlDbType.Int);
                _cmdInsertFile.Parameters.Add("@InstallationRootID", System.Data.SqlDbType.Int).Direction = System.Data.ParameterDirection.Output;

            }

            _cmdInsertFile.Transaction = ArtContentManager.Static.Database.CurrentTransaction(Database.TransactionType.Active);
            _cmdInsertFile.Parameters["@InstallationRootPath"].Value = Installation.RootPath;
            _cmdInsertFile.Parameters["@InstallationTypeID"].Value = Installation.Type.TypeID;
            
            _cmdInsertFile.ExecuteScalar();

            Installation.RootID = (int)_cmdInsertFile.Parameters["@InstallationRootID"].Value;

        }
    }
}
