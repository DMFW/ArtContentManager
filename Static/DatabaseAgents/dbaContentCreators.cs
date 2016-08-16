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
    static class dbaContentCreators
    {
        static SqlCommand _cmdReadContentCreatorsByID;
        static SqlCommand _cmdReadContentCreatorsByName;
        static SqlCommand _cmdInsertContentCreator  ;

        public static void Load(ArtContentManager.Content.Creator Creator)
        {
            SqlConnection DB = ArtContentManager.Static.Database.DBReadOnly;

            // Assume the ID is set and then load the rest of the data
            if (_cmdReadContentCreatorsByID == null)
            {
                string readContentCreatorsSQL = "Select * from ContentCreators where CreatorID = @CreatorID";
                _cmdReadContentCreatorsByName = new SqlCommand(readContentCreatorsSQL, DB);
                _cmdReadContentCreatorsByName.Parameters.Add("@CreatorID", System.Data.SqlDbType.Int);
            }

            _cmdReadContentCreatorsByName.Parameters["@CreatorID"].Value = Creator.ID;

            _cmdReadContentCreatorsByName.Transaction = ArtContentManager.Static.Database.CurrentTransaction(Static.Database.TransactionType.ReadOnly);
            SqlDataReader reader = _cmdReadContentCreatorsByName.ExecuteReader();

            while (reader.Read())
            {
                Creator.ID = (int)reader["CreatorID"];
                Creator.CreatorTrueName = reader["CreatorTrueName"].ToString();
                Creator.CreatorDirectoryName = reader["CreatorDirectoryName"].ToString();
                Creator.ContactEmail = reader["ContactEmail"].ToString();
                Creator.Notes = reader["Notes"].ToString();
            }

            reader.Close();

        }

        public static bool IsCreatorRecorded(ArtContentManager.Content.Creator Creator)
        {
            SqlConnection DB = ArtContentManager.Static.Database.DBReadOnly;

            // Set default return value and output parameters
            bool isContentCreatorRecorded = false;

            if (_cmdReadContentCreatorsByName == null)
            {
                string readContentCreatorsSQL = "Select * from ContentCreators where CreatorNameCode = @CreatorNameCode";
                _cmdReadContentCreatorsByName = new SqlCommand(readContentCreatorsSQL, DB);
                _cmdReadContentCreatorsByName.Parameters.Add("@CreatorNameCode", System.Data.SqlDbType.NVarChar, 50);
            }

            _cmdReadContentCreatorsByName.Parameters["@CreatorNameCode"].Value = Creator.CreatorNameCode;

            _cmdReadContentCreatorsByName.Transaction = ArtContentManager.Static.Database.CurrentTransaction(Static.Database.TransactionType.ReadOnly);
            SqlDataReader reader = _cmdReadContentCreatorsByName.ExecuteReader();

            while (reader.Read())
            {
                if (isContentCreatorRecorded)
                {
                    Trace.WriteLine("Creator name found");
                }
                Creator.ID = (int)reader["CreatorID"];
                Creator.CreatorTrueName = reader["CreatorTrueName"].ToString();
                Creator.CreatorDirectoryName = reader["CreatorDirectoryName"].ToString();
                Creator.ContactEmail = reader["ContactEmail"].ToString();
                Creator.Notes = reader["Notes"].ToString();
                isContentCreatorRecorded = true;
            }

            reader.Close();
            return isContentCreatorRecorded;
        }

        public static void RecordContentCreator(ArtContentManager.Content.Creator Creator)
        {
            SqlConnection DB = ArtContentManager.Static.Database.DBActive;

            if (_cmdInsertContentCreator == null)
            {
                string insertFileSQL = "INSERT INTO ContentCreators (CreatorNameCode, CreatorDirectoryName, CreatorTrueName, ContactEmail, Notes) VALUES (@CreatorNameCode, @CreatorDirectoryName, @CreatorTrueName, @ContactEmail, @Notes) SET @CreatorID = SCOPE_IDENTITY();";
                _cmdInsertContentCreator = new SqlCommand(insertFileSQL, DB);

                _cmdInsertContentCreator.Parameters.Add("@CreatorNameCode", System.Data.SqlDbType.NVarChar, 50);
                _cmdInsertContentCreator.Parameters.Add("@CreatorDirectoryName", System.Data.SqlDbType.NVarChar, 50);
                _cmdInsertContentCreator.Parameters.Add("@CreatorTrueName", System.Data.SqlDbType.NVarChar, 255);
                _cmdInsertContentCreator.Parameters.Add("@ContactEmail", System.Data.SqlDbType.NVarChar, 255);
                _cmdInsertContentCreator.Parameters.Add("@Notes", System.Data.SqlDbType.NVarChar, 255);
                _cmdInsertContentCreator.Parameters.Add("@CreatorID", System.Data.SqlDbType.Int).Direction = System.Data.ParameterDirection.Output;

            }

            _cmdInsertContentCreator.Parameters["@CreatorNameCode"].Value = Creator.CreatorNameCode;
            _cmdInsertContentCreator.Parameters["@CreatorDirectoryName"].Value = Creator.CreatorDirectoryName;
            _cmdInsertContentCreator.Parameters["@CreatorTrueName"].Value = Creator.CreatorTrueName;
            _cmdInsertContentCreator.Parameters["@ContactEmail"].Value = Creator.ContactEmail;
            _cmdInsertContentCreator.Parameters["@Notes"].Value = Creator.Notes;

            _cmdInsertContentCreator.Transaction = ArtContentManager.Static.Database.CurrentTransaction(Database.TransactionType.Active);
            _cmdInsertContentCreator.ExecuteScalar();

            Creator.ID = (int)_cmdInsertContentCreator.Parameters["@CreatorID"].Value;

        }

    }
}
