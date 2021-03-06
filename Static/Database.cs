﻿using System;
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

        private static string _DBConnectionString;
        private static string _DBInitialCatalog;

        private static SqlConnection _DBActive;
        private static SqlTransaction _trnActive;

        private static SqlConnection _DBReadOnly;
        private static SqlTransaction _trnReadOnly;

        private static int _trnLevelActive = 0;
        private static int _trnLevelReadOnly = 0;
        private static bool _scanReferenceDataLoaded = false;

        public enum ResetLevel
        {
            AllDynamicData,
            ProductData
        }

        public enum TransactionType
        {
            Active,
            ReadOnly
        }

        // Static dictionaries loaded for performance purposes during scanning

        public static Dictionary<string, int> ProcessRoleExtensionsPrimary;
        public static Dictionary<string, int> ProcessRoles;
        public static Dictionary<string, string> ExcludedFiles;
        public static Dictionary<string, int> ReservedFiles;
        public static Dictionary<string, Content.InstallationType> InstallationTypes;
        public static Dictionary<string, Content.Installation> Installations;

        public static SqlConnection DBActive
        {
            get { return _DBActive; }
        }

        public static SqlConnection DBReadOnly
        {
            get { return _DBReadOnly; }
        }

        public static bool Open()
        {

            // Open with the default connection string
            string dbConnection = Properties.Settings.Default.DatabaseConnection;
            return Open(dbConnection);
             
        }

        public static bool Open(string connectionString)
        {
            // Open with any specified connection string

            try
            {

                _DBConnectionString = connectionString;
                SqlConnectionStringBuilder builder = new SqlConnectionStringBuilder(_DBConnectionString);
                _DBInitialCatalog = builder.InitialCatalog;

                _DBActive = new SqlConnection(_DBConnectionString);
                _DBActive.Open();

                _DBReadOnly = new SqlConnection(_DBConnectionString);
                _DBReadOnly.Open();

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
                if (_DBActive != null)
                {
                    _DBActive.Close();
                }
                if (_DBReadOnly != null)
                {
                    _DBReadOnly.Close();
                }
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

        public static bool ScanReferenceDataLoaded
        {
            get { return _scanReferenceDataLoaded; }
        }

        public static void BeginTransaction(TransactionType tt)
        {
            switch (tt)
            {
                case TransactionType.Active:
                    if (_trnActive != null)
                    {
                        Trace.Write("Attempting to start new transation when the previous one is not disposed of");
                    }
                    else
                    {
                        _trnLevelActive++;
                        _trnActive = ArtContentManager.Static.Database.DBActive.BeginTransaction(System.Data.IsolationLevel.ReadUncommitted);
                    }
                    break;
                case TransactionType.ReadOnly:
                    if (_trnReadOnly != null)
                    {
                        Trace.Write("Attempting to start new transation when the previous one is not disposed of");
                    }
                    else
                    {
                        _trnLevelReadOnly++;
                        _trnReadOnly = ArtContentManager.Static.Database.DBReadOnly.BeginTransaction(System.Data.IsolationLevel.ReadUncommitted);
                    }
                    break;
            }

       }

        public static SqlTransaction CurrentTransaction(TransactionType tt)
        {
  
            switch (tt)
            {
                case TransactionType.Active:
                    return _trnActive;

                case TransactionType.ReadOnly:
                    return _trnReadOnly;
        
                default:
                    return null;
            }
              
        }

        public static void CommitTransaction(TransactionType tt)
        {

            switch (tt)
            {
                case TransactionType.Active:

                    _trnActive.Commit();
                    _trnActive.Dispose();
                    _trnActive = null;
                    _trnLevelActive--;
                    break;

                case TransactionType.ReadOnly:

                    _trnReadOnly.Commit();
                    _trnReadOnly.Dispose();
                    _trnReadOnly = null;
                    _trnLevelReadOnly--;
                    break;
            }

        }

        public static void RollbackTransaction(TransactionType tt)
        {

            switch (tt)
            {
                case TransactionType.Active:

                    _trnActive.Rollback();
                    _trnActive.Dispose();
                    _trnActive = null;
                    _trnLevelActive--;
                    break;

                case TransactionType.ReadOnly:

                    _trnReadOnly.Rollback();
                    _trnReadOnly.Dispose();
                    _trnReadOnly = null;
                    _trnLevelReadOnly--;
                    break;

            }
        }

        public static void LoadScanReferenceData()
        {
            LoadProcessRoles();
            Trace.WriteLine("Process roles loaded");
            LoadProcessRoleExtensionsPrimary();
            Trace.WriteLine("Process role primary extensions loaded");
            LoadSpecialFiles();
            Trace.WriteLine("Special files loaded");
            LoadInstallationTypes();
            Trace.WriteLine("Installation types loaded");
            LoadInstallations();
            Trace.WriteLine("Installations loaded");
            _scanReferenceDataLoaded = true;
        }

        public static void UnloadScanReferenceData()
        {
            ProcessRoles=null;
            ProcessRoleExtensionsPrimary = null;
            ExcludedFiles = null;
            ReservedFiles = null;
            InstallationTypes = null;
            Installations = null;
            _scanReferenceDataLoaded = false;
        }

        private static void LoadProcessRoles()
        {

            ProcessRoles = new Dictionary<string, int>();

            try
            {
                SqlDataReader myReader = null;
                SqlCommand myCommand = new SqlCommand("SELECT * from ProcessRoles", _DBReadOnly);
                myReader = myCommand.ExecuteReader();
                while (myReader.Read())
                {
                    ProcessRoles.Add( myReader["RoleDescription"].ToString(), (Int16)myReader["RoleID"]);
                }
                myReader.Close();
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.ToString());
            }
        }

        private static void LoadProcessRoleExtensionsPrimary()
        {

            ProcessRoleExtensionsPrimary = new Dictionary<string, int>();

            try
            {
                SqlDataReader myReader = null;
                SqlCommand myCommand = new SqlCommand("SELECT * from ProcessRoleExtensionsPrimary", _DBReadOnly);
                myReader = myCommand.ExecuteReader();
                while (myReader.Read())
                {
                    ProcessRoleExtensionsPrimary.Add(myReader["Extension"].ToString(), (Int16)myReader["RoleID"]);
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
                SqlCommand myCommand = new SqlCommand("SELECT * from SpecialFiles", _DBReadOnly);
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

        private static void LoadInstallationTypes()
        {

            InstallationTypes = new Dictionary<string, Content.InstallationType>();

            try
            {
                SqlDataReader rdrIT = null;
                SqlCommand cmdIT = new SqlCommand("SELECT * from InstallationTypes", _DBReadOnly);
                rdrIT = cmdIT.ExecuteReader();
                while (rdrIT.Read())
                {
                    Content.InstallationType installationType = new Content.InstallationType();
                    installationType.TypeID = (int)rdrIT["InstallationTypeID"];
                    installationType.SoftwareType = rdrIT["SoftwareType"].ToString();
                    installationType.IdentifyingDirectoryName = rdrIT["IdentifyingDirectoryName"].ToString();

                    SqlDataReader rdrITCR = null;
                    SqlCommand cmdITCR = new SqlCommand("SELECT * from InstallationTypeCategory", _DBReadOnly);
                    rdrITCR = cmdITCR.ExecuteReader();
                    while (rdrITCR.Read())
                    {
                        Content.InstallationType.Category category = new Content.InstallationType.Category();

                        category.RelativePath = rdrITCR["RelativePath"].ToString();
                        category.Name = rdrITCR["CategoryName"].ToString();
                         
                        installationType.Categories.Add(category.RelativePath, category);
                    }
                    rdrITCR.Close();

                    InstallationTypes.Add(rdrIT["IdentifyingDirectoryName"].ToString(), installationType);
                }
                rdrIT.Close();
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.ToString());
            }
        }

        private static void LoadInstallations()
        {

            Installations = new Dictionary<string, Content.Installation>();

            try
            {
                SqlDataReader myReader = null;
                SqlCommand myCommand = new SqlCommand("SELECT * from Installations INNER JOIN InstallationTypes ON Installations.InstallationTypeID = InstallationTypes.InstallationTypeID", _DBReadOnly);
                myReader = myCommand.ExecuteReader();
                while (myReader.Read())
                {
                    Content.Installation installation = new Content.Installation();
                    installation.RootID = (int)myReader["InstallationRootID"];
                    installation.RootPath = myReader["InstallationRootPath"].ToString();

                    if (InstallationTypes.ContainsKey(myReader["IdentifyingDirectoryName"].ToString()))
                    {
                        // Set the type from the previously loaded distcionary of types keyed by identifying directory name
                        installation.Type = InstallationTypes[myReader["IdentifyingDirectoryName"].ToString()];
                    }
                    else
                    {
                        // Something has gone seriously wrong here with installations and their types.
                        Trace.WriteLine("Installation " + installation.RootID + " is not of a recognised type.");
                    }
                }
                myReader.Close();
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.ToString());
            }
        }


        public static void Reset(ResetLevel resetLevel)
        {

            try
            {

                // This routine resets the database to "empty", except for the following code type entries
                // which are preserved across the reset as neutral and applicable to all re-populations.

                // Tags (These are free floating concepts)
                // SpecialTypes (These are fixed reserved data elements)
                // SpecialFiles (These are fixed reserved data elements)

                // ProcessRoles (These are semi-fixed but are re-ordered in the reset by WorkFlowOrder)
                // ProcessRoleExtensionsPrimary  (These are semi-fixed and re-ordered with ProcessRoles)
                // ProcessRoleExtensionsSecondary (These are semi-fixed and re-ordered with ProcessRoles)

                if (resetLevel == ResetLevel.AllDynamicData)
                {
                    string sqlScanHistory = "TRUNCATE TABLE ScanHistory";
                    SqlCommand cmdScanHistory = new SqlCommand(sqlScanHistory, _DBActive);
                    cmdScanHistory.ExecuteNonQuery();

                    cmdScanHistory.CommandText = "DBCC CHECKIDENT (ScanHistory,RESEED, 0)";
                    cmdScanHistory.ExecuteNonQuery();

                    string sqlArtProductCredits = "TRUNCATE TABLE ArtProductCredits";
                    SqlCommand cmdArtProductCredits = new SqlCommand(sqlArtProductCredits, _DBActive);
                    cmdArtProductCredits.ExecuteNonQuery();

                    string sqlArtTags = "TRUNCATE TABLE ArtTags";
                    SqlCommand cmdArtTags = new SqlCommand(sqlArtTags, _DBActive);
                    cmdArtTags.ExecuteNonQuery();

                    string sqlArt = "DELETE FROM Art";
                    SqlCommand cmdArt = new SqlCommand(sqlArt, _DBActive);
                    cmdArt.ExecuteNonQuery();

                    cmdArt.CommandText = "DBCC CHECKIDENT (Art,RESEED, 0)";
                    cmdArt.ExecuteNonQuery();

                    string sqlInstallations = "DELETE FROM Installations";
                    SqlCommand cmdInstallations = new SqlCommand(sqlInstallations, _DBActive);
                    cmdInstallations.ExecuteNonQuery();

                    cmdInstallations.CommandText = "DBCC CHECKIDENT (Installations,RESEED, 0)";
                    cmdInstallations.ExecuteNonQuery();

                }

                string sqlProductInstaller = "TRUNCATE TABLE ProductInstaller";
                SqlCommand cmdProductInstaller = new SqlCommand(sqlProductInstaller, _DBActive);
                cmdProductInstaller.ExecuteNonQuery();

                string sqlProductFilesTruncate = "TRUNCATE TABLE ProductFiles";
                SqlCommand cmdProductFiles = new SqlCommand(sqlProductFilesTruncate, _DBActive);
                cmdProductFiles.ExecuteNonQuery();

                string sqlProductTagsTruncate = "TRUNCATE TABLE ProductTags";
                SqlCommand cmdProductTags = new SqlCommand(sqlProductTagsTruncate, _DBActive);
                cmdProductTags.ExecuteNonQuery();

                string sqlProductContentTypesFractured = "TRUNCATE TABLE ProductContentTypesFractured";
                SqlCommand cmdProductContentTypesFractured = new SqlCommand(sqlProductContentTypesFractured, _DBActive);
                cmdProductContentTypesFractured.ExecuteNonQuery();

                string sqlProductContentTypesOrganised = "TRUNCATE TABLE ProductContentTypesOrganised";
                SqlCommand cmdProductContentTypesOrganised = new SqlCommand(sqlProductContentTypesOrganised, _DBActive);
                cmdProductContentTypesOrganised.ExecuteNonQuery();

                string sqlContentLocations = "TRUNCATE TABLE ContentLocations";
                SqlCommand cmdContentLocations = new SqlCommand(sqlContentLocations, _DBActive);
                cmdContentLocations.ExecuteNonQuery();

                string sqlContentSpecialTypes = "TRUNCATE TABLE ContentSpecialTypes";
                SqlCommand cmdContentSpecialTypes = new SqlCommand(sqlContentSpecialTypes, _DBActive);
                cmdContentSpecialTypes.ExecuteNonQuery();

                string sqlProductGroupMembers = "TRUNCATE TABLE ProductGroupMembers";
                SqlCommand cmdProductGroupMembers = new SqlCommand(sqlProductGroupMembers, _DBActive);
                cmdProductGroupMembers.ExecuteNonQuery();

                string sqlProductGroups = "DELETE FROM ProductGroups";
                SqlCommand cmdProductGroups = new SqlCommand(sqlProductGroups, _DBActive);
                cmdProductGroups.ExecuteNonQuery();

                string sqlProductNotes = "TRUNCATE TABLE ProductNotes";
                SqlCommand cmdProductNotes = new SqlCommand(sqlProductNotes, _DBActive);
                cmdProductNotes.ExecuteNonQuery();

                string sqlProducts = "DELETE FROM Products";
                SqlCommand cmdProducts = new SqlCommand(sqlProducts, _DBActive);
                cmdProducts.ExecuteNonQuery();

                cmdProducts.CommandText = "DBCC CHECKIDENT (Products,RESEED, 0)";
                cmdProducts.ExecuteNonQuery();

                string sqlProductCreators = "DELETE FROM ProductCreators";
                SqlCommand cmdProductCreators = new SqlCommand(sqlProductCreators, _DBActive);
                cmdProductCreators.ExecuteNonQuery();

                string sqlContentCreators = "DELETE FROM ContentCreators";
                SqlCommand cmdContentCreators = new SqlCommand(sqlContentCreators, _DBActive);
                cmdContentCreators.ExecuteNonQuery();

                cmdContentCreators.CommandText = "DBCC CHECKIDENT (ContentCreators,RESEED, 0)";
                cmdContentCreators.ExecuteNonQuery();

                if (resetLevel == ResetLevel.AllDynamicData)
                {

                    string sqlFilterExpressionRows = "DELETE FROM FilterExpressionRows";
                    SqlCommand cmdFilterExpressionRows = new SqlCommand(sqlFilterExpressionRows, _DBActive);
                    cmdFilterExpressionRows.ExecuteNonQuery();

                    string sqlFilterExpressionFavourites = "TRUNCATE TABLE FilterExpressionFavourites";
                    SqlCommand cmdFilterExpressionFavourites = new SqlCommand(sqlFilterExpressionFavourites, _DBActive);
                    cmdFilterExpressionFavourites.ExecuteNonQuery();

                    cmdFilterExpressionFavourites.CommandText = "DBCC CHECKIDENT (FilterExpressionFavourites,RESEED, 0)";
                    cmdFilterExpressionFavourites.ExecuteNonQuery();

                    string sqlOrganisationScheme = "DELETE FROM OrganisationScheme";
                    SqlCommand cmdOrganisationScheme = new SqlCommand(sqlOrganisationScheme, _DBActive);
                    cmdOrganisationScheme.ExecuteNonQuery();

                    cmdOrganisationScheme.CommandText = "DBCC CHECKIDENT (OrganisationScheme,RESEED, 0)";
                    cmdOrganisationScheme.ExecuteNonQuery();

                    sqlOrganisationScheme = "INSERT INTO OrganisationScheme (Description, DateCreated) VALUES('Derived From Scanning', @CurrentTime)";
                    cmdOrganisationScheme.CommandText = sqlOrganisationScheme;
                    cmdOrganisationScheme.Parameters.Add("@CurrentTime", SqlDbType.DateTime);
                    cmdOrganisationScheme.Parameters["@CurrentTime"].Value = DateTime.Now;
                    cmdOrganisationScheme.ExecuteNonQuery();

                    string sqlFileLocationsTruncate = "TRUNCATE TABLE FileLocations";
                    SqlCommand cmdFileLocations = new SqlCommand(sqlFileLocationsTruncate, _DBActive);
                    cmdFileLocations.ExecuteNonQuery();

                    string sqlDefaultFileRelativeLocations = "TRUNCATE TABLE DefaultFileRelativeLocations";
                    SqlCommand cmdDefaultFileRelativeLocations = new SqlCommand(sqlDefaultFileRelativeLocations, _DBActive);
                    cmdDefaultFileRelativeLocations.ExecuteNonQuery();

                    string sqlFileImagesTruncate = "TRUNCATE TABLE FileImages";
                    SqlCommand cmdFileImagesTruncate = new SqlCommand(sqlFileImagesTruncate, _DBActive);
                    cmdFileImagesTruncate.ExecuteNonQuery();

                    string sqlFileTextNotesTruncate = "TRUNCATE TABLE FileTextNotes";
                    SqlCommand cmdFileTextNotesTruncate = new SqlCommand(sqlFileTextNotesTruncate, _DBActive);
                    cmdFileTextNotesTruncate.ExecuteNonQuery();

                    string sqlFilesDelete = "DELETE FROM Files";
                    SqlCommand cmdFiles = new SqlCommand(sqlFilesDelete, _DBActive);
                    cmdFiles.ExecuteNonQuery();

                    cmdFiles.CommandText = "DBCC CHECKIDENT (Files,RESEED, 0)";
                    cmdFiles.ExecuteNonQuery();

                    ReorganiseProcessRoles();
                }

                MessageBox.Show("Database reset complete");
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                Trace.WriteLine(e.ToString());
            }

        }

        private static void ReorganiseProcessRoles()
        {

            // Sort the ProcessRoles by work flow order and description and rewrite then
            // so that we get a more logical ordering in the table.
            // This is self indulgent because it is not strictly necessary but what
            // is a database tidy operation for, if not to please the designer? So humour me.

            DataColumn parentColumn;
            DataColumn childColumn;
            DataRelation relation;

            try
            {
                #region LoadDataSet

                string sqlProcessRoles = "Select * from ProcessRoles Order By ProcessRoles.WorkFlowOrder, ProcessRoles.RoleDescription";
                string sqlProcessRoleExtensionsPrimary = "Select * from ProcessRoleExtensionsPrimary";
                string sqlProcessRoleExtensionsSecondary = "Select * from ProcessRoleExtensionsSecondary";
                string sqlSpecialFiles = "Select * from SpecialFiles";

                SqlCommand cmdProcessRoles = new SqlCommand(sqlProcessRoles, _DBActive);
                SqlCommand cmdProcessRoleExtensionsPrimary = new SqlCommand(sqlProcessRoleExtensionsPrimary, _DBActive);
                SqlCommand cmdProcessRoleExtensionsSecondary = new SqlCommand(sqlProcessRoleExtensionsSecondary, _DBActive);
                SqlCommand cmdSpecialFiles = new SqlCommand(sqlSpecialFiles, _DBActive);

                DataSet dsProcessRoleSet = new DataSet("ProcessRoleSet");

                SqlDataAdapter daProcessRoles = new SqlDataAdapter();
                daProcessRoles.SelectCommand = cmdProcessRoles;
                daProcessRoles.Fill(dsProcessRoleSet,"ProcessRoles");

                SqlDataAdapter daProcessRoleExtensionsPrimary = new SqlDataAdapter();
                daProcessRoleExtensionsPrimary.SelectCommand = cmdProcessRoleExtensionsPrimary;
                daProcessRoleExtensionsPrimary.Fill(dsProcessRoleSet, "ProcessRoleExtensionsPrimary");

                SqlDataAdapter daProcessRoleExtensionsSecondary = new SqlDataAdapter();
                daProcessRoleExtensionsSecondary.SelectCommand = cmdProcessRoleExtensionsSecondary;
                daProcessRoleExtensionsSecondary.Fill(dsProcessRoleSet, "ProcessRoleExtensionsSecondary");

                SqlDataAdapter daSpecialFiles = new SqlDataAdapter();
                daSpecialFiles.SelectCommand = cmdSpecialFiles;
                daSpecialFiles.Fill(dsProcessRoleSet, "SpecialFiles");

                parentColumn = dsProcessRoleSet.Tables["ProcessRoles"].Columns["RoleID"];

                childColumn = dsProcessRoleSet.Tables["ProcessRoleExtensionsPrimary"].Columns["RoleID"];
                relation = new System.Data.DataRelation("ProcessRolePrimaryExtension", parentColumn, childColumn);
                dsProcessRoleSet.Relations.Add(relation);

                childColumn = dsProcessRoleSet.Tables["ProcessRoleExtensionsSecondary"].Columns["RoleID"];
                relation = new System.Data.DataRelation("ProcessRoleSecondaryExtension", parentColumn, childColumn);
                dsProcessRoleSet.Relations.Add(relation);

                childColumn = dsProcessRoleSet.Tables["SpecialFiles"].Columns["RoleID"];
                relation = new System.Data.DataRelation("SpecialFilesForRole", parentColumn, childColumn);
                dsProcessRoleSet.Relations.Add(relation);

                #endregion

                #region PhysicalDelete

                string sqlProcessRoleExtensionsPrimaryDelete = "DELETE FROM ProcessRoleExtensionsPrimary";
                SqlCommand cmdDeleteProcessRoleExtensionsPrimary = new SqlCommand(sqlProcessRoleExtensionsPrimaryDelete, _DBActive);
                cmdDeleteProcessRoleExtensionsPrimary.ExecuteNonQuery();

                string sqlProcessRoleExtensionsSecondaryDelete = "DELETE FROM ProcessRoleExtensionsSecondary";
                SqlCommand cmdDeleteProcessRoleExtensionsSecondary = new SqlCommand(sqlProcessRoleExtensionsSecondaryDelete, _DBActive);
                cmdDeleteProcessRoleExtensionsSecondary.ExecuteNonQuery();

                string sqlSpecialFilesDelete = "DELETE FROM SpecialFiles";
                SqlCommand cmdDeleteSpecialFiles = new SqlCommand(sqlSpecialFilesDelete, _DBActive);
                cmdDeleteSpecialFiles.ExecuteNonQuery();

                string sqlProcessRolesDelete = "DELETE FROM ProcessRoles";
                SqlCommand cmdDeleteProcessRoles = new SqlCommand(sqlProcessRolesDelete, _DBActive);
                cmdDeleteProcessRoles.ExecuteNonQuery();

                string sqlProcessRolesReset = "DBCC CHECKIDENT (ProcessRoles,RESEED, 0)";
                SqlCommand cmdResetProcessRoles = new SqlCommand(sqlProcessRolesReset, _DBActive);
                cmdResetProcessRoles.ExecuteNonQuery();

                #endregion

                #region Insert

                string sqlProcessRoleInsert = "INSERT INTO ProcessRoles (RoleDescription, WorkFlowOrder) VALUES (@RoleDescription , @WorkFlowOrder); SELECT CAST(scope_identity() AS int)";
                SqlCommand cmdProcessRoleInsert = new SqlCommand(sqlProcessRoleInsert, _DBActive);
                cmdProcessRoleInsert.Parameters.Add("@RoleDescription", SqlDbType.Text);
                cmdProcessRoleInsert.Parameters.Add("@WorkFlowOrder", SqlDbType.Int);
                int newRoleID;

                string sqlProcessRolePrimaryExtensionInsert = "INSERT INTO ProcessRoleExtensionsPrimary (RoleID, Extension) VALUES (@RoleID , @Extension)";
                SqlCommand cmdProcessRolePrimaryExtensionInsert = new SqlCommand(sqlProcessRolePrimaryExtensionInsert, _DBActive);
                cmdProcessRolePrimaryExtensionInsert.Parameters.Add("@RoleID", SqlDbType.Int);
                cmdProcessRolePrimaryExtensionInsert.Parameters.Add("@Extension", SqlDbType.Text);

                string sqlProcessRoleSecondaryExtensionInsert = "INSERT INTO ProcessRoleExtensionsSecondary (RoleID, Extension) VALUES (@RoleID , @Extension)";
                SqlCommand cmdProcessRoleSecondaryExtensionInsert = new SqlCommand(sqlProcessRoleSecondaryExtensionInsert, _DBActive);
                cmdProcessRoleSecondaryExtensionInsert.Parameters.Add("@RoleID", SqlDbType.Int);
                cmdProcessRoleSecondaryExtensionInsert.Parameters.Add("@Extension", SqlDbType.Text);

                string sqlSpecialFilesInsert = "INSERT INTO SpecialFiles (FileName, ExcludeFromScan, RoleID) VALUES (@FileName, @ExcludeFromScan, @RoleID)";
                SqlCommand cmdSpecialFilesInsert = new SqlCommand(sqlSpecialFilesInsert, _DBActive);
                cmdSpecialFilesInsert.Parameters.Add("@FileName", SqlDbType.Text);
                cmdSpecialFilesInsert.Parameters.Add("@ExcludeFromScan", SqlDbType.Bit);
                cmdSpecialFilesInsert.Parameters.Add("@RoleID", SqlDbType.Int);
                
                DataTable tblProcessRoles = dsProcessRoleSet.Tables["ProcessRoles"];

                foreach (DataRow drFileRole in tblProcessRoles.Rows)
                {
                    Console.Write(drFileRole["WorkFlowOrder"].ToString() + "," + drFileRole["RoleDescription"].ToString() + Environment.NewLine);

                    cmdProcessRoleInsert.Parameters["@RoleDescription"].Value = drFileRole["RoleDescription"];
                    cmdProcessRoleInsert.Parameters["@WorkFlowOrder"].Value = drFileRole["WorkFlowOrder"];
                    int.TryParse(cmdProcessRoleInsert.ExecuteScalar().ToString(),out newRoleID);

                    foreach (DataRow drPrimaryExtensionRow in drFileRole.GetChildRows(dsProcessRoleSet.Relations["ProcessRolePrimaryExtension"]))
                    {
                        Console.WriteLine("Primary : " + drPrimaryExtensionRow["Extension"].ToString());
                        cmdProcessRolePrimaryExtensionInsert.Parameters["@RoleID"].Value = newRoleID;
                        cmdProcessRolePrimaryExtensionInsert.Parameters["@Extension"].Value = drPrimaryExtensionRow["Extension"];
                        cmdProcessRolePrimaryExtensionInsert.ExecuteNonQuery();

                    }

                    foreach (DataRow drSecondaryExtensionRow in drFileRole.GetChildRows(dsProcessRoleSet.Relations["ProcessRoleSecondaryExtension"]))
                    {
                        Console.WriteLine("Secondary : " + drSecondaryExtensionRow["Extension"].ToString());
                        cmdProcessRoleSecondaryExtensionInsert.Parameters["@RoleID"].Value = newRoleID;
                        cmdProcessRoleSecondaryExtensionInsert.Parameters["@Extension"].Value = drSecondaryExtensionRow["Extension"];
                        cmdProcessRoleSecondaryExtensionInsert.ExecuteNonQuery();

                    }

                    foreach (DataRow drSpecialFiles in drFileRole.GetChildRows(dsProcessRoleSet.Relations["SpecialFilesForRole"]))
                    {
                        Console.WriteLine("Special File : " + drSpecialFiles["FileName"].ToString());
                        cmdSpecialFilesInsert.Parameters["@FileName"].Value = drSpecialFiles["FileName"].ToString();
                        cmdSpecialFilesInsert.Parameters["@ExcludeFromScan"].Value = drSpecialFiles["ExcludeFromScan"];
                        cmdSpecialFilesInsert.Parameters["@RoleID"].Value = newRoleID;
                        cmdSpecialFilesInsert.ExecuteNonQuery();

                    }

                }

                #endregion

                // Bulk copy would be nice but we need to handle the changes keys which are the point of the reorganisation

                // using (SqlBulkCopy bulkCopy =
                //           new SqlBulkCopy(_DB))
                // {
                //    bulkCopy.DestinationTableName =
                //        "dbo.FileRoles";
                //
                //    bulkCopy.WriteToServer(tblFileRoles);
                // }
            }
            catch(Exception e)
            {
                MessageBox.Show(e.ToString());
                Trace.WriteLine(e.ToString());
            }
        }

        public static bool TruncateLog()
        {
            try
            {
                string sqlTruncateLog = "BACKUP LOG " + _DBInitialCatalog + "; DBCC SHRINKFILE (" + _DBInitialCatalog + "_Log, 1);";
                SqlCommand cmdTruncateLog = new SqlCommand(sqlTruncateLog);
                return true;
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                Trace.WriteLine(e.ToString());
                return false;
            }
        }
    }
}
