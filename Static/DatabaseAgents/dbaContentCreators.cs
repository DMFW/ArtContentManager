using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Diagnostics;
using System.Data;

namespace ArtContentManager.Static.DatabaseAgents
{
    static class dbaContentCreators
    {

        // Used for population of DataTable for maintenance

        static DataTable _tblContentCreators;
        static bool _dataTableLoaded = false;

        // Used for maintaining objects for use within a product

        static SqlCommand _cmdReadContentCreatorsByID;
        static SqlCommand _cmdReadContentCreatorsByName;
        static SqlCommand _cmdInsertContentCreator;
        static SqlCommand _cmdUpdateContentCreator;
        static SqlCommand _cmdCountProducts;
        static SqlCommand _cmdReadProductCredits;

        #region DirectDataTable

        static public void LoadContentCreators(bool forceLoad)
        {

            if ((!forceLoad) & (_dataTableLoaded))
            {
                return;
            }

            SqlConnection DB = ArtContentManager.Static.Database.DBReadOnly;
            string sqlContentCreators = "SELECT 'False' IsSelected, * FROM ContentCreators ORDER BY CreatorNameCode";
            SqlCommand cmdSelectContentCreators = new SqlCommand(sqlContentCreators, DB);

            if (_tblContentCreators == null)
            {
                _tblContentCreators = new DataTable("ContentCreators");
            }
            else
            {
                _tblContentCreators.Clear();
            }

            using (SqlDataAdapter sadContentCreators = new SqlDataAdapter(cmdSelectContentCreators))
            {
                sadContentCreators.Fill(_tblContentCreators);
            }

            DataColumn[] primaryKeyColumns = new DataColumn[1];
            primaryKeyColumns[0] = _tblContentCreators.Columns["CreatorID"];
            _tblContentCreators.PrimaryKey = primaryKeyColumns;

            _dataTableLoaded = true;

        }

        static public DataTable tblContentCreators
        {
            get { return _tblContentCreators; }
        }

        static public List<Content.Creator> SelectedContentCreators()
        {
            DataTable tblContentCreators = Static.DatabaseAgents.dbaContentCreators.tblContentCreators;

            var results = from DataRow selectedRow in tblContentCreators.Rows
                          where (string)selectedRow["IsSelected"] == "True"
                          select selectedRow;

            List<DataRow> selectedRows = results.ToList<DataRow>();
            List<Content.Creator> lstSelectedContentCreators = new List<Content.Creator>();

            foreach (DataRow row in selectedRows)
            {
                Content.Creator Creator = new Content.Creator();
                Creator.CreatorID = row.Field<int>("CreatorID");
                Load(Creator);
                lstSelectedContentCreators.Add(Creator);
            }
            return lstSelectedContentCreators;
        }

        static public void AddObjectToDataTable(Content.Creator newContentCreator)
        {

            if (_tblContentCreators == null) { return; }

            DataRow newRow = null; // A new row will be returned from the load data row with object method if we pass null in to it 

            ArtContentManager.Static.DataObjectUtilities.LoadDataRowWithObject(newContentCreator, ref _tblContentCreators, ref newRow);
            newRow.SetField("IsSelected", "True"); // So it is flagged on the grid.
        }

        static public void UpdateObjectOnDataTable(Content.Creator updatedContentCreator)
        {

            if (_tblContentCreators == null) { return; }

            DataRow rowToUpdate = _tblContentCreators.Rows.Find(updatedContentCreator.CreatorID);
            ArtContentManager.Static.DataObjectUtilities.LoadDataRowWithObject(updatedContentCreator, ref _tblContentCreators, ref rowToUpdate);

        }

        #endregion DirectDataTable

        #region ObjectControl

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

            _cmdReadContentCreatorsByName.Parameters["@CreatorID"].Value = Creator.CreatorID;

            _cmdReadContentCreatorsByName.Transaction = ArtContentManager.Static.Database.CurrentTransaction(Static.Database.TransactionType.ReadOnly);
            SqlDataReader reader = _cmdReadContentCreatorsByName.ExecuteReader();

