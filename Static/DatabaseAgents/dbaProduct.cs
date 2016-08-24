using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Data.SqlClient;
using System.Text.RegularExpressions;
using System.Diagnostics;

namespace ArtContentManager.Static.DatabaseAgents
{
    static class dbaProduct
    {

        // Handles database related updates and reads for the Product and closely related extension objects

        public struct ProductLoadOptions
        {
            public bool basic;
            public bool installationFiles;
            public bool contentFiles;
            public bool creators;
        }

        static SqlCommand _cmdReadProductByID;
        static SqlCommand _cmdReadProductInstallationFilesByID;
        static SqlCommand _cmdReadProductContentFilesByID;
        static SqlCommand _cmdReadProductCreatorsByID;
        static SqlCommand _cmdSelectUnassignedParentFiles;
        static SqlCommand _cmdAddProduct;
        static SqlCommand _cmdAddProductFile;
        static SqlCommand _cmdAddProductCreator;
        static SqlCommand _cmdUpdateProduct;

        public static void Load(ArtContentManager.Content.Product Product, ProductLoadOptions loadOptions)
        {

            // This function assumes the ID is set within the Product object and on that basis loads 
            // whatever level of detail is requested in the load options.

            SqlConnection DB = ArtContentManager.Static.Database.DBReadOnly;

            if (_cmdReadProductByID == null)
            {
                string readProductByID_SQL = "Select * from Products where ProductID = @ProductID";
                _cmdReadProductByID = new SqlCommand(readProductByID_SQL, DB);
                _cmdReadProductByID.Parameters.Add("@ProductID", System.Data.SqlDbType.Int);
            }

            if (loadOptions.basic)
            {
                _cmdReadProductByID.Parameters["@ProductID"].Value = Product.ID;

                _cmdReadProductByID.Transaction = ArtContentManager.Static.Database.CurrentTransaction(Database.TransactionType.ReadOnly);
                SqlDataReader reader = _cmdReadProductByID.ExecuteReader();

                while (reader.Read())
                {
                    Product.Name = reader["ProductName"].ToString();
                    Product.NameSavedToDatabase = reader["ProductName"].ToString();
                    Product.IsPrimary = (bool)reader["IsPrimary"];
                    Product.DatePurchased = reader["DatePurchased"] as DateTime?;
                    Product.MarketPlaceID = reader["MarketPlaceID"] as int? ?? default(int);
                    Product.ProductURI = reader["ProductURI"].ToString();
                }
                reader.Close();
            }

            if (loadOptions.installationFiles) { LoadProductInstallationFiles(Product); }
            if (loadOptions.contentFiles) { LoadProductContentFiles(Product); }
            if (loadOptions.creators) { LoadProductCreators(Product); }

        }

        #region PrivateLoadMethods

        private static void LoadProductInstallationFiles(ArtContentManager.Content.Product Product)
        {

            SqlConnection DB = ArtContentManager.Static.Database.DBReadOnly;

            if (_cmdReadProductInstallationFilesByID == null)
            {
                string readProductInstallationFilesByID_SQL = "Select * from ProductFiles where ProductID = @ProductID ORDER BY InstallerSequence";
                _cmdReadProductInstallationFilesByID = new SqlCommand(readProductInstallationFilesByID_SQL, DB);
                _cmdReadProductInstallationFilesByID.Parameters.Add("@ProductID", System.Data.SqlDbType.Int);
            }

            _cmdReadProductInstallationFilesByID.Parameters["@ProductID"].Value = Product.ID;

            _cmdReadProductInstallationFilesByID.Transaction = ArtContentManager.Static.Database.CurrentTransaction(Database.TransactionType.ReadOnly);
            SqlDataReader reader = _cmdReadProductInstallationFilesByID.ExecuteReader();

            while (reader.Read())
            {
                Content.File installationFile = new Content.File((int)reader["FileID"]);
                dbaFile.Load(installationFile);
                Product.InstallationFiles.Add(installationFile);
            }
            reader.Close();
        }


