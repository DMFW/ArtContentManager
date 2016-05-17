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
        static SqlCommand _cmdReadContentCreators;
        static SqlCommand _cmdInsertContentCreator;

        public static bool IsCreatorRecorded(ArtContentManager.Content.Creator Creator)
        {
            SqlConnection DB = ArtContentManager.Static.Database.DB;

            // Set default return value and output parameters
            bool isContentCreatorRecorded = false;

            if (_cmdReadContentCreators == null)
            {
                string readContentCreatorsSQL = "Select * from ContentCreators where CreatorNameCode = @CreatorNameCode";
                _cmdReadContentCreators = new SqlCommand(readContentCreatorsSQL, DB);
                _cmdReadContentCreators.Parameters.Add("@CreatorNameCode", System.Data.SqlDbType.NVarChar, 50);
            }

            _cmdReadContentCreators.Parameters["@CreatorNameCode"].Value = Creator.CreatorNameCode;

            _cmdReadContentCreators.Transaction = ArtContentManager.Static.Database.ActiveTransaction;
            SqlDataReader reader = _cmdReadContentCreators.ExecuteReader();

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
            SqlConnection DB = ArtContentManager.Static.Database.DB;

            if (_cmdInsertContentCreator == null)
            {
                string insertFileSQL = "INSERT INTO ContentCreators (CreatorNameCode, CreatorDirectoryName, CreatorTrueName, ContactEmail, Notes) VALUES (@CreatorNameCode, @CreatorDirectoryName, @CreatorTrueName, @ContactEmail, @Notes) SET @ContactID = SCOPE_IDENTITY();";
                _cmdInsertContentCreator = new SqlCommand(insertFileSQL, DB);

                _cmdInsertContentCreator.Parameters.Add("@CreatorNameCode", System.Data.SqlDbType.NVarChar, 50);
                _cmdInsertContentCreator.Parameters.Add("@CreatorDirectoryName", System.Data.SqlDbType.NVarChar, 50);
                _cmdInsertContentCreator.Parameters.Add("@CreatorTrueName", System.Data.SqlDbType.NVarChar, 255);
                _cmdInsertContentCreator.Parameters.Add("@ContactEmail", System.Data.SqlDbType.NVarChar, 255);
                _cmdInsertContentCreator.Parameters.Add("@Notes", System.Data.SqlDbType.NVarChar, 255);
                _cmdInsertContentCreator.Parameters.Add("@ContactID", System.Data.SqlDbType.Int).Direction = System.Data.ParameterDirection.Output;

            }

            _cmdInsertContentCreator.Transaction = ArtContentManager.Static.Database.ActiveTransaction;
            _cmdInsertContentCreator.Parameters["@CreatorNameCode"].Value = Creator.CreatorNameCode;
            _cmdInsertContentCreator.Parameters["@CreatorDirectoryName"].Value = Creator.CreatorDirectoryName;
            _cmdInsertContentCreator.Parameters["@CreatorTrueName"].Value = Creator.CreatorTrueName;
            _cmdInsertContentCreator.Parameters["@ContactEmail"].Value = Creator.ContactEmail;
            _cmdInsertContentCreator.Parameters["@Notes"].Value = Creator.Notes;
           
            _cmdInsertContentCreator.ExecuteScalar();

            Creator.ID = (int)_cmdInsertContentCreator.Parameters["@CreatorID"].Value;

        }

    }
}
