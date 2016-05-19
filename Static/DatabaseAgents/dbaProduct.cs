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

        static SqlCommand _cmdSelectUnassignedParentFiles;
        static SqlCommand _cmdAddProduct;
        static SqlCommand _cmdAddProductFile;
        static SqlCommand _cmdAddProductCreator;

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

                        if (fileTextData.VendorNameCode != null)
                        {
                            if (fileTextData.VendorNameCode != String.Empty)
                            {
                                Content.Creator creator = new Content.Creator();
                                creator.CreatorNameCode = fileTextData.VendorNameCode;
                                if (!dbaContentCreators.IsCreatorRecorded(creator))
                                {
                                    creator.CreatorDirectoryName = fileTextData.VendorNameCode;
                                    creator.CreatorTrueName = fileTextData.VendorName;
                                    dbaContentCreators.RecordContentCreator(creator);
                                    product.Creators.Add(creator.ID, creator.ID);
                                }
                                else
                                {
                                    product.Creators.Add(creator.ID, creator.ID);
                                }
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

            foreach (int CreatorID in Product.Creators.Values)
            {
                _cmdAddProductCreator.Parameters["@CreatorID"].Value = CreatorID;
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
    }
}
