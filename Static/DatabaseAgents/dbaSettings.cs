﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;

namespace ArtContentManager.Static.DatabaseAgents
{
    static class dbaSettings
    {

        static Dictionary<string, Tuple<string, int>> _Settings;
        static SqlCommand _cmdSelectSettings;
        static SqlCommand _cmdSaveSetting;
        static SqlCommand _cmdInsertSetting;

        public static void LoadSettings()
        {
            _Settings = new Dictionary<string, Tuple<string, int>>();

            SqlConnection DB = ArtContentManager.Static.Database.DBActive;
            
            if (_cmdSelectSettings == null)
            {
                string sqlSelectSettings = "SELECT * FROM Settings";
                _cmdSelectSettings = new SqlCommand(sqlSelectSettings, DB);
            }

            SqlDataReader rdrSettings = _cmdSelectSettings.ExecuteReader();

            if (rdrSettings.HasRows)
            {
                while (rdrSettings.Read())
                {

                    string settingTextValue = rdrSettings["SettingTextValue"].ToString();
                    int settingIntValue;
                    if (Int32.TryParse(rdrSettings["SettingIntValue"].ToString(), out settingIntValue)) { }
                    else { settingIntValue = 0; }

                    _Settings.Add(rdrSettings["SettingName"].ToString(), new Tuple<string, int>(settingTextValue, settingIntValue));
                }
            }

            rdrSettings.Close();
        }

        public static void SaveSetting(string SettingName, Tuple<string, int> SettingValues)
        {
            SqlConnection DB = ArtContentManager.Static.Database.DBActive;

            if (_Settings.ContainsKey(SettingName))
            {
                _Settings[SettingName] = SettingValues;

                if (_cmdSaveSetting == null)
                {
                    string sqlSaveSetting = "UPDATE Settings Set SettingTextValue = @SettingTextValue, SettingIntValue = @SettingIntValue WHERE SettingName = @SettingName";
                    _cmdSaveSetting = new SqlCommand(sqlSaveSetting, DB);
                    _cmdSaveSetting.Parameters.Add("@SettingName", System.Data.SqlDbType.NVarChar, 255);
                    _cmdSaveSetting.Parameters.Add("@SettingTextValue", System.Data.SqlDbType.NVarChar, 255);
                    _cmdSaveSetting.Parameters.Add("@SettingIntValue", System.Data.SqlDbType.Int);
                }

                _cmdSaveSetting.Parameters["@SettingName"].Value = SettingName;
                _cmdSaveSetting.Parameters["@SettingTextValue"].Value = SettingValues.Item1;
                _cmdSaveSetting.Parameters["@SettingIntValue"].Value = SettingValues.Item2;

                ArtContentManager.Static.Database.BeginTransaction(Database.TransactionType.Active);
                _cmdSaveSetting.Transaction = ArtContentManager.Static.Database.CurrentTransaction(Database.TransactionType.Active);
                _cmdSaveSetting.ExecuteScalar();
                ArtContentManager.Static.Database.CommitTransaction(Database.TransactionType.Active);
            }
            else
            {
                _Settings.Add(SettingName, SettingValues);
                if (_cmdInsertSetting == null)
                {
                    string sqlSaveSetting = "INSERT INTO Settings (SettingName, SettingTextValue, SettingIntValue) Values (@SettingName, @SettingTextValue, @SettingIntValue)";
                    _cmdInsertSetting = new SqlCommand(sqlSaveSetting, DB);
                    _cmdInsertSetting.Parameters.Add("@SettingName", System.Data.SqlDbType.NVarChar, 255);
                    _cmdInsertSetting.Parameters.Add("@SettingTextValue", System.Data.SqlDbType.NVarChar, 255);
                    _cmdInsertSetting.Parameters.Add("@SettingIntValue", System.Data.SqlDbType.Int);
                }


                _cmdInsertSetting.Transaction = ArtContentManager.Static.Database.CurrentTransaction(Database.TransactionType.Active);
                _cmdInsertSetting.Parameters["@SettingName"].Value = SettingName;
                _cmdInsertSetting.Parameters["@SettingTextValue"].Value = SettingValues.Item1;
                _cmdInsertSetting.Parameters["@SettingIntValue"].Value = SettingValues.Item2;

                ArtContentManager.Static.Database.BeginTransaction(Database.TransactionType.Active);
                _cmdInsertSetting.Transaction = ArtContentManager.Static.Database.CurrentTransaction(Database.TransactionType.Active);
                _cmdInsertSetting.ExecuteScalar();
                ArtContentManager.Static.Database.CommitTransaction(Database.TransactionType.Active);
            }

        }

        public static Tuple<string,int> Setting(string SettingName)
        {
            if (_Settings == null)
            { 
                return null;
            }

            if (_Settings.ContainsKey(SettingName))
            {
                return _Settings[SettingName];
            }
            else
            {
                return null;
            }
        }

    }
}
