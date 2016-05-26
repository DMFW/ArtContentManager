using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Data;
using System.Data.SqlClient;
using System.ComponentModel;
using System.IO;

namespace ArtContentManager.Forms
{
    /// <summary>
    /// Interaction logic for AddContent.xaml
    /// </summary>
    public partial class frmFileMaintenance : SkinableWindow
    {

        private BackgroundWorker _scanCountWorker;
        private BackgroundWorker _scanImportWorker;

        private string _formScanRoot;
        private Actions.Scan _currentRootScan;

        private List<Actions.Scan> _existingScans;

        public frmFileMaintenance()
        {
            InitializeComponent();
            Loaded += frmFileMaintenance_Loaded;
        }

        private void btnBrowseScanRoot_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                txtScanRoot.Text = dialog.SelectedPath;
            }
        }

        private void btnScan_Click(object sender, RoutedEventArgs e)
        {

            if (!Directory.Exists(_formScanRoot))
            {
                MessageBoxResult invalidScanPath = System.Windows.MessageBox.Show("You have selected an invalid root path. The scan cannot be performed.", "Scan Request Invalid");
                return;
            }

            string workFolder = Static.DatabaseAgents.dbaSettings.Setting("WorkFolder").Item1;

            if (!Static.FileSystemScan.IsWritableDirectory(workFolder))
            {
                MessageBoxResult invalidScanPath = System.Windows.MessageBox.Show("The working folder is not a writeable location", "Scan Request Invalid");
                return;
            }

            _scanCountWorker = new BackgroundWorker();
            _scanCountWorker.WorkerReportsProgress = true;
            _scanCountWorker.WorkerSupportsCancellation = true;
            _scanCountWorker.DoWork += scanCountWorker_DoWork;
            _scanCountWorker.RunWorkerCompleted += scanCountWorker_RunWorkerCompleted;
            _scanCountWorker.ProgressChanged += scanCountWorker_ReportProgress;

            btnScan.IsEnabled = false;
            btnScanCancel.IsEnabled = true;
            _scanCountWorker.RunWorkerAsync();   // Count all the files
           
        }

        private void scanCountWorker_DoWork(object sender, DoWorkEventArgs e)
        {

            _currentRootScan = new Actions.Scan();

            _currentRootScan.FolderName = _formScanRoot;
            _currentRootScan.IsRequestRoot = true;
            ArtContentManager.Static.DatabaseAgents.dbaScanHistory.SetLastCompletedScanTime(_currentRootScan);

            _currentRootScan.StartScanTime = DateTime.Now;
            ArtContentManager.Static.DatabaseAgents.dbaScanHistory.RecordStartScan(_currentRootScan);

            Static.FileSystemScan.scanMode = Static.FileSystemScan.ScanMode.smCount;
            Static.FileSystemScan.Scan(_currentRootScan, _scanCountWorker);   
        }

        private void scanCountWorker_ReportProgress(object sender, ProgressChangedEventArgs e)
        {
            lblStatusMessage.Content = Static.ScanProgress.Message;
        }

        private void scanCountWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

            _scanImportWorker = new BackgroundWorker();
            _scanImportWorker.WorkerReportsProgress = true;
            _scanImportWorker.WorkerSupportsCancellation = true;
            _scanImportWorker.DoWork += scanImportWorker_DoWork;
            _scanImportWorker.RunWorkerCompleted += scanImportWorker_RunWorkerCompleted;
            _scanImportWorker.ProgressChanged += scanImportWorker_ReportProgress;

            if (e.Cancelled)
            {
                ArtContentManager.Static.Database.BeginTransaction(Static.Database.TransactionType.Active);
                ArtContentManager.Static.DatabaseAgents.dbaScanHistory.RecordScanAbort(_currentRootScan);
                ArtContentManager.Static.Database.CommitTransaction(Static.Database.TransactionType.Active);
            }
            else
            {
                _scanImportWorker.RunWorkerAsync(); // Process all the files
            }
        }

        private void scanImportWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Static.FileSystemScan.scanMode = Static.FileSystemScan.ScanMode.smImport;
            Static.FileSystemScan.Scan(_currentRootScan, _scanImportWorker);
        }

        private void scanImportWorker_ReportProgress(object sender, ProgressChangedEventArgs e)
        {
            lblStatusMessage.Content = Static.ScanProgress.Message;
            pbgScan.Value = Static.ScanProgress.CompletionPct;
        }

        private void scanImportWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            _scanCountWorker = null;
            _scanImportWorker = null;
            btnScan.IsEnabled = true;
            btnScanCancel.IsEnabled = false;
            ArtContentManager.Static.Database.BeginTransaction(Static.Database.TransactionType.Active);

            if (e.Cancelled)
            {
                ArtContentManager.Static.DatabaseAgents.dbaScanHistory.RecordScanAbort(_currentRootScan);
            }
            else
            {
                lblStatusMessage.Content = "Completed scanning of " + _currentRootScan.TotalFiles + " files [" + _currentRootScan.ProcessedFiles + " new imports]";
                _currentRootScan.CompleteScanTime = DateTime.Now;
                ArtContentManager.Static.DatabaseAgents.dbaScanHistory.RecordScanComplete(_currentRootScan);
            }
            ArtContentManager.Static.Database.CommitTransaction(Static.Database.TransactionType.Active);

        }

        private void frmFileMaintenance_Loaded(object sender, RoutedEventArgs e)
        {

            txtScanRoot.Text = Static.DatabaseAgents.dbaSettings.Setting("LastScanPath").Item1;
            LoadExistingScans();
        }

        private void LoadExistingScans()
        { 
            _existingScans = new List<Actions.Scan>();
            Actions.Scan existingScan;

            SqlDataReader drLoadExistingScans;
            SqlCommand cmdLoadExistingScans = new SqlCommand("SELECT * from ScanHistory WHERE IsRequestRoot = @IsRequestRoot", Static.Database.DBReadOnly);

            cmdLoadExistingScans.Parameters.Add("@IsRequestRoot", System.Data.SqlDbType.Bit);
            cmdLoadExistingScans.Parameters["@IsRequestRoot"].Value = true;

            drLoadExistingScans = cmdLoadExistingScans.ExecuteReader();
            while (drLoadExistingScans.Read())
            {
                existingScan = new Actions.Scan();
                existingScan.ID = (int)drLoadExistingScans["ScanID"];
                existingScan.FolderName = drLoadExistingScans["FolderName"].ToString();

                if (drLoadExistingScans["Started"] != DBNull.Value) { existingScan.StartScanTime = (DateTime)drLoadExistingScans["Started"]; }
                if (drLoadExistingScans["Aborted"] != DBNull.Value) { existingScan.AbortScanTime = (DateTime)drLoadExistingScans["Aborted"]; }
                if (drLoadExistingScans["Completed"] != DBNull.Value) { existingScan.CompleteScanTime = (DateTime)drLoadExistingScans["Completed"]; }

                existingScan.TotalFiles = (int)drLoadExistingScans["TotalFiles"];
                existingScan.NewFiles = (int)drLoadExistingScans["NewFiles"];
                existingScan.ProcessedFiles = (int)drLoadExistingScans["ProcessedFiles"];

                _existingScans.Add(existingScan);
            }
            drLoadExistingScans.Close();

        }

        private void txtScanRoot_TextChanged(object sender, TextChangedEventArgs e)
        {
            Static.DatabaseAgents.dbaSettings.SaveSetting("LastScanPath", new Tuple<string, int>(txtScanRoot.Text, 0));
            _formScanRoot = txtScanRoot.Text;
        }

        private void btnScanCancel_Click(object sender, RoutedEventArgs e)
        {
            if (_scanCountWorker != null)
            {
                _scanCountWorker.CancelAsync();
            }

            if (_scanImportWorker != null)
            {
                _scanImportWorker.CancelAsync();
            }
        }

        private void btnAutoProducts_Click(object sender, RoutedEventArgs e)
        {
            Static.DatabaseAgents.dbaProduct.AutoAssignProducts();
        }

        
    }
 }
