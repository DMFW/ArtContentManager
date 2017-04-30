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
using WPFExtension;

namespace ArtContentManager.Forms
{
    /// <summary>
    /// Interaction logic for frmProductDetails.xaml
    /// </summary>
    public partial class frmProductDetail : Window
    {

        Content.Product _displayProduct;
        bool _hyperLinkProductEditMode = false;
        bool _hyperLinkOrderEditMode = false;
        bool _promotionImagesLoaded = false;
        frmContentCreatorsSelect frmSelectContentCreators;

        public frmProductDetail(Content.Product displayProduct)
        {

            _displayProduct = displayProduct;
            DataContext = _displayProduct;

            InitializeComponent();

            txbHyperlinkProduct.Visibility = Visibility.Visible;
            txtProductHyperlink.Visibility = Visibility.Hidden;
            txbHyperlinkOrder.Visibility = Visibility.Visible;
            txtOrderHyperlink.Visibility = Visibility.Hidden;

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

            tabProduct_tiName.Header = _displayProduct.Name;

            LoadPrimaryImages();
            LoadPromotionImages();

            tabProductTextFiles.Items.Clear();

            for (int i = 0; i < displayProduct.TextFiles.Count; i++)
            {
                TabItem tabText = new TabItem();
                tabProductTextFiles.Items.Add(tabText);
                tabText.Header = _displayProduct.TextFiles[i].Name;

                ScrollViewer svwText = new ScrollViewer();
                tabText.Content = svwText;

                TextBlockSelect tbsText = new TextBlockSelect();
                svwText.Content = tbsText;
                tbsText.Text = _displayProduct.TextFiles[i].Text;
                tbsText.TextSelected += tbsText_TextSelected;
            }
        }
        private void tbsText_TextSelected(string SelectedText)
        {
            Clipboard.SetText(SelectedText);
        }

        private void LoadPrimaryImages()
        {

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
        }

        private void LoadPromotionImages()
        {

            Dictionary<string, string> imageFiles = _displayProduct.ImageFiles(false);

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

            _promotionImagesLoaded = true;
        }    

        private void btnSave_Click(object sender, RoutedEventArgs e)
        {

            BindComboBoxOutput();

            try
            {
                _displayProduct.Save();
                MessageBox.Show("Product saved to database");
            }
            catch (Exception error)
            {
                MessageBox.Show("Save failed :-" + Environment.NewLine + error.Message);
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
            if (_hyperLinkProductEditMode == false)
            {
                txbHyperlinkProduct.Visibility = Visibility.Hidden;
                txtProductHyperlink.Visibility = Visibility.Visible;
                btnProductLink.Content = "Show Product Hyperlink";
                _hyperLinkProductEditMode = true;
            }
            else
            {
                txbHyperlinkProduct.Visibility = Visibility.Visible;
                txtProductHyperlink.Visibility = Visibility.Hidden;
                btnProductLink.Content = "Edit Product Hyperlink";
                _hyperLinkProductEditMode = false;
            }
        }

        private void btnOrderLink_Click(object sender, RoutedEventArgs e)
        {
            if (_hyperLinkOrderEditMode == false)
            {
                txbHyperlinkOrder.Visibility = Visibility.Hidden;
                txtOrderHyperlink.Visibility = Visibility.Visible;
                btnOrderLink.Content = "Show Order Hyperlink";
                _hyperLinkOrderEditMode = true;
            }
            else
            {
                txbHyperlinkOrder.Visibility = Visibility.Visible;
                txtOrderHyperlink.Visibility = Visibility.Hidden;
                btnOrderLink.Content = "Edit Order Hyperlink";
                _hyperLinkOrderEditMode = false;
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
                dctProductCreators.Add(productCreator.CreatorID, productCreator);
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