            while (reader.Read())
            {
                Creator.CreatorID = (int)reader["CreatorID"];
                Creator.CreatorNameCode = reader["CreatorNameCode"].ToString();
                Creator.CreatorTrueName = reader["CreatorTrueName"].ToString();
                Creator.CreatorDirectoryName = reader["CreatorDirectoryName"].ToString();
                Creator.ContactEmail = reader["ContactEmail"].ToString();
                Creator.CreatorURI = reader["CreatorURI"].ToString();
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
                Creator.CreatorID = (int)reader["CreatorID"];
                Creator.CreatorTrueName = reader["CreatorTrueName"].ToString();
                Creator.CreatorDirectoryName = reader["CreatorDirectoryName"].ToString();
                Creator.ContactEmail = reader["ContactEmail"].ToString();
                Creator.CreatorURI = reader["CreatorURI"].ToString();
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
                string insertFileSQL = "INSERT INTO ContentCreators (CreatorNameCode, CreatorDirectoryName, CreatorTrueName, ContactEmail, CreatorURI, Notes) VALUES (@CreatorNameCode, @CreatorDirectoryName, @CreatorTrueName, @ContactEmail, @CreatorURI, @Notes) SET @CreatorID = SCOPE_IDENTITY();";
                _cmdInsertContentCreator = new SqlCommand(insertFileSQL, DB);

                _cmdInsertContentCreator.Parameters.Add("@CreatorNameCode", System.Data.SqlDbType.NVarChar, 50);
                _cmdInsertContentCreator.Parameters.Add("@CreatorDirectoryName", System.Data.SqlDbType.NVarChar, 50);
                _cmdInsertContentCreator.Parameters.Add("@CreatorTrueName", System.Data.SqlDbType.NVarChar, 255);
                _cmdInsertContentCreator.Parameters.Add("@ContactEmail", System.Data.SqlDbType.NVarChar, 255);
                _cmdInsertContentCreator.Parameters.Add("@CreatorURI", System.Data.SqlDbType.NVarChar, 255);
                _cmdInsertContentCreator.Parameters.Add("@Notes", System.Data.SqlDbType.NVarChar, 255);
                _cmdInsertContentCreator.Parameters.Add("@CreatorID", System.Data.SqlDbType.Int).Direction = System.Data.ParameterDirection.Output;

            }

            _cmdInsertContentCreator.Parameters["@CreatorNameCode"].Value = Creator.CreatorNameCode;
            _cmdInsertContentCreator.Parameters["@CreatorDirectoryName"].Value = Creator.CreatorDirectoryName;
            _cmdInsertContentCreator.Parameters["@CreatorTrueName"].Value = Creator.CreatorTrueName;
            _cmdInsertContentCreator.Parameters["@ContactEmail"].Value = Creator.ContactEmail;
            _cmdInsertContentCreator.Parameters["@CreatorURI"].Value = Creator.CreatorURI;
            _cmdInsertContentCreator.Parameters["@Notes"].Value = Creator.Notes;

            _cmdInsertContentCreator.Transaction = ArtContentManager.Static.Database.CurrentTransaction(Database.TransactionType.Active);
            _cmdInsertContentCreator.ExecuteScalar();

            Creator.CreatorID = (int)_cmdInsertContentCreator.Parameters["@CreatorID"].Value;

        }

