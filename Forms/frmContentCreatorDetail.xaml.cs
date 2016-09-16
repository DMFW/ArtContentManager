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
    /// Interaction logic for frmContentCreatorDetail.xaml
    /// </summary>
    public partial class frmContentCreatorDetail : SkinableWindow
    {
        Content.Creator _creator;
        bool hyperLinkCreatorEditMode;
        public frmContentCreatorDetail()
        {
            // Add mode; create a new creator with a zero ID
            _creator = new ArtContentManager.Content.Creator();
            DataContext = _creator;

            InitializeComponent();
        }
        public frmContentCreatorDetail(Content.Creator creator)
        {
            // Update mode; update the supplied creator
            _creator = creator;
            _creator.LoadProductCount();

            DataContext = _creator;

            InitializeComponent();

        }
        private void Hyperlink_RequestNavigate(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start((sender as Hyperlink).NavigateUri.AbsoluteUri);
        }

        private void dgProductCredits_Hyperlink_Click(object sender, RoutedEventArgs e)
        {
            Hyperlink link = (Hyperlink)e.OriginalSource;
            System.Diagnostics.Process.Start(link.NavigateUri.AbsoluteUri);
        }

        private void btnCreatorHyperlink_Click(object sender, RoutedEventArgs e)
        {
            if (hyperLinkCreatorEditMode == false)
            {
                txbCreatorHyperlink.Visibility = Visibility.Hidden;
                txtCreatorHyperlink.Visibility = Visibility.Visible;
                btnCreatorHyperlink.Content = "Show Creator Hyperlink";
                hyperLinkCreatorEditMode = true;
            }
            else
            {
                txbCreatorHyperlink.Visibility = Visibility.Visible;
                txtCreatorHyperlink.Visibility = Visibility.Hidden;
                btnCreatorHyperlink.Content = "Edit Creator Hyperlink";
                hyperLinkCreatorEditMode = false;
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _creator.Save();
                MessageBox.Show("Creator saved to database");
            }
            catch(Exception error)
            {
                MessageBox.Show("Save failed :-" + Environment.NewLine + error.Message);
            }  

        }

        private void btnShowProducts_Click(object sender, RoutedEventArgs e)
        {
            _creator.LoadProductCreditsList();
            dgProductCredits.DataContext = _creator;
            dgProductCredits.ItemsSource = _creator.ProductCredits;
        }
    }
}
