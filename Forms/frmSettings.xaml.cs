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
        private bool showingInitialSettings;
        private ArtContentManager.Skins.Skin skinSetting = new ArtContentManager.Skins.Skin();

        public frmSettings()
        {

            DataContext = skinSetting;
            InitializeComponent();

            showingInitialSettings = true;

            if (Static.DatabaseAgents.dbaSettings.Setting("CurrentSkinUri") != null)
            {
                skinSetting.URIPath = Static.DatabaseAgents.dbaSettings.Setting("CurrentSkinUri").Item1;
            }

            if (Static.DatabaseAgents.dbaSettings.Setting("WorkFolder") != null)
            {
                txtWorkFolder.Text = Static.DatabaseAgents.dbaSettings.Setting("WorkFolder").Item1;
            }

            if (Static.DatabaseAgents.dbaSettings.Setting("ImageStoreFolder") != null)
            {
                txtImageFolder.Text = Static.DatabaseAgents.dbaSettings.Setting("ImageStoreFolder").Item1;
            }

            if (Static.DatabaseAgents.dbaSettings.Setting("ProductPatternMatchLength") != null)
            {
                txtProductPatternMatchLength.Text = Static.DatabaseAgents.dbaSettings.Setting("ProductPatternMatchLength").Item2.ToString();
            }

            showingInitialSettings = false;
        }

        private void btnApplySkin_Click(object sender, RoutedEventArgs e)
        {
            string selectedURI;
            Uri skinURI;

            selectedURI = ((ArtContentManager.Skins.Skin)cboSkin.SelectedItem).URIPath;
            skinURI = new Uri(selectedURI, UriKind.Relative);

            foreach (Window openWindow in Application.Current.Windows)
            {
                if (openWindow is SkinableWindow)
                {
                    SkinableWindow openSkinableWindow = (SkinableWindow)openWindow;
                    openSkinableWindow.ApplySkin(skinURI);
                }
            }

            Static.DatabaseAgents.dbaSettings.SaveSetting("CurrentSkinUri", new Tuple<string, int>(selectedURI, 0));

        }

        private void btnReset_Click(object sender, RoutedEventArgs e)
        {

            if (rdoResetLevel_All.IsChecked == true)
            {
                MessageBoxResult result = MessageBox.Show("Do you wish to reset the database to empty?", "Database Reset", MessageBoxButton.OKCancel);

                if (result == MessageBoxResult.OK)
                {
                    ArtContentManager.Static.Database.Reset(Static.Database.ResetLevel.AllDynamicData);
                }
            }

            if (rdoResetLevel_Product.IsChecked == true)
            {
                MessageBoxResult result = MessageBox.Show("Do you wish to reset all product code data (file data will be retained)?", "Database Reset", MessageBoxButton.OKCancel);

                if (result == MessageBoxResult.OK)
                {
                    ArtContentManager.Static.Database.Reset(Static.Database.ResetLevel.ProductData);
                }
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

        private void btnBrowseImageFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                txtImageFolder.Text = dialog.SelectedPath;
            }
        }

        private void btnBrowseInstallationSourceFolder_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new System.Windows.Forms.FolderBrowserDialog();
            System.Windows.Forms.DialogResult result = dialog.ShowDialog();

            if (result == System.Windows.Forms.DialogResult.OK)
            {
                txtInstallationSourceFolder.Text = dialog.SelectedPath;
            }
        }

        private void txtWorkFolder_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!showingInitialSettings)
            {
                if (Static.FileSystemScan.IsWritableDirectory(txtWorkFolder.Text))
                {
                    Static.DatabaseAgents.dbaSettings.SaveSetting("WorkFolder", new Tuple<string, int>(txtWorkFolder.Text, 0));
                }
            }
        }

        private void txtImageFolder_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!showingInitialSettings)
            {
                if (Static.FileSystemScan.IsWritableDirectory(txtImageFolder.Text))
                {
                    Static.DatabaseAgents.dbaSettings.SaveSetting("ImageStoreFolder", new Tuple<string, int>(txtImageFolder.Text, 0));
                }
            }
        }

        private void txtInstallationSourceFolder_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (!showingInitialSettings)
            {
                if (Static.FileSystemScan.IsWritableDirectory(txtImageFolder.Text))
                {
                    Static.DatabaseAgents.dbaSettings.SaveSetting("InstallationSourceFolder", new Tuple<string, int>(txtInstallationSourceFolder.Text, 0));
                }
            }
        }

        private void txtProductPatternMatchLength_TextChanged(object sender, TextChangedEventArgs e)
        {
            int productPatternMatchLength;

            if (!showingInitialSettings)
            {
                if (Int32.TryParse(txtProductPatternMatchLength.Text, out productPatternMatchLength))
                {
                    Static.DatabaseAgents.dbaSettings.SaveSetting("ProductPatternMatchLength", new Tuple<string, int>("", productPatternMatchLength));
                }
            }

        }


    }
}