        private static void LoadProductContentFiles(ArtContentManager.Content.Product Product)
        {

            SqlConnection DB = ArtContentManager.Static.Database.DBReadOnly;

            if (_cmdReadProductContentFilesByID == null)
            {
                string readProductContentFilesByID_SQL = "SELECT FileID FROM Files WHERE ParentID = @ParentFileID";
                _cmdReadProductContentFilesByID = new SqlCommand(readProductContentFilesByID_SQL, DB);
                _cmdReadProductContentFilesByID.Parameters.Add("@ParentFileID", System.Data.SqlDbType.Int);
            }

            if (Product.InstallationFiles.Count == 0)
            {
                LoadProductInstallationFiles(Product);
            }

            foreach (Content.File installationFile in Product.InstallationFiles)
            {
                _cmdReadProductContentFilesByID.Parameters["@ParentFileID"].Value = installationFile.ID;

                _cmdReadProductContentFilesByID.Transaction = ArtContentManager.Static.Database.CurrentTransaction(Database.TransactionType.ReadOnly);
                SqlDataReader reader = _cmdReadProductContentFilesByID.ExecuteReader();

                while (reader.Read())
                {
                    Content.File contentFile = new Content.File((int)reader["FileID"]);
                    dbaFile.Load(contentFile);

                    Product.ContentFiles.Add(contentFile);

                    // Also add to the text file list of there is associated text
                    if (contentFile.Text != null)
                    {
                        if (contentFile.Text != string.Empty)
                        {
                            Product.TextFiles.Add(contentFile);
                        }
                    }

                }
                reader.Close();
            } 
        }

        private static void LoadProductCreators(ArtContentManager.Content.Product Product)
        {

            SqlConnection DB = ArtContentManager.Static.Database.DBReadOnly;

            if (_cmdReadProductCreatorsByID == null)
            {
                string readProductCreatorsByID_SQL = "Select * from ProductCreators where ProductID = @ProductID";
                _cmdReadProductCreatorsByID = new SqlCommand(readProductCreatorsByID_SQL, DB);
                _cmdReadProductCreatorsByID.Parameters.Add("@ProductID", System.Data.SqlDbType.Int);
            }

            _cmdReadProductCreatorsByID.Parameters["@ProductID"].Value = Product.ID;

            _cmdReadProductCreatorsByID.Transaction = ArtContentManager.Static.Database.CurrentTransaction(Database.TransactionType.ReadOnly);
            SqlDataReader reader = _cmdReadProductCreatorsByID.ExecuteReader();

            while (reader.Read())
            {
                Content.Creator creator = new Content.Creator();
                creator.ID = (int)reader["CreatorID"];
                dbaContentCreators.Load(creator);
                Product.Creators.Add(creator);
            }
            reader.Close();

        }

        #endregion PrivateLoadMethods

