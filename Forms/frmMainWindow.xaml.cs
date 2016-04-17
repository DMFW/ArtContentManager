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
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Diagnostics;

namespace ArtContentManager.Forms
{
    /// <summary>
    /// Interaction logic for frmMainWindow.xaml
    /// </summary>
    public partial class frmMainWindow : SkinableWindow
    {
        frmFileMaintenance frmFileMaintenance;
        frmSettings frmSettings;

        public frmMainWindow()
        {
            Static.Log.InitiateTracer();
            InitializeComponent();

            lblStatus.Content = "Opening database...";
            if (Static.Database.Open())
            {
                lblStatus.Content = "Database opened successfully.";
            }
            else
            {
                lblStatus.Content = "Database could not be opened. Review your settings and log file.";
            }
        }

        private void btnFiles_Click(object sender, RoutedEventArgs e)
        {
            frmFileMaintenance = new frmFileMaintenance();
            frmFileMaintenance.ShowDialog();
        }

        private void btnOrganisation_Click(object sender, RoutedEventArgs e)
        {
            frmSettings = new frmSettings();
            frmSettings.ShowDialog();
        }

        private void btnSettings_Click(object sender, RoutedEventArgs e)
        {
            frmSettings = new frmSettings();
            frmSettings.ShowDialog();
        }

        private void frmMainWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            lblStatus.Content = "Closing database...";
            Properties.Settings.Default.Save();
            Static.Database.Close();
        }

    }
}
