using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;

namespace ArtContentManager.Static.DatabaseAgents
{
    static class dbaCurrencies
    {

        static DataTable _tblCurrencies;
        static bool _dataTableInitialised = false;
        static bool _dataTableLoaded = false;
        private static void InitialiseDataTable()
        {
            _tblCurrencies = new DataTable("Currencies");
            _tblCurrencies.Columns.Add("Currency");
            _tblCurrencies.Columns.Add("CurrencyDescription");
            _tblCurrencies.Columns.Add("CurrencySymbol");
            _dataTableInitialised = true;
        }
        public static void LoadCurrencies(bool forceLoad)
        {

            if (_dataTableInitialised == false)
            {
                InitialiseDataTable();
            }

            if ((!forceLoad) & (_dataTableLoaded))
            {
                return;
            }

            _tblCurrencies.Clear();
            _tblCurrencies.Rows.Add("  ", "", "");

            SqlConnection DB = ArtContentManager.Static.Database.DBReadOnly;
            string sqlCurrencies = "Select * from Currencies Order By Currency";
            SqlCommand cmdSelectCurrencies = new SqlCommand(sqlCurrencies, DB);

            using (SqlDataAdapter sadCurrency = new SqlDataAdapter(cmdSelectCurrencies))
            {
                sadCurrency.Fill(_tblCurrencies);
            }
            _dataTableLoaded = true;

        }

        public static DataTable tblCurrencies
        {
            get { return _tblCurrencies; }
        }
    }
}
