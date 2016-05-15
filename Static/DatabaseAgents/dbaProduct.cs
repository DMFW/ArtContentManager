using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Data.SqlClient;
using System.Diagnostics;

namespace ArtContentManager.Static.DatabaseAgents
{
    static class dbaProduct
    {
        static SqlCommand _cmdSelectUnassignedParentFiles;
        static SqlCommand _cmdAddProduct;

        public static void AutoAssignProducts()
        {

            int patternMatchLength;
            string thisFileName = "";
            string lastFileName = "";
            string lastPattern;
            string thisPattern;
            bool patternMatch;
            int productID;

            // Assign products to anything that hasn't yet been assigned

            SqlConnection DB = ArtContentManager.Static.Database.DB;

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

            SqlDataReader reader = _cmdSelectUnassignedParentFiles.ExecuteReader();

            while (reader.Read())
            {
                patternMatch = false;
                int ID = (int)reader["FileID"];
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

                if (patternMatch == false)
                {
                    productID = RecordProduct();
                }
            }
        }


        public static int RecordProduct(ArtContentManager.Content.Product Product)
        {
            SqlConnection DB = ArtContentManager.Static.Database.DB;

            if (_cmdAddProduct == null)
            {
                string insertProductSQL = "INSERT INTO Products (FileName, Extension, Checksum, Size, RoleID, ParentID, ExtractUnreadable) VALUES (@FileName, @Extension, @Checksum, @Size, @RoleID, @ParentID, @ExtractUnreadable) SET @ProductID = SCOPE_IDENTITY();";
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
            _cmdAddProduct.Parameters["@ProductThumbnail"].Value = File.Checksum;
            _cmdAddProduct.Parameters["@Size"].Value = File.Size;
            _cmdAddProduct.Parameters["@RoleID"].Value = File.RoleID;
            _cmdAddProduct.Parameters["@ExtractUnreadable"].Value = File.ExtractUnreadable;
            _cmdAddProduct.Parameters["@ParentID"].Value = File.ParentID;

            _cmdAddProduct.ExecuteScalar();

            Product.ID = (int)_cmdAddProduct.Parameters["@ProductID"].Value;
        }

    }
}
