using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace ArtContentManager.Static.DatabaseAgents
{
    class dbaContentTypes
    {

        static SqlCommand _cmdInsertContentType;

        public static void RecordContentType(ArtContentManager.Content.ContentType ContentType)
        {
            SqlConnection DB = ArtContentManager.Static.Database.DBActive;

            if (_cmdInsertContentType == null)
            {
                string insertFileSQL = "INSERT INTO ContentTypes (OrganisationSchemeID, InstallationID, ContentDirectoryName, ContentTypeDescription) VALUES (@OrganisationSchemeID, @InstallationID, @ContentDirectoryName, @ContentTypeDescription);";
                _cmdInsertContentType = new SqlCommand(insertFileSQL, DB);

                _cmdInsertContentType.Parameters.Add("@OrganisationSchemeID", System.Data.SqlDbType.Int);
                _cmdInsertContentType.Parameters.Add("@InstallationID", System.Data.SqlDbType.Int);
                _cmdInsertContentType.Parameters.Add("@ContentDirectoryName", System.Data.SqlDbType.NVarChar, 255);
                _cmdInsertContentType.Parameters.Add("@ContentTypeDescription", System.Data.SqlDbType.NVarChar, 255);
            }

            _cmdInsertContentType.Parameters["@OrganisationSchemeID"].Value = ContentType.OrganisationSchemeID;
            _cmdInsertContentType.Parameters["@InstallationID"].Value = ContentType.Installation.RootID;
            _cmdInsertContentType.Parameters["@ContentDirectoryName"].Value = ContentType.DirectoryPath;
            _cmdInsertContentType.Parameters["@ContentTypeDescription"].Value = ContentType.Description;

            _cmdInsertContentType.Transaction = ArtContentManager.Static.Database.CurrentTransaction(Database.TransactionType.Active);
            _cmdInsertContentType.ExecuteScalar();

        }

    }
}
