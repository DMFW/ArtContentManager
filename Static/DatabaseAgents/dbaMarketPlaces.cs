using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data;
using System.Data.SqlClient;
using System.Collections.ObjectModel;

namespace ArtContentManager.Static.DatabaseAgents
{
    static class dbaMarketPlaces
    {
        static DataTable _tblMarketPlaces;
        static bool _dataTableInitialised = false;
        static bool _dataTableLoaded = false;
        static ObservableCollection<ArtContentManager.Content.MarketPlace> _obcMarketPlaces;
        private static void InitialiseDataTable()
        {
            _tblMarketPlaces = new DataTable("MarketPlaces");
            _tblMarketPlaces.Columns.Add("MarketPlaceID");
            _tblMarketPlaces.Columns.Add("MarketPlaceName");
            _tblMarketPlaces.Columns.Add("MarketPlaceHomeUri");
            _tblMarketPlaces.Columns.Add("MarketPlaceIdentifier");
            _dataTableInitialised = true;
        }
        public static void LoadMarketPlaces(bool forceLoad)
        {

            if (_dataTableInitialised == false)
            {
                InitialiseDataTable();
            }

            if ((!forceLoad) & (_dataTableLoaded))
            {
                return;
            }

            _tblMarketPlaces.Clear();
            _tblMarketPlaces.Rows.Add(0, "Not Specified", "", "");

            SqlConnection DB = ArtContentManager.Static.Database.DBReadOnly;
            string sqlSelectMarketPlaces = "Select * from MarketPlaces Order By DisplayPriorityOrder";
            SqlCommand cmdSelectMarketPlaces = new SqlCommand(sqlSelectMarketPlaces, DB);

            using (SqlDataAdapter sadMarkePlace = new SqlDataAdapter(cmdSelectMarketPlaces))
            {
                sadMarkePlace.Fill(_tblMarketPlaces);
            }
            _dataTableLoaded = true;

        }

        public static DataTable tblMarketPlaces
        {
            get { return _tblMarketPlaces; }
        }
    }
}
