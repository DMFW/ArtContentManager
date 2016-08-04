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

        private BackgroundWorker _scanWorker;
        private BackgroundWorker _scanImportWorker;

        private string _formScanRoot;
        private Actions.Scan _currentRootScan;

        private bool showRootScansOnly;
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

        private void btnFullScan_Click(object sender, RoutedEventArgs e)
        {
            Queue <Static.FileSystemScan.ScanMode> qScanDirectives = new Queue<Static.FileSystemScan.ScanMode>();

            // Do the count then the full import

            qScanDirectives.Enqueue(Static.FileSystemScan.ScanMode.smFullImportCount);
            qScanDirectives.Enqueue(Static.FileSystemScan.ScanMode.smFullImport);

            StartScan(qScanDirectives);
        }

        private void btnCategoryScan_Click(object sender, RoutedEventArgs e)
        {

            Queue<Static.FileSystemScan.ScanMode> qScanDirectives = new Queue<Static.FileSystemScan.ScanMode>();

            // Do the count then the full import

            qScanDirectives.Enqueue(Static.FileSystemScan.ScanMode.smCategoryImportCount);
            qScanDirectives.Enqueue(Static.FileSystemScan.ScanMode.smCategoryImport);

            StartScan(qScanDirectives);
        }

        private void StartScan(Queue<Static.FileSystemScan.ScanMode> qScanMode)
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

            _scanWorker = new BackgroundWorker();
            _scanWorker.WorkerReportsProgress = true;
            _scanWorker.WorkerSupportsCancellation = true;
            _scanWorker.DoWork += scanWorker_DoWork;
            _scanWorker.RunWorkerCompleted += scanWorker_RunWorkerCompleted;
            _scanWorker.ProgressChanged += scanWorker_ReportProgress;

            btnFullScan.IsEnabled = false;
            btnCategoryScan.IsEnabled = false;
            btnScanCancel.IsEnabled = true;

            _currentRootScan = new Actions.Scan();
            _currentRootScan.FolderName = _formScanRoot;
            _currentRootScan.IsRequestRoot = true;
            _currentRootScan.StartScanTime = DateTime.Now;
            ArtContentManager.Static.DatabaseAgents.dbaScanHistory.RecordStartScan(_currentRootScan);
            ArtContentManager.Static.DatabaseAgents.dbaScanHistory.SetLastCompletedScanTime(_currentRootScan);

            _scanWorker.RunWorkerAsync(qScanMode);

        }

        private void scanWorker_DoWork(object sender, DoWorkEventArgs e)
        {

            Queue<Static.FileSystemScan.ScanMode> qScanMode = (Queue <Static.FileSystemScan.ScanMode>) e.Argument;
            Static.FileSystemScan.ScanMode scanMode = qScanMode.Dequeue();
            Static.FileSystemScan.Scan(scanMode, _currentRootScan, _scanWorker);

            // Return the current scan and the queue of remaining scan types in a tuple.
            Tuple<Static.FileSystemScan.ScanMode, Queue<Static.FileSystemScan.ScanMode>> tplScanDirectives = new Tuple<Static.FileSystemScan.ScanMode, Queue<Static.FileSystemScan.ScanMode>>(scanMode, qScanMode);
            e.Result = tplScanDirectives;
        }

        private void scanWorker_ReportProgress(object sender, ProgressChangedEventArgs e)
        {
            lblStatusMessage.Content = Static.ScanProgress.Message;
            pbgScan.Value = Static.ScanProgress.CompletionPct;
        }

        private void scanWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {

            Tuple<Static.FileSystemScan.ScanMode, Queue<Static.FileSystemScan.ScanMode>> tplScanDirectives = (Tuple <Static.FileSystemScan.ScanMode, Queue<Static.FileSystemScan.ScanMode>>) e.Result;

            // Retrieve the last scan mode we completed and the queue of remaining modes we want to execute.

            Static.FileSystemScan.ScanMode completedScanMode = tplScanDirectives.Item1;
            Queue<Static.FileSystemScan.ScanMode> qScanMode = tplScanDirectives.Item2;


            if (e.Cancelled)
            {
                ArtContentManager.Static.Database.BeginTransaction(Static.Database.TransactionType.Active);
                ArtContentManager.Static.DatabaseAgents.dbaScanHistory.RecordScanAbort(_currentRootScan);
                ArtContentManager.Static.Database.CommitTransaction(Static.Database.TransactionType.Active);
            }
            else
            {

                if (completedScanMode == Static.FileSystemScan.ScanMode.smFullImport)
                {
                    lblStatusMessage.Content = "Completed scanning of " + _currentRootScan.TotalFiles + " files [" + _currentRootScan.ProcessedFiles + " new imports]";
                    _currentRootScan.CompleteScanTime = DateTime.Now;
                    ArtContentManager.Static.Database.BeginTransaction(Static.Database.TransactionType.Active);
                    ArtContentManager.Static.DatabaseAgents.dbaScanHistory.RecordScanComplete(_currentRootScan);
                    ArtContentManager.Static.Database.CommitTransaction(Static.Database.TransactionType.Active);
                }

                if (qScanMode.Count > 0)
                {
                    // Dive back in and do another scan
                    _scanWorker.RunWorkerAsync(qScanMode);
                }
                else
                {
                    _scanWorker = null;
                    btnFullScan.IsEnabled = true;
                    btnCategoryScan.IsEnabled = true;
                    btnScanCancel.IsEnabled = false;
                }
            }
        }

        private void frmFileMaintenance_Loaded(object sender, RoutedEventArgs e)
        {

            txtScanRoot.Text = Static.DatabaseAgents.dbaSettings.Setting("LastScanPath").Item1;
            showRootScansOnly = true;
            LoadExistingScans(showRootScansOnly);
        }

        private void LoadExistingScans(bool rootScansOnly)
        { 
            _existingScans = new List<Actions.Scan>();
            Actions.Scan existingScan;

            SqlCommand cmdLoadExistingScans;
            SqlDataReader drLoadExistingScans;

            if (rootScansOnly)
            {
                cmdLoadExistingScans = new SqlCommand("SELECT * from ScanHistory WHERE IsRequestRoot = @IsRequestRoot ORDER BY Started DESC, ScanID", Static.Database.DBReadOnly);
                cmdLoadExistingScans.Parameters.Add("@IsRequestRoot", System.Data.SqlDbType.Bit);
                cmdLoadExistingScans.Parameters["@IsRequestRoot"].Value = true;
            }
            else
            {
                cmdLoadExistingScans = new SqlCommand("SELECT * from ScanHistory ORDER BY Started DESC, ScanID", Static.Database.DBReadOnly);
            }

            drLoadExistingScans = cmdLoadExistingScans.ExecuteReader();
            while (drLoadExistingScans.Read())
            {
                existingScan = new Actions.Scan();
                existingScan.ID = (int)drLoadExistingScans["ScanID"];
                existingScan.FolderName = drLoadExistingScans["FolderName"].ToString();
                existingScan.IsRequestRoot = (bool)drLoadExistingScans["IsRequestRoot"];

                if (drLoadExistingScans["Started"] != DBNull.Value) { existingScan.StartScanTime = (DateTime)drLoadExistingScans["Started"]; }
                if (drLoadExistingScans["Aborted"] != DBNull.Value) { existingScan.AbortScanTime = (DateTime)drLoadExistingScans["Aborted"]; }
                if (drLoadExistingScans["Completed"] != DBNull.Value) { existingScan.CompleteScanTime = (DateTime)drLoadExistingScans["Completed"]; }

                existingScan.TotalFiles = drLoadExistingScans["TotalFiles"] as int? ?? 0;
                existingScan.NewFiles = drLoadExistingScans["NewFiles"] as int? ?? 0;
                existingScan.ProcessedFiles = drLoadExistingScans["ProcessedFiles"] as int? ?? 0;

                _existingScans.Add(existingScan);
            }
            drLoadExistingScans.Close();
            lvwExistingScans.ItemsSource = _existingScans;

        }

        private List<Actions.Scan> ExistingScans
        {
            get { return _existingScans; }
        }

        private void txtScanRoot_TextChanged(object sender, TextChangedEventArgs e)
        {
            Static.DatabaseAgents.dbaSettings.SaveSetting("LastScanPath", new Tuple<string, int>(txtScanRoot.Text, 0));
            _formScanRoot = txtScanRoot.Text;
        }

        private void btnScanCancel_Click(object sender, RoutedEventArgs e)
        {
            if (_scanWorker != null)
            {
                _scanWorker.CancelAsync();
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

        private void chkRootScansOnly_Checked(object sender, RoutedEventArgs e)
        {
            showRootScansOnly = (bool)chkRootScansOnly.IsChecked;
            LoadExistingScans(showRootScansOnly);
        }

        private void chkRootScansOnly_Unchecked(object sender, RoutedEventArgs e)
        {
            showRootScansOnly = (bool)chkRootScansOnly.IsChecked;
            LoadExistingScans(showRootScansOnly);
        }

        private void btnAutoProducts_Click_1(object sender, RoutedEventArgs e)
        {

        }
    }
 }
