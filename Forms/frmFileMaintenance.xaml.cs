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
using System.ComponentModel;
using System.IO;

namespace ArtContentManager.Forms
{
    /// <summary>
    /// Interaction logic for AddContent.xaml
    /// </summary>
    public partial  class frmFileMaintenance : SkinableWindow
    {

        private BackgroundWorker scanCountWorker;
        private BackgroundWorker scanImportWorker;

        private string _currentScanRoot;

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

            if (!Directory.Exists(_currentScanRoot))
            {
                MessageBoxResult invalidScanPath = System.Windows.MessageBox.Show("You have selected an invalid root path. The scan cannot be performed.", "Scan Request Invalid");
                return;
            }

            scanCountWorker = new BackgroundWorker();
            scanCountWorker.WorkerReportsProgress = true;
            scanCountWorker.WorkerSupportsCancellation = true;
            scanCountWorker.DoWork += scanCountWorker_DoWork;
            scanCountWorker.RunWorkerCompleted += scanCountWorker_RunWorkerCompleted;
            scanCountWorker.ProgressChanged += scanCountWorker_ReportProgress;

            btnScan.IsEnabled = false;
            btnScanCancel.IsEnabled = true;
            scanCountWorker.RunWorkerAsync();   // Count all the files
           
        }

        private void scanCountWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Static.FileSystemScan.scanMode = Static.FileSystemScan.ScanMode.smCount;
            Static.FileSystemScan.Scan(_currentScanRoot, scanCountWorker);   
        }

        private void scanCountWorker_ReportProgress(object sender, ProgressChangedEventArgs e)
        {
            lblStatusMessage.Content = Static.ScanProgress.Message;
        }

        private void scanCountWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

            scanImportWorker = new BackgroundWorker();
            scanImportWorker.WorkerReportsProgress = true;
            scanImportWorker.WorkerSupportsCancellation = true;
            scanImportWorker.DoWork += scanImportWorker_DoWork;
            scanImportWorker.RunWorkerCompleted += scanImportWorker_RunWorkerCompleted;
            scanImportWorker.ProgressChanged += scanImportWorker_ReportProgress;

            scanImportWorker.RunWorkerAsync(); // Process all the files
        }

        private void scanImportWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            Static.FileSystemScan.scanMode = Static.FileSystemScan.ScanMode.smImport;
            Static.FileSystemScan.knownFileTotalCount = Static.FileSystemScan.fileCount; // For progress reporting purposes
            Static.FileSystemScan.Scan(_currentScanRoot, scanImportWorker);
        }

        private void scanImportWorker_ReportProgress(object sender, ProgressChangedEventArgs e)
        {
            lblStatusMessage.Content = Static.ScanProgress.Message;
            pbgScan.Value = Static.ScanProgress.CompletionPct;
        }

        private void scanImportWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            scanCountWorker = null;
            scanImportWorker = null;
            btnScan.IsEnabled = true;
            btnScanCancel.IsEnabled = false;

            if (!e.Cancelled)
            {
                lblStatusMessage.Content = "Completed processing of " + Static.FileSystemScan.fileCount + " files";
            }

        }

        private void frmFileMaintenance_Loaded(object sender, RoutedEventArgs e)
        {
            txtScanRoot.Text = Properties.Settings.Default.LastScanPath;
        }

        private void txtScanRoot_TextChanged(object sender, TextChangedEventArgs e)
        {
            Properties.Settings.Default.LastScanPath = txtScanRoot.Text;
            _currentScanRoot = txtScanRoot.Text;
        }

        private void btnScanCancel_Click(object sender, RoutedEventArgs e)
        {
            if (scanCountWorker != null)
            {
                scanCountWorker.CancelAsync();
            }

            if (scanImportWorker != null)
            {
                scanImportWorker.CancelAsync();
            }
        }

    }
 }
