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

namespace ArtContentManager.Forms
{
    /// <summary>
    /// Interaction logic for Settings.xaml
    /// </summary>
    public partial class frmSettings : SkinableWindow
    {
        private ArtContentManager.Content.Skin skinSetting = new ArtContentManager.Content.Skin();

        public frmSettings()
        {
            DataContext = skinSetting;
            skinSetting.URIPath = Properties.Settings.Default.CurrentSkinUri;
            InitializeComponent();

            if (Properties.Settings.Default.WorkFolder != null)
            {
                txtWorkFolder.Text = Properties.Settings.Default.WorkFolder;
            }
        }

        private void btnApplySkin_Click(object sender, RoutedEventArgs e)
        {
            string selectedURI;
            Uri skinURI;

            selectedURI = ((ArtContentManager.Content.Skin)cboSkin.SelectedItem).URIPath;
            skinURI = new Uri(selectedURI, UriKind.Relative);

            foreach (SkinableWindow openWindow in Application.Current.Windows)
            {
                openWindow.ApplySkin(skinURI);
            }

            Properties.Settings.Default.CurrentSkinUri = selectedURI;

        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {
            MessageBoxResult result = MessageBox.Show("Do you wish to reset the database to empty?", "Database Reset", MessageBoxButton.OKCancel);

            if (result == MessageBoxResult.OK)
            {
                ArtContentManager.Static.Database.Reset();
            }
        }

        private void btnBrowseWorkFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                txtWorkFolder.Text = dialog.SelectedPath;
            }

        }

        private void txtWorkFolder_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (Static.FileSystemScan.IsWritableDirectory(txtWorkFolder.Text))
            {
                Properties.Settings.Default.WorkFolder = txtWorkFolder.Text;
            }
        }

    }
}
