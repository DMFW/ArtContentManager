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
using System.IO;

namespace ArtContentManager.Forms
{
    /// <summary>
    /// Interaction logic for frmProductDetails.xaml
    /// </summary>
    public partial class frmProductDetail : Window
    {

        Content.Product _displayProduct;
        bool hyperLinkProductEditMode = false;
        bool hyperLinkOrderEditMode = false;
        frmContentCreatorsSelect frmSelectContentCreators;

        public frmProductDetail(Content.Product displayProduct)
        {

            _displayProduct = displayProduct;
            DataContext = _displayProduct;
            
            InitializeComponent();

            ArtContentManager.Static.DatabaseAgents.dbaMarketPlaces.LoadMarketPlaces(false);
            cboMarketPlace.ItemsSource = ArtContentManager.Static.DatabaseAgents.dbaMarketPlaces.tblMarketPlaces.DefaultView;

            ArtContentManager.Static.DatabaseAgents.dbaCurrencies.LoadCurrencies(false);
            cboCurrency.ItemsSource = ArtContentManager.Static.DatabaseAgents.dbaCurrencies.tblCurrencies.DefaultView;

            if (displayProduct.MarketPlaceID == null)
            {
                cboMarketPlace.SelectedValue = 0;
            }
            else
            {
                cboMarketPlace.SelectedValue = (int)_displayProduct.MarketPlaceID;
            }

            if (displayProduct.Currency == null)
            {
                cboCurrency.SelectedValue = " ";
            }
            else
            {
                cboCurrency.SelectedValue = _displayProduct.Currency;
            }

            LoadImages();
           
            tabCtrlProductTextFiles.Items.Clear();

            for (int i=0; i < displayProduct.TextFiles.Count; i++)
            {
                TabItem tabText = new TabItem();
                tabCtrlProductTextFiles.Items.Add(tabText);
                tabText.Header = _displayProduct.TextFiles[i].Name;

                ScrollViewer svwText = new ScrollViewer();
                tabText.Content = svwText;
                TextBlock tbText = new TextBlock();
                svwText.Content = tbText;
                tbText.Text = _displayProduct.TextFiles[i].Text;

            }
        }

        private void LoadImages()
        {

            TabItem imageTab = (TabItem)tabCtrlProduct.Items[0];
            imageTab.Header = _displayProduct.Name;

            Dictionary<string, string> imageFiles = _displayProduct.ImageFiles(true);

            if (imageFiles.ContainsKey("tbn"))
            {
                Content.ImageResource thumbNailImage = new Content.ImageResource(imageFiles["tbn"]);
                if (thumbNailImage.ImageSource != null)
                {
                    imgThumbnail.Source = thumbNailImage.ImageSource;
                    imgThumbnail.InvalidateVisual();
                }
            }

            if (imageFiles.ContainsKey("000"))
            {
                Content.ImageResource primaryImage = new Content.ImageResource(imageFiles["000"]);
                if (primaryImage.ImageSource != null)
                {
                    imgPrimaryImage.Source = primaryImage.ImageSource;
                    imgPrimaryImage.InvalidateVisual();
                }
            }

            splPromotionImages.Children.Clear();

            foreach (KeyValuePair<string, string> kvpImageFile in imageFiles)
            {

                if (kvpImageFile.Key != "000" & kvpImageFile.Key != "tbn")
                {
                    // Ignore the primary image and the thumbnail but process everything else

                    System.Windows.Controls.Image promotionImageCtl = new System.Windows.Controls.Image();
                    promotionImageCtl.Height = splPromotionImages.Height;
                    promotionImageCtl.Stretch = Stretch.Uniform;
                    promotionImageCtl.StretchDirection = StretchDirection.DownOnly;
                    splPromotionImages.Children.Add(promotionImageCtl);

                    Content.ImageResource promotionImage = new Content.ImageResource(kvpImageFile.Value);
                    if (promotionImage.ImageSource != null)
                    {
                        promotionImageCtl.Source = promotionImage.ImageSource;
                    }

                }
            }

            svPromotionImages.InvalidateScrollInfo();

        }

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {

            BindComboBoxOutput();

            try
            {
                _displayProduct.Save();
                MessageBox.Show("Product saved to database");
            }
            catch(Exception error)
            {
                MessageBox.Show("Save failed :-"+ Environment.NewLine + error.Message);
            }
        }

        private void BindComboBoxOutput()
        {

            // The combo box controls which we have to bind manually to the product
            // unless I find there is a better more automated way to do it.

            int marketPlaceID;

            if (cboMarketPlace.SelectedValue != null)
            {
                Int32.TryParse(cboMarketPlace.SelectedValue.ToString(), out marketPlaceID);
                _displayProduct.MarketPlaceID = marketPlaceID;
            }

            if (cboCurrency.SelectedValue != null)
            {
                _displayProduct.Currency = cboCurrency.SelectedValue.ToString();
            }

        }

        private void Hyperlink_RequestNavigate(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start((sender as Hyperlink).NavigateUri.AbsoluteUri);
        }

        private void btnProductLink_Click(object sender, RoutedEventArgs e)
        {
            if (hyperLinkProductEditMode == false)
            {
                txbHyperlinkProduct.Visibility = Visibility.Hidden;
                txtProductHyperlink.Visibility = Visibility.Visible;
                btnProductLink.Content = "Show Product Hyperlink";
                hyperLinkProductEditMode = true;
            }
            else
            {
                txbHyperlinkProduct.Visibility = Visibility.Visible;
                txtProductHyperlink.Visibility = Visibility.Hidden;
                btnProductLink.Content = "Edit Product Hyperlink";
                hyperLinkProductEditMode = false;
            }
        }

        private void btnOrderLink_Click(object sender, RoutedEventArgs e)
        {
            if (hyperLinkOrderEditMode == false)
            {
                txbHyperlinkOrder.Visibility = Visibility.Hidden;
                txtOrderHyperlink.Visibility = Visibility.Visible;
                btnOrderLink.Content = "Show Order Hyperlink";
                hyperLinkOrderEditMode = true;
            }
            else
            {
                txbHyperlinkOrder.Visibility = Visibility.Visible;
                txtOrderHyperlink.Visibility = Visibility.Hidden;
                btnOrderLink.Content = "Edit Order Hyperlink";
                hyperLinkOrderEditMode = false;
            }
        }

        void btnViewCreator_Click(object sender, RoutedEventArgs e)
        {
            // Launch the detail view form directly here

            Content.Creator creatorToView = _displayProduct.Creators[dgCreators.SelectedIndex];

            frmContentCreatorDetail frmContentCreatorDetail = new frmContentCreatorDetail(creatorToView);
            frmContentCreatorDetail.ShowDialog();
        }

        private void btnSelectCreators_Click(object sender, RoutedEventArgs e)
        {

            Dictionary<int, Content.Creator> dctProductCreators = new Dictionary<int, Content.Creator>();

            foreach (Content.Creator productCreator in _displayProduct.Creators)
            {
                dctProductCreators.Add(productCreator.ID, productCreator);
            }

            frmContentCreatorsSelect frmContentCreators = new frmContentCreatorsSelect(dctProductCreators);
            frmContentCreators.ShowDialog();

            _displayProduct.Creators.Clear();
            foreach (Content.Creator selectedCreator in Static.DatabaseAgents.dbaContentCreators.SelectedContentCreators())
            {
                _displayProduct.Creators.Add(selectedCreator);
            }
        }
    }
}
