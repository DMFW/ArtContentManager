using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
                _DB = new SqlConnection("Data Source=(LocalDB)\\v11.0;AttachDbFilename=D:\\SQLServer\\MSSQL11.SQLEXPRESS\\MSSQL\\DATA\\ArtProducts.mdf;Integrated Security=True;Connect Timeout=30;MultipleActiveResultSets=true");
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
                string sqlFileLocationsTruncate = "TRUNCATE TABLE FileLocations";
                SqlCommand cmdFileLocations = new SqlCommand(sqlFileLocationsTruncate, _DB);
                cmdFileLocations.ExecuteNonQuery();

                string sqlFilesTruncate = "TRUNCATE TABLE Files";
                SqlCommand cmdFiles = new SqlCommand(sqlFilesTruncate, _DB);
                cmdFiles.ExecuteNonQuery();

                cmdFiles.CommandText = "DBCC CHECKIDENT (Files,RESEED, 0)";
                cmdFiles.ExecuteNonQuery();

                MessageBox.Show("Database reset complete");
            }
            catch (Exception e)
            {
                Trace.WriteLine(e.ToString());
            }

        }

    }
}
