﻿using System;
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
        static SqlCommand _cmdDeleteProductCreators;
        static SqlCommand _cmdUpdateProduct;
        static SqlCommand _cmdDeleteProductNotes;
        static SqlCommand _cmdMergeProductNotes;

        public static void Load(ArtContentManager.Content.Product Product, ProductLoadOptions loadOptions)
        {

            // This function assumes the ID is set within the Product object and on that basis loads 
            // whatever level of detail is requested in the load options.

            SqlConnection DB = ArtContentManager.Static.Database.DBReadOnly;

            if (_cmdReadProductByID == null)
            {
                string readProductByID_SQL = "SELECT * FROM Products LEFT JOIN ProductNotes ON Products.ProductID = ProductNotes.ProductID WHERE Products.ProductID = @ProductID";
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
                    Product.MarketPlaceID = reader["MarketPlaceID"] as int?;
                    Product.ProductURI = reader["ProductURI"].ToString();
                    Product.OrderURI = reader["OrderURI"].ToString();
                    Product.Currency = reader["Currency"].ToString();
                    Product.Price = reader["Price"] as decimal? ?? 0;
                    Product.Notes = reader["Notes"].ToString();
                }
                reader.Close();
            }

            if (loadOptions.installationFiles | loadOptions.contentFiles) { LoadProductInstallationFiles(Product); } // If we load the content files we have to load the installation files.
            if (loadOptions.contentFiles) { LoadProductContentFiles(Product); }
            if (loadOptions.creators) { LoadProductCreators(Product); }

        }

        #region PrivateLoadMethods

        private static void LoadProductInstallationFiles(ArtContentManager.Content.Product Product)
        {

            Product.InstallationFiles.Clear();

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

            Product.ContentFiles.Clear();
            Product.TextFiles.Clear();

            SqlConnection DB = ArtContentManager.Static.Database.DBReadOnly;

            if (_cmdReadProductContentFilesByID == null)
            {
                string readProductContentFilesByID_SQL = "SELECT FileID FROM Files WHERE ParentID = @ParentFileID";
                _cmdReadProductContentFilesByID = new SqlCommand(readProductContentFilesByID_SQL, DB);
                _cmdReadProductContentFilesByID.Parameters.Add("@ParentFileID", System.Data.SqlDbType.Int);
            }

            foreach (Content.File installationFile in Product.InstallationFiles)
            {
                _cmdReadProductContentFilesByID.Parameters["@ParentFileID"].Value = installationFile.ID;

                _cmdReadProductContentFilesByID.Transaction = ArtContentManager.Static.Database.CurrentTransaction(Database.TransactionType.ReadOnly);
                SqlDataReader reader = _cmdReadProductContentFilesByID.ExecuteReader();

                while (reader.Read())
                {

                    Content.File contentFile = new Content.File((int)reader["FileID"]);
                    contentFile.ParentFile = installationFile;

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

            Product.Creators.Clear();

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
                creator.CreatorID = (int)reader["CreatorID"];
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


        public static void RecordProduct(ArtContentManager.Content.Product product)
        {

            SqlConnection DB = ArtContentManager.Static.Database.DBActive;

            if (_cmdAddProduct == null)
            {
                string insertProductSQL = "INSERT INTO Products (ProductName, IsPrimary) VALUES (@ProductName, @IsPrimary) SET @ProductID = SCOPE_IDENTITY();";
                _cmdAddProduct = new SqlCommand(insertProductSQL, DB);
                _cmdAddProduct.Parameters.Add("@ProductName", System.Data.SqlDbType.NVarChar, 255);
                _cmdAddProduct.Parameters.Add("@IsPrimary", System.Data.SqlDbType.Bit);
                _cmdAddProduct.Parameters.Add("@ProductID", System.Data.SqlDbType.Int).Direction = System.Data.ParameterDirection.Output;
            }


            _cmdAddProduct.Transaction = ArtContentManager.Static.Database.CurrentTransaction(Database.TransactionType.Active);
            _cmdAddProduct.Parameters["@ProductName"].Value = product.Name;
            _cmdAddProduct.Parameters["@IsPrimary"].Value = product.IsPrimary;

            _cmdAddProduct.ExecuteScalar();

            product.ID = (int)_cmdAddProduct.Parameters["@ProductID"].Value;
            RecordProductCreators(product);

        }

        private static void RecordProductCreators(ArtContentManager.Content.Product product)
        {

            SqlConnection DB = ArtContentManager.Static.Database.DBActive;

            if (_cmdAddProductCreator == null)
            {
                string insertProductCreatorSQL = "INSERT INTO ProductCreators (ProductID, CreatorID) VALUES (@ProductID, @CreatorID)";
                _cmdAddProductCreator = new SqlCommand(insertProductCreatorSQL, DB);
                _cmdAddProductCreator.Parameters.Add("@ProductID", System.Data.SqlDbType.Int);
                _cmdAddProductCreator.Parameters.Add("@CreatorID", System.Data.SqlDbType.Int);
            }

            _cmdAddProductCreator.Transaction = ArtContentManager.Static.Database.CurrentTransaction(Database.TransactionType.Active);
            _cmdAddProductCreator.Parameters["@ProductID"].Value = product.ID;

            foreach (Content.Creator creator in product.Creators)
            {
                _cmdAddProductCreator.Parameters["@CreatorID"].Value = creator.CreatorID;
                _cmdAddProductCreator.ExecuteScalar();
            }
        }

        public static void AddProductFile(ArtContentManager.Content.Product product, int fileID, string fileName)
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
            _cmdAddProductFile.Parameters["@ProductID"].Value = product.ID;
            _cmdAddProductFile.Parameters["@InstallerSequence"].Value = installerSeq;
            _cmdAddProductFile.Parameters["@FileID"].Value = fileID;

            _cmdAddProductFile.ExecuteScalar();

        }

        static public void UpdateProduct(Content.Product product)
        {
            SqlConnection DB = ArtContentManager.Static.Database.DBActive;

            if (_cmdUpdateProduct == null)
            {
                string updateProductFileSQL = "UPDATE Products SET ProductName = @ProductName, IsPrimary = @IsPrimary, DatePurchased = @DatePurchased," +
                                                "MarketPlaceID = @MarketPlaceID, ProductURI = @ProductURI, OrderURI = @OrderURI, " +
                                                "Currency = @Currency, Price = @Price WHERE ProductID = @ProductID;";
                _cmdUpdateProduct = new SqlCommand(updateProductFileSQL, DB);

                _cmdUpdateProduct.Parameters.Add("@ProductID", System.Data.SqlDbType.Int);
                _cmdUpdateProduct.Parameters.Add("@ProductName", System.Data.SqlDbType.NVarChar, 255);
                _cmdUpdateProduct.Parameters.Add("@IsPrimary", System.Data.SqlDbType.Bit);
                _cmdUpdateProduct.Parameters.Add("@DatePurchased", System.Data.SqlDbType.DateTime);
                _cmdUpdateProduct.Parameters.Add("@MarketPlaceID", System.Data.SqlDbType.Int);
                _cmdUpdateProduct.Parameters.Add("@ProductURI", System.Data.SqlDbType.NVarChar, 255);
                _cmdUpdateProduct.Parameters.Add("@OrderURI", System.Data.SqlDbType.NVarChar, 255);
                _cmdUpdateProduct.Parameters.Add("@Currency", System.Data.SqlDbType.Char, 3);
                _cmdUpdateProduct.Parameters.Add("@Price", System.Data.SqlDbType.SmallMoney);

            }

            _cmdUpdateProduct.Transaction = ArtContentManager.Static.Database.CurrentTransaction(Database.TransactionType.Active);

            _cmdUpdateProduct.Parameters["@ProductID"].Value = product.ID;
            _cmdUpdateProduct.Parameters["@ProductName"].Value = product.Name;
            _cmdUpdateProduct.Parameters["@IsPrimary"].Value = product.IsPrimary;
            _cmdUpdateProduct.Parameters["@DatePurchased"].Value = (object)product.DatePurchased ?? DBNull.Value;
            _cmdUpdateProduct.Parameters["@MarketPlaceID"].Value = (object)product.MarketPlaceID ?? DBNull.Value;
            _cmdUpdateProduct.Parameters["@ProductURI"].Value = (object)product.ProductURI ?? DBNull.Value;
            _cmdUpdateProduct.Parameters["@OrderURI"].Value = (object)product.OrderURI ?? DBNull.Value;
            _cmdUpdateProduct.Parameters["@Currency"].Value = product.Currency;
            _cmdUpdateProduct.Parameters["@Price"].Value = product.Price;

            _cmdUpdateProduct.ExecuteScalar();

            UpdateProductNotes(product);
        }

        static private void UpdateProductNotes(Content.Product product)
        {

            // Product notes do not exist at the time of product creation, so we only need to consider them 
            // in the update process for the whole product when we will either add them, delete them or update them.

            SqlConnection DB = ArtContentManager.Static.Database.DBActive;

            if (product.Notes.Trim() == "")
            {
                if (_cmdDeleteProductNotes == null)
                {
                    string deleteProductNotesSQL = "DELETE ProductNotes WHERE ProductID = @ProductID;";
                    _cmdDeleteProductNotes = new SqlCommand(deleteProductNotesSQL, DB);
                }
                _cmdDeleteProductNotes.Transaction = ArtContentManager.Static.Database.CurrentTransaction(Database.TransactionType.Active);
                _cmdDeleteProductNotes.Parameters["@ProductID"].Value = product.ID;
                _cmdDeleteProductNotes.ExecuteScalar();
            }
            else
            {
                if (_cmdMergeProductNotes == null)
                {
                    string mergeProductNotesSQL = "MERGE ProductNotes AS [Target] " +
                                                  "USING (SELECT @ProductID as ProductID, @Notes as Notes) AS [Source] ON [Target].ProductID = [Source].ProductID " +
                                                  "WHEN MATCHED THEN UPDATE SET Notes = @Notes " +
                                                  "WHEN NOT MATCHED THEN INSERT (ProductID, Notes) VALUES (@ProductID, @Notes); ";
                    _cmdMergeProductNotes = new SqlCommand(mergeProductNotesSQL, DB);

                    _cmdMergeProductNotes.Parameters.Add("@ProductID", System.Data.SqlDbType.Int);
                    _cmdMergeProductNotes.Parameters.Add("@Notes", System.Data.SqlDbType.NVarChar);
                }
                _cmdMergeProductNotes.Transaction = ArtContentManager.Static.Database.CurrentTransaction(Database.TransactionType.Active);
                _cmdMergeProductNotes.Parameters["@ProductID"].Value = product.ID;
                _cmdMergeProductNotes.Parameters["@Notes"].Value = product.Notes;
                _cmdMergeProductNotes.ExecuteScalar();
            }
        }

        public static void ReplaceProductCreators(Content.Product product)
        {

            SqlConnection DB = ArtContentManager.Static.Database.DBActive;

            if (_cmdDeleteProductCreators == null)
            {
                string deleteProductCreatorsSQL = "DELETE FROM ProductCreators WHERE ProductID = @ProductID;";
                _cmdDeleteProductCreators = new SqlCommand(deleteProductCreatorsSQL, DB);

                _cmdDeleteProductCreators.Parameters.Add("@ProductID", System.Data.SqlDbType.Int);
            }

            _cmdDeleteProductCreators.Parameters["@ProductID"].Value = product.ID;

            _cmdDeleteProductCreators.Transaction = ArtContentManager.Static.Database.CurrentTransaction(Database.TransactionType.Active);
            _cmdDeleteProductCreators.ExecuteScalar();

            RecordProductCreators(product);
        }

        public static void MapProductCreator(int sourceCreatorID, int targetCreatorID)
        {

            // Maps the source into the target across all products as a duplicate
            // The original is NOT deleted because we may be splitting the source across multiple creators
            // The delete operation needs to happen only when this is completed.
            // Note the SQL index on product creators has IGNORE_DUP_KEY set ON for the benefit of this routine.
            // We must not have duplicate keys but at the same time we don't care of we attempt to insert one that 
            // is a duplicate, we just want processing to continue and the INSERT to be ignored without 
            // rolling back the transaction. 

            // Don't use a try catch here. We want to handle errors at the level above, not consume them here.

            SqlConnection DB = ArtContentManager.Static.Database.DBActive;

            string mapSQL = string.Format("INSERT INTO ProductCreators SELECT ProductID, {0} FROM ProductCreators WHERE CreatorID = {1}", targetCreatorID, sourceCreatorID);

            SqlCommand cmdMapProductCreator = new SqlCommand(mapSQL, DB);
            cmdMapProductCreator.Transaction = ArtContentManager.Static.Database.CurrentTransaction(Database.TransactionType.Active);

            cmdMapProductCreator.Transaction = ArtContentManager.Static.Database.CurrentTransaction(Database.TransactionType.Active);
            cmdMapProductCreator.ExecuteScalar();
                
        }

        public static void RemoveCreatorFromAllProducts(int sourceCreatorID)
        {

            // Remove all product creator records for a specified creator
            // Don't put a try catch here. We handle errors at the level above.

            SqlConnection DB = ArtContentManager.Static.Database.DBActive;  

            string deleteSQL = string.Format("DELETE FROM ProductCreators WHERE CreatorID = {0}", sourceCreatorID);

            SqlCommand cmdRemoveCreatorFromAllProducts = new SqlCommand(deleteSQL, DB);
            cmdRemoveCreatorFromAllProducts.Transaction = ArtContentManager.Static.Database.CurrentTransaction(Database.TransactionType.Active);
            cmdRemoveCreatorFromAllProducts.ExecuteScalar();

        }

    }
}