        public static void AutoAssignProducts()
        {

            int patternMatchLength;
            string thisFileName = "";
            string lastFileName = "";
            string lastPattern;
            string thisPattern;
            bool patternMatch;
            ArtContentManager.Content.Product product = null;

            // Assign products to anything that hasn't yet been assigned

            SqlConnection DBReadOnly = ArtContentManager.Static.Database.DBReadOnly;
            SqlConnection DBActive = ArtContentManager.Static.Database.DBActive;

            Static.Database.BeginTransaction(Database.TransactionType.Active);

            if (Static.DatabaseAgents.dbaSettings.Setting("ProductPatternMatchLength") == null)
            {
                MessageBox.Show("Auto assignment requires a product pattern match length to be defined in the settings.", "Auto assign products", MessageBoxButton.OKCancel);
                return;
            }
            else
            {
                patternMatchLength = Static.DatabaseAgents.dbaSettings.Setting("ProductPatternMatchLength").Item2;
            }

            if (_cmdSelectUnassignedParentFiles == null)
            {
                string sqlAutoProducts = "SELECT * from ParentFilesNotAssignedToProducts ORDER BY FileName Asc";
                _cmdSelectUnassignedParentFiles = new SqlCommand(sqlAutoProducts, DBReadOnly);
            }


            ArtContentManager.Static.Database.BeginTransaction(Database.TransactionType.ReadOnly);
           _cmdSelectUnassignedParentFiles.Transaction = ArtContentManager.Static.Database.CurrentTransaction(Database.TransactionType.ReadOnly);

            SqlDataReader reader = _cmdSelectUnassignedParentFiles.ExecuteReader();

            while (reader.Read())
            {
                patternMatch = false;
                int fileID = (int)reader["FileID"];
                thisFileName = reader["FileName"].ToString();

                if (lastFileName.Length > patternMatchLength)
                {
                    lastPattern = lastFileName.Substring(0, patternMatchLength);
                }
                else
                {
                    lastPattern = lastFileName;
                }

                if (thisFileName.Length > patternMatchLength)
                {
                    thisPattern = thisFileName.Substring(0, patternMatchLength);
                }
                else
                {
                    thisPattern = thisFileName;
                }

                if ((thisPattern.Length < lastPattern.Length) & (thisPattern.Length > 0))
                {
                    if (lastPattern.Substring(0, thisPattern.Length) == thisPattern)
                    {
                        patternMatch = true;
                    }
                }

                if ((thisPattern.Length > lastPattern.Length) & (lastPattern.Length > 0))
                {
                    if (thisPattern.Substring(0, lastPattern.Length) == lastPattern)
                    {
                        patternMatch = true;
                    }
                }

                if (thisPattern.Length == lastPattern.Length)
                {
                    if (thisPattern == lastPattern)
                    {
                        patternMatch = true;
                    }
                }

                if (patternMatch == true)
                {
                    AddProductFile(product, fileID, thisFileName);
                }
                else
                {

                    // Initiate a new transaction on change of product

                    Static.Database.CommitTransaction(Database.TransactionType.Active);
                    Static.Database.BeginTransaction(Database.TransactionType.Active);

                    Content.File parentFile = new Content.File(fileID);
                    Content.FileTextData fileTextData = dbaFile.DeriveFileTextData(parentFile);
                    product = new Content.Product();

                    product.Name = thisPattern; // Default the product name to the pattern match but we might be able to do better if we can process a read me file
                    if (fileTextData != null)
                    {
                        if (fileTextData.ProductName != null)
                        {
                            if (fileTextData.ProductName != String.Empty)
                            {
                                product.Name = fileTextData.ProductName; // Better product name...
                            }
                        }

                        for (int i = 0; i < fileTextData.VendorNameCodes.Count; i++)
                        {
                            Content.Creator creator = new Content.Creator();
                            creator.CreatorNameCode = fileTextData.VendorNameCodes[i];
                            if (!dbaContentCreators.IsCreatorRecorded(creator))
                            {
                                creator.CreatorTrueName = fileTextData.VendorNames[i];
                                if (fileTextData.ContactEmails.Count > (i + 1))
                                {
                                    creator.ContactEmail = fileTextData.ContactEmails[i];
                                }
                                dbaContentCreators.RecordContentCreator(creator);
                                product.Creators.Add(creator);
                            }
                            else
                            {
                                product.Creators.Add(creator);
                            }
                        }

                    }
                    
                    RecordProduct(product);
                    AddProductFile(product, fileID, thisFileName);
                }
                lastFileName = thisFileName;
            }

            // Final commit for the last product
            Static.Database.CommitTransaction(Database.TransactionType.Active);

            reader.Close();
            ArtContentManager.Static.Database.CommitTransaction(Database.TransactionType.ReadOnly);

            MessageBox.Show("Completed automatic assignment of products");

        }


        public static void RecordProduct(ArtContentManager.Content.Product Product)
        {
            SqlConnection DB = ArtContentManager.Static.Database.DBActive;

            if (_cmdAddProduct == null)
            {
                string insertProductSQL = "INSERT INTO Products (ProductName, IsPrimary) VALUES (@ProductName, @IsPrimary) SET @ProductID = SCOPE_IDENTITY();";
                _cmdAddProduct = new SqlCommand(insertProductSQL, DB);
                _cmdAddProduct.Parameters.Add("@ProductName", System.Data.SqlDbType.NVarChar,255);
                _cmdAddProduct.Parameters.Add("@IsPrimary", System.Data.SqlDbType.Bit);
                _cmdAddProduct.Parameters.Add("@ProductID", System.Data.SqlDbType.Int).Direction = System.Data.ParameterDirection.Output;
            }


            _cmdAddProduct.Transaction = ArtContentManager.Static.Database.CurrentTransaction(Database.TransactionType.Active);
            _cmdAddProduct.Parameters["@ProductName"].Value = Product.Name;
            _cmdAddProduct.Parameters["@IsPrimary"].Value = Product.IsPrimary;

            _cmdAddProduct.ExecuteScalar();

            Product.ID = (int)_cmdAddProduct.Parameters["@ProductID"].Value;

            if (_cmdAddProductCreator == null)
            {
                string insertProductCreatorSQL = "INSERT INTO ProductCreators (ProductID, CreatorID) VALUES (@ProductID, @CreatorID)";
                _cmdAddProductCreator = new SqlCommand(insertProductCreatorSQL, DB);
                _cmdAddProductCreator.Parameters.Add("@ProductID", System.Data.SqlDbType.Int);
                _cmdAddProductCreator.Parameters.Add("@CreatorID", System.Data.SqlDbType.Int);
            }

            _cmdAddProductCreator.Transaction = ArtContentManager.Static.Database.CurrentTransaction(Database.TransactionType.Active);
            _cmdAddProductCreator.Parameters["@ProductID"].Value = Product.ID;

            foreach (Content.Creator creator in Product.Creators)
            {
                _cmdAddProductCreator.Parameters["@CreatorID"].Value = creator.ID;
                _cmdAddProductCreator.ExecuteScalar();
            }

        }

