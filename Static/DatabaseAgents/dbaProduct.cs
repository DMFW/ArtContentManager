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
        static SqlCommand _cmdSelectUnassignedParentFiles;
        static SqlCommand _cmdAddProduct;
        static SqlCommand _cmdAddProductFile;

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

            SqlConnection DB = ArtContentManager.Static.Database.DB;

            Static.Database.BeginTransaction();

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
                _cmdSelectUnassignedParentFiles = new SqlCommand(sqlAutoProducts, DB);
            }

            _cmdSelectUnassignedParentFiles.Transaction = ArtContentManager.Static.Database.ActiveTransaction;
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

                if (thisPattern.Length < lastPattern.Length)
                {
                    if (lastPattern.Substring(0, thisPattern.Length) == thisPattern)
                    {
                        patternMatch = true;
                    }
                }

                if (thisPattern.Length > lastPattern.Length)
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

                    Static.Database.CommitTransaction();
                    Static.Database.BeginTransaction();

                    Content.File parentFile = new Content.File(fileID);
                    Content.FileTextData fileTextData = dbaFile.DeriveFileTextData(parentFile);
                    product = new Content.Product();

                    product.Name = thisPattern; // Default the product name to the pattern match but we might be able to do better if we can process a read me file
                    if (fileTextData != null)
                    {
                        if (fileTextData.ProductName != String.Empty)
                        {
                            product.Name = fileTextData.ProductName; // Better product name...
                        }

                        if (fileTextData.VendorNameCode != String.Empty)
                        {
                            Content.Creator creator = new Content.Creator();
                            creator.CreatorNameCode = fileTextData.VendorNameCode;
                            if (!dbaContentCreators.IsCreatorRecorded(creator))
                            {
                                creator.CreatorDirectoryName = fileTextData.VendorNameCode;
                                creator.CreatorTrueName = fileTextData.VendorName;
                                dbaContentCreators.RecordContentCreator(creator);
                                product.CreatorID = creator.ID;
                            }
                        }

                    }
                    
                    RecordProduct(product);
                    AddProductFile(product, fileID, thisFileName);
                }
            }

            // Final commit for the last product
            Static.Database.CommitTransaction();
            MessageBox.Show("Completed automatic assignment of products");

        }


        public static void RecordProduct(ArtContentManager.Content.Product Product)
        {
            SqlConnection DB = ArtContentManager.Static.Database.DB;

            if (_cmdAddProduct == null)
            {
                string insertProductSQL = "INSERT INTO Products (CreatorID, ProductName, ProductThumbnail, ReadMe, IsPrimary) VALUES (@CreatorID, @ProductName, @ProductThumbnail, @ReadMe, @IsPrimary) SET @ProductID = SCOPE_IDENTITY();";
                _cmdAddProduct = new SqlCommand(insertProductSQL, DB);
                _cmdAddProduct.Parameters.Add("@CreatorID", System.Data.SqlDbType.Int);
                _cmdAddProduct.Parameters.Add("@ProductName", System.Data.SqlDbType.Int);
                _cmdAddProduct.Parameters.Add("@ProductThumbnail", System.Data.SqlDbType.Image);
                _cmdAddProduct.Parameters.Add("@ReadMe", System.Data.SqlDbType.NVarChar);
                _cmdAddProduct.Parameters.Add("@IsPrimary", System.Data.SqlDbType.Binary);
                _cmdAddProduct.Parameters.Add("@ProductID", System.Data.SqlDbType.Int).Direction = System.Data.ParameterDirection.Output;
            }


            _cmdAddProduct.Transaction = ArtContentManager.Static.Database.ActiveTransaction;
            _cmdAddProduct.Parameters["@CreatorID"].Value = Product.CreatorID;
            _cmdAddProduct.Parameters["@ProductName"].Value = Product.Name;
            _cmdAddProduct.Parameters["@ProductThumbnail"].Value = Product.Thumbnail;
            _cmdAddProduct.Parameters["@ReadMe"].Value = Product.ReadMe;
            _cmdAddProduct.Parameters["@IsPrimary"].Value = Product.IsPrimary;

            _cmdAddProduct.ExecuteScalar();

            Product.ID = (int)_cmdAddProduct.Parameters["@ProductID"].Value;
        }

        public static void AddProductFile(ArtContentManager.Content.Product Product, int fileID, string fileName)
        {

            int installerSeq;

            SqlConnection DB = ArtContentManager.Static.Database.DB;

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

            _cmdAddProductFile.Transaction = ArtContentManager.Static.Database.ActiveTransaction;
            _cmdAddProductFile.Parameters["@ProductID"].Value = Product.ID;
            _cmdAddProductFile.Parameters["@InstallerSequence"].Value = installerSeq;
            _cmdAddProductFile.Parameters["@FileID"].Value = fileID;

            _cmdAddProductFile.ExecuteScalar();

        }
    }
}
