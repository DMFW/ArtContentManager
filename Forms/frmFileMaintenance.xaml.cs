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

        private ArtContentManager.DatabaseAgents.dbaScanHistory _dbaScanHistory;
        private Actions.Scan _currentRootScan; 

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

             ArtContentManager.Static.Database.Open();

            _dbaScanHistory = new DatabaseAgents.dbaScanHistory();

            _currentRootScan = new Actions.Scan();

            _currentRootScan.FolderName = _formScanRoot;
            _currentRootScan.IsRequestRoot = true;
            _dbaScanHistory.SetLastCompletedScanTime(_currentRootScan);

            _currentRootScan.StartScanTime = DateTime.Now;
            _dbaScanHistory.RecordStartScan(_currentRootScan);

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
                _dbaScanHistory.RecordScanAbort(_currentRootScan);
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

            if (e.Cancelled)
            {
                _dbaScanHistory.RecordScanAbort(_currentRootScan);
            }
            else
            {
                lblStatusMessage.Content = "Completed processing of " + _currentRootScan.ProcessedFiles + " files";
                _currentRootScan.CompleteScanTime = DateTime.Now;
                _dbaScanHistory.RecordScanComplete(_currentRootScan);
            }
            ArtContentManager.Static.Database.Close();
        }

        private void frmFileMaintenance_Loaded(object sender, RoutedEventArgs e)
        {
            txtScanRoot.Text = Properties.Settings.Default.LastScanPath;
        }

        private void txtScanRoot_TextChanged(object sender, TextChangedEventArgs e)
        {
            Properties.Settings.Default.LastScanPath = txtScanRoot.Text;
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

    }
 }
