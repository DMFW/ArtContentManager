using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using System.Windows.Forms;
using System.Diagnostics;

namespace ArtContentManager.Static
{
    public static class Database
    {

        private static SqlConnection _DB;

        // Static dictionaries loaded for performance purposes during scanning 
        public static Dictionary<string, int> FileRoleExtensionsPrimary;
        public static Dictionary<string, int> FileRoles;
        public static Dictionary<string, string> ExcludedFiles;
        public static Dictionary<string, int> ReservedFiles;
       
        public static bool Open()
        {
            try
            {
                string dbConnection = Properties.Settings.Default.DatabaseConnection;
                _DB = new SqlConnection(dbConnection);
                _DB.Open();
                Trace.WriteLine("Database opened");
                return true;
            }
            catch(Exception e)
            {
                Trace.WriteLine(e.Message);
                MessageBox.Show(e.Message);
                return false;
            }
        }

        public static bool Close()
        {
            try
            {
                _DB.Close();
                Trace.WriteLine("Database closed");
                return true;
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.Message);
                MessageBox.Show(e.Message);
                return false;
            }
        }
  
        public static SqlConnection DB
        {
            get 
            {
                return _DB;
            }
        }

        public static void LoadScanReferenceData()
        {

            LoadFileRoles();
            Trace.WriteLine("File roles loaded");
            LoadFileRoleExtensionsPrimary();
            Trace.WriteLine("File role primary extensions loaded");
            LoadSpecialFiles();
            Trace.WriteLine("Special files loaded");
        }

        public static void UnloadScanReferenceData()
        {
            FileRoles=null;
            FileRoleExtensionsPrimary = null;
            ExcludedFiles = null;
            ReservedFiles = null;
        }

