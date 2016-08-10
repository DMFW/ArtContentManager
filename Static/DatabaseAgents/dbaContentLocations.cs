using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace ArtContentManager.Static.DatabaseAgents
{
    class dbaContentLocations
    {

        static SqlCommand _cmdInsertContentLocation;

        public static void RecordContentLocation(ArtContentManager.Content.ContentLocation ContentLocation)
        {
            SqlConnection DB = ArtContentManager.Static.Database.DBActive;

            if (_cmdInsertContentLocation == null)
            {
                string insertFileSQL = "INSERT INTO ContentLocations (OrganisationSchemeID, InstallationID, ContentDirectoryName, CategoryRootName, ContentTagPath, ContentTag, SubFolderCount, ItemCount) VALUES (@OrganisationSchemeID, @InstallationID, @ContentDirectoryName, @CategoryRootName, @ContentTagPath, @ContentTag, @SubFolderCount, @ItemCount);";
                _cmdInsertContentLocation = new SqlCommand(insertFileSQL, DB);

                _cmdInsertContentLocation.Parameters.Add("@OrganisationSchemeID", System.Data.SqlDbType.Int);
                _cmdInsertContentLocation.Parameters.Add("@InstallationID", System.Data.SqlDbType.Int);
                _cmdInsertContentLocation.Parameters.Add("@ContentDirectoryName", System.Data.SqlDbType.NVarChar, 255);
                _cmdInsertContentLocation.Parameters.Add("@CategoryRootName", System.Data.SqlDbType.NVarChar, 50);
                _cmdInsertContentLocation.Parameters.Add("@ContentTagPath", System.Data.SqlDbType.NVarChar, 255);
                _cmdInsertContentLocation.Parameters.Add("@ContentTag", System.Data.SqlDbType.NVarChar, 255);
                _cmdInsertContentLocation.Parameters.Add("@SubFolderCount", System.Data.SqlDbType.Int);
                _cmdInsertContentLocation.Parameters.Add("@ItemCount", System.Data.SqlDbType.Int);
            }

            _cmdInsertContentLocation.Parameters["@OrganisationSchemeID"].Value = ContentLocation.OrganisationSchemeID;
            _cmdInsertContentLocation.Parameters["@InstallationID"].Value = ContentLocation.Installation.RootID;
            _cmdInsertContentLocation.Parameters["@ContentDirectoryName"].Value = ContentLocation.ContentDirectoryName;
            _cmdInsertContentLocation.Parameters["@CategoryRootName"].Value = ContentLocation.Category.Name;
            _cmdInsertContentLocation.Parameters["@ContentTagPath"].Value = ContentLocation.ContentTagPath;
            _cmdInsertContentLocation.Parameters["@ContentTag"].Value = ContentLocation.ContentTag;
            _cmdInsertContentLocation.Parameters["@SubFolderCount"].Value = ContentLocation.SubFolderCount;
            _cmdInsertContentLocation.Parameters["@ItemCount"].Value = ContentLocation.ItemCount;

            _cmdInsertContentLocation.Transaction = ArtContentManager.Static.Database.CurrentTransaction(Database.TransactionType.Active);
            _cmdInsertContentLocation.ExecuteScalar();

        }

    }
}