        public static void AddProductFile(ArtContentManager.Content.Product Product, int fileID, string fileName)
        {

            int installerSeq;

            SqlConnection DB = ArtContentManager.Static.Database.DBActive;

            if (_cmdAddProductFile == null)
            {
                string insertProductFileSQL = "INSERT INTO ProductFiles (ProductID, InstallerSequence, FileID) VALUES (@ProductID, @InstallerSequence, @FileID)";
                _cmdAddProductFile = new SqlCommand(insertProductFileSQL, DB);

                _cmdAddProductFile.Parameters.Add("@ProductID", System.Data.SqlDbType.Int);
                _cmdAddProductFile.Parameters.Add("@InstallerSequence", System.Data.SqlDbType.SmallInt);
                _cmdAddProductFile.Parameters.Add("@FileID", System.Data.SqlDbType.Int);
            }

            Regex objNameSetPattern = new Regex("([0-9]){1,3}(?=(of|OF)([0-9]){1,3})");

            if (objNameSetPattern.IsMatch(fileName))
            {
                Match patternMatch = objNameSetPattern.Match(fileName);
                installerSeq = Int32.Parse(patternMatch.ToString());
            }
            else
            {
                installerSeq = 0;
            }

            _cmdAddProductFile.Transaction = ArtContentManager.Static.Database.CurrentTransaction(Database.TransactionType.Active);
            _cmdAddProductFile.Parameters["@ProductID"].Value = Product.ID;
            _cmdAddProductFile.Parameters["@InstallerSequence"].Value = installerSeq;
            _cmdAddProductFile.Parameters["@FileID"].Value = fileID;

            _cmdAddProductFile.ExecuteScalar();

        }

        static public bool UpdateProduct(Content.Product product)
        {

            try
            {
                SqlConnection DB = ArtContentManager.Static.Database.DBActive;

                if (_cmdUpdateProduct == null)
                {
                    string updateProductFileSQL = "UPDATE Product SET ProductName = @ProductName, IsPrimary = @IsPrimary, DatePurchased = @DatePurchased," + 
                                                  "MarketPlaceID = @MarketPlaceID, ProductURI = @ProductURI WHERE ProductID = @ProductID;";
                    _cmdUpdateProduct = new SqlCommand(updateProductFileSQL, DB);

                    _cmdUpdateProduct.Parameters.Add("@ProductID", System.Data.SqlDbType.Int);
                    _cmdUpdateProduct.Parameters.Add("@ProductName", System.Data.SqlDbType.NVarChar, 255);
                    _cmdUpdateProduct.Parameters.Add("@IsPrimary", System.Data.SqlDbType.Bit);
                    _cmdUpdateProduct.Parameters.Add("@DatePurchased", System.Data.SqlDbType.DateTime2);
                    _cmdUpdateProduct.Parameters.Add("@MarketPlaceID", System.Data.SqlDbType.Int);
                    _cmdUpdateProduct.Parameters.Add("@ProductURI", System.Data.SqlDbType.NVarChar, 255);

                }

                Static.Database.BeginTransaction(Database.TransactionType.Active);

                _cmdUpdateProduct.Transaction = ArtContentManager.Static.Database.CurrentTransaction(Database.TransactionType.Active);

                _cmdUpdateProduct.Parameters["@ProductID"].Value = product.ID;
                _cmdUpdateProduct.Parameters["@ProductName"].Value = product.Name;
                _cmdUpdateProduct.Parameters["@IsPrimary"].Value = product.IsPrimary;
                _cmdUpdateProduct.Parameters["@DatePurchased"].Value = product.DatePurchased;
                _cmdUpdateProduct.Parameters["@MarketPlaceID"].Value = product.MarketPlaceID;
                _cmdUpdateProduct.Parameters["@ProductURI"].Value = product.ProductURI;

                _cmdUpdateProduct.ExecuteScalar();

                // Here we will post updates to child files

                Static.Database.CommitTransaction(Database.TransactionType.Active);

                product.MoveImageFiles();

                return true;
            }
            catch(Exception e)
            {
                MessageBox.Show(e.Message);
                return false;
            }

        }
    }
}