        private static void LoadFileRoles()
        {

            FileRoles = new Dictionary<string, int>();

            try
            {
                SqlDataReader myReader = null;
                SqlCommand myCommand = new SqlCommand("SELECT * from FileRoles", _DB);
                myReader = myCommand.ExecuteReader();
                while (myReader.Read())
                {
                    FileRoles.Add( myReader["RoleDescription"].ToString(), (Int16)myReader["RoleID"]);
                }
                myReader.Close();
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.ToString());
            }
        }

        private static void LoadFileRoleExtensionsPrimary()
        {

            FileRoleExtensionsPrimary = new Dictionary<string, int>();

            try
            {
                SqlDataReader myReader = null;
                SqlCommand myCommand = new SqlCommand("SELECT * from FileRoleExtensionsPrimary", _DB);
                myReader = myCommand.ExecuteReader();
                while (myReader.Read())
                {
                    FileRoleExtensionsPrimary.Add(myReader["Extension"].ToString(), (Int16)myReader["RoleID"]);
                }
                myReader.Close();
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.ToString());
            }
        }

        private static void LoadSpecialFiles()
        {

            ExcludedFiles = new Dictionary<string, string>();
            ReservedFiles = new Dictionary<string, int>();

            try
            {
                SqlDataReader myReader = null;
                SqlCommand myCommand = new SqlCommand("SELECT * from SpecialFiles", _DB);
                myReader = myCommand.ExecuteReader();
                while (myReader.Read())
                {
                    if ((bool)myReader["ExcludeFromScan"])
                    {
                        ExcludedFiles.Add(myReader["FileName"].ToString(), myReader["FileName"].ToString());
                    }
                    else
                    {
                        ReservedFiles.Add(myReader["FileName"].ToString(), (int)myReader["RoleID"]);
                    }
                }
                myReader.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine(e.ToString());
            }
        }

        public static void Reset()
        {

            try
            {

                DataColumn parentColumn;
                DataColumn childColumn;
                DataRelation relation;

                // This routine resets the database to "empty", except for the following code type entries
                // which are preserved across the reset as neutral and applicable to all re-populations.

                // Tags (These are free floating concepts)
                // SpecialTypes (These are fixed reserved data elements)
                // SpecialFiles (These are fixed reserved data elements)

                // ProcessRoles (These are semi-fixed but are re-ordered in the reset by WorkFlowOrder)
                // ProcessRoleExtensionsPrimary  (These are semi-fixed and re-ordered with ProcessRoles)
                // ProcessRoleExtensionsSecondary (These are semi-fixed and re-ordered with ProcessRoles)

                string sqlScanHistory = "TRUNCATE TABLE ScanHistory";
                SqlCommand cmdScanHistory = new SqlCommand(sqlScanHistory, _DB);
                cmdScanHistory.ExecuteNonQuery();

                string sqlArtProductCredits = "TRUNCATE TABLE ArtProductCredits";
                SqlCommand cmdArtProductCredits = new SqlCommand(sqlArtProductCredits, _DB);
                cmdArtProductCredits.ExecuteNonQuery();

                string sqlArtTags = "TRUNCATE TABLE ArtTags";
                SqlCommand cmdArtTags = new SqlCommand(sqlArtTags, _DB);
                cmdArtTags.ExecuteNonQuery();

                string sqlArt = "DELETE FROM Art";
                SqlCommand cmdArt = new SqlCommand(sqlArt, _DB);
                cmdArt.ExecuteNonQuery();

                cmdArt.CommandText = "DBCC CHECKIDENT (Art,RESEED, 0)";
                cmdArt.ExecuteNonQuery();

                string sqlProductInstaller = "TRUNCATE TABLE ProductInstaller";
                SqlCommand cmdProductInstaller = new SqlCommand(sqlProductInstaller, _DB);
                cmdProductInstaller.ExecuteNonQuery();

                string sqlProductFilesTruncate = "TRUNCATE TABLE ProductFiles";
                SqlCommand cmdProductFiles = new SqlCommand(sqlProductFilesTruncate, _DB);
                cmdProductFiles.ExecuteNonQuery();

                string sqlProductTagsTruncate = "TRUNCATE TABLE ProductTags";
                SqlCommand cmdProductTags = new SqlCommand(sqlProductTagsTruncate, _DB);
                cmdProductTags.ExecuteNonQuery();

                string sqlProductImagesTruncate = "TRUNCATE TABLE ProductImages";
                SqlCommand cmdProductImages = new SqlCommand(sqlProductImagesTruncate, _DB);
                cmdProductImages.ExecuteNonQuery();

                string sqlProductContentTypes = "TRUNCATE TABLE ProductContentTypes";
                SqlCommand cmdProductContentTypes = new SqlCommand(sqlProductContentTypes, _DB);
                cmdProductContentTypes.ExecuteNonQuery();

                string sqlContentTypes = "TRUNCATE TABLE ContentTypes";
                SqlCommand cmdContentTypes = new SqlCommand(sqlContentTypes, _DB);
                cmdContentTypes.ExecuteNonQuery();

                string sqlContentSpecialTypes = "TRUNCATE TABLE ContentSpecialTypes";
                SqlCommand cmdContentSpecialTypes = new SqlCommand(sqlContentSpecialTypes, _DB);
                cmdContentSpecialTypes.ExecuteNonQuery();

                string sqlDefinitionGroupID = "DELETE FROM DefinitionGroupID";
                SqlCommand cmdDefinitionGroupID = new SqlCommand(sqlDefinitionGroupID, _DB);
                cmdDefinitionGroupID.ExecuteNonQuery();

                string sqlProductGroupMembers = "TRUNCATE TABLE ProductGroupMembers";
                SqlCommand cmdProductGroupMembers = new SqlCommand(sqlProductGroupMembers, _DB);
                cmdProductGroupMembers.ExecuteNonQuery();

                string sqlProductGroups = "DELETE FROM ProductGroups";
                SqlCommand cmdProductGroups = new SqlCommand(sqlProductGroups, _DB);
                cmdProductGroups.ExecuteNonQuery();

                string sqlProducts = "DELETE FROM Products";
                SqlCommand cmdProducts = new SqlCommand(sqlProducts, _DB);
                cmdProducts.ExecuteNonQuery();

                cmdProducts.CommandText = "DBCC CHECKIDENT (Products,RESEED, 0)";
                cmdProducts.ExecuteNonQuery();

                string sqlFileLocationsTruncate = "TRUNCATE TABLE FileLocations";
                SqlCommand cmdFileLocations = new SqlCommand(sqlFileLocationsTruncate, _DB);
                cmdFileLocations.ExecuteNonQuery();

                string sqlFilesTruncate = "DELETE FROM Files";
                SqlCommand cmdFiles = new SqlCommand(sqlFilesTruncate, _DB);
                cmdFiles.ExecuteNonQuery();

                cmdFiles.CommandText = "DBCC CHECKIDENT (Files,RESEED, 0)";
                cmdFiles.ExecuteNonQuery();

                // Sort the ProcessRoles by work flow order and description and rewrite then
                // so that we get a more logical ordering in the table.
                // This is self indulgent because it is not strictly necessary but what
                // is a database tidy operation for, if not to please the designer? So humour me.

                // This is not going to work (yet) until the dataset is extended to take care of
                // the ProcessRoleExtensionsPrimary and ProcessRoleExtensionsSecondary tables

                string sqlProcessRoles = "Select * from ProcessRoles " +
                                      "Left Join ProcessRolesExtensionsPrimary on ProcessRoles.RoleID = ProcessRoleExtensionsPrimary.RoleID " +
                                      "Left Join ProcessRolesExtensionsSecondary  on ProcessRoles.RoleID = ProcessRoleExtensionsSecondary.RoleID " +
                                      "Order By ProcessRoles.WorkFlowOrder, ProcessRoles.RoleDescription";

                SqlCommand cmdProcessRoles = new SqlCommand(sqlProcessRoles, _DB);

                DataSet dsProcessRole = new DataSet("ProcessRoles");

                SqlDataAdapter daProcessRoles = new SqlDataAdapter();
                daProcessRoles.TableMappings.Add("ProcessRoles", "ProcessRoles");
                daProcessRoles.TableMappings.Add("ProcessRoleExtensionsPrimary", "ProcessRoleExtensionsPrimary");
                daProcessRoles.TableMappings.Add("ProcessRoleExtensionsSecondary", "ProcessRoleExtensionsSecondary");

                parentColumn = dsProcessRole.Tables["ProcessRoles"].Columns["RoleID"];

                childColumn = dsProcessRole.Tables["ProcessRolesExtensionsPrimary"].Columns["RoleID"];
                relation = new System.Data.DataRelation("ProcessRolePrimaryExtension", parentColumn, childColumn);
                dsProcessRole.Relations.Add(relation);

                childColumn = dsProcessRole.Tables["ProcessRolesExtensionsSecondary"].Columns["RoleID"];
                relation = new System.Data.DataRelation("ProcessRoleSecondaryExtension", parentColumn, childColumn);
                dsProcessRole.Relations.Add(relation);

                daProcessRoles.SelectCommand = cmdProcessRoles;
                daProcessRoles.Fill(dsProcessRole);

                DataTable tblProcessRoles = dsProcessRole.Tables["ProcessRoles"];

                // A delete will go here but only once I've confirmed the write works

                string sqlProcessRoleInsert = "INSERT INTO ProcessRoles (RoleDescription, WorkFlowOrder) VALUES (? , ?)";
                SqlCommand cmdProcessRoleInsert = new SqlCommand(sqlProcessRoleInsert, _DB);


                foreach (DataRow drFileRole in tblProcessRoles.Rows)
                {
                    Console.Write(drFileRole["WorkFlowOrder"].ToString() + "," + drFileRole["RoleDescription"].ToString());
                }

                // Bulk copy would be nice but we need to handle the changes keys which are the point of the reorganisation

                // using (SqlBulkCopy bulkCopy =
                //           new SqlBulkCopy(_DB))
                // {
                //    bulkCopy.DestinationTableName =
                //        "dbo.FileRoles";
                //
                //    bulkCopy.WriteToServer(tblFileRoles);
                // }

                MessageBox.Show("Database reset complete");
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                Trace.WriteLine(e.ToString());
            }

        }

    }
}