        public static void UpdateContentCreator(ArtContentManager.Content.Creator Creator)
        {
            SqlConnection DB = ArtContentManager.Static.Database.DBActive;

            if (_cmdUpdateContentCreator == null)
            {
                string updateFileSQL = "UPDATE ContentCreators Set CreatorNameCode = @CreatorNameCode, CreatorDirectoryName = @CreatorDirectoryName, CreatorTrueName = @CreatorTrueName, ContactEmail = @ContactEmail, CreatorURI = @CreatorURI, Notes = @Notes WHERE CreatorID = @CreatorID;";
                _cmdUpdateContentCreator = new SqlCommand(updateFileSQL, DB);

                _cmdUpdateContentCreator.Parameters.Add("@CreatorNameCode", System.Data.SqlDbType.NVarChar, 50);
                _cmdUpdateContentCreator.Parameters.Add("@CreatorDirectoryName", System.Data.SqlDbType.NVarChar, 50);
                _cmdUpdateContentCreator.Parameters.Add("@CreatorTrueName", System.Data.SqlDbType.NVarChar, 255);
                _cmdUpdateContentCreator.Parameters.Add("@ContactEmail", System.Data.SqlDbType.NVarChar, 255);
                _cmdUpdateContentCreator.Parameters.Add("@CreatorURI", System.Data.SqlDbType.NVarChar, 255);
                _cmdUpdateContentCreator.Parameters.Add("@Notes", System.Data.SqlDbType.NVarChar, 255);
                _cmdUpdateContentCreator.Parameters.Add("@CreatorID", System.Data.SqlDbType.Int);

            }

            _cmdUpdateContentCreator.Parameters["@CreatorID"].Value = Creator.CreatorID;
            _cmdUpdateContentCreator.Parameters["@CreatorNameCode"].Value = Creator.CreatorNameCode;
            _cmdUpdateContentCreator.Parameters["@CreatorDirectoryName"].Value = Creator.CreatorDirectoryName;
            _cmdUpdateContentCreator.Parameters["@CreatorTrueName"].Value = Creator.CreatorTrueName;
            _cmdUpdateContentCreator.Parameters["@ContactEmail"].Value = Creator.ContactEmail;
            _cmdUpdateContentCreator.Parameters["@CreatorURI"].Value = Creator.CreatorURI;
            _cmdUpdateContentCreator.Parameters["@Notes"].Value = Creator.Notes;

            _cmdUpdateContentCreator.Transaction = ArtContentManager.Static.Database.CurrentTransaction(Database.TransactionType.Active);
            _cmdUpdateContentCreator.ExecuteScalar();

        }

        public static int ProductCount(ArtContentManager.Content.Creator Creator)
        {

            SqlConnection DB = ArtContentManager.Static.Database.DBReadOnly;
            int productCount = 0;

            if (_cmdCountProducts == null)
            {
                string countSQL = "SELECT Count(*) FROM ProductCreators WHERE CreatorID = @CreatorID";
                _cmdCountProducts = new SqlCommand(countSQL, DB);
                _cmdCountProducts.Parameters.Add("@CreatorID", System.Data.SqlDbType.Int);
            }

            _cmdCountProducts.Parameters["@CreatorID"].Value = Creator.CreatorID;

            SqlDataReader reader = _cmdCountProducts.ExecuteReader();

            while (reader.Read())
            {
                productCount = (int)reader[0];
            }
            reader.Close();

            return productCount;

        }

        public static ObservableCollection<Content.Product> LoadProductCreditsList(ArtContentManager.Content.Creator Creator)
        {

            SqlConnection DB = ArtContentManager.Static.Database.DBReadOnly;

            ObservableCollection<Content.Product> obcProductCredits = new ObservableCollection<Content.Product>();

            if (_cmdReadProductCredits == null)
            {
                string readSQL = "SELECT ProductID FROM ProductCreators WHERE CreatorID = @CreatorID";
                _cmdReadProductCredits = new SqlCommand(readSQL, DB);
                _cmdReadProductCredits.Parameters.Add("@CreatorID", System.Data.SqlDbType.Int);
            }

            _cmdReadProductCredits.Parameters["@CreatorID"].Value = Creator.CreatorID;

            SqlDataReader reader = _cmdReadProductCredits.ExecuteReader();

            while (reader.Read())
            {
                Content.Product creditedProduct = new Content.Product();

                creditedProduct.ID = (int)reader["ProductID"];

                dbaProduct.ProductLoadOptions loadOptions = new dbaProduct.ProductLoadOptions
                {
                    basic = true,
                    installationFiles = false,
                    contentFiles = false,
                    creators = false
                };

                dbaProduct.Load(creditedProduct, loadOptions);
                obcProductCredits.Add(creditedProduct);

            }
            reader.Close();

            return obcProductCredits;

        }

        #endregion ObjectControl
    }
}
