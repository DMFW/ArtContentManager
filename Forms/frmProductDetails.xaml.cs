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
    public partial class frmProductDetails : Window
    {

        Content.Product _displayProduct;
        bool hyperLinkEditMode = false;

        public frmProductDetails(Content.Product displayProduct)
        {

           _displayProduct = displayProduct;
           DataContext = _displayProduct;

           InitializeComponent();

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
            if (ArtContentManager.Static.DatabaseAgents.dbaProduct.UpdateProduct(_displayProduct))
            {
                lblStatusMessage.Content = "Updated product details saved to database.";
                if (_displayProduct.Name != _displayProduct.NameSavedToDatabase)
                {
                    // The name has been changed. Copy images using the correct name and folder structure
                    ArtContentManager.Static.ProductImageManager.RenameProductImages(_displayProduct.NameSavedToDatabase, _displayProduct.Name);
                    _displayProduct.NameSavedToDatabase = _displayProduct.Name;
                    ArtContentManager.Static.ProductImageManager.DeleteOldProductNameImages();
                }
            }
            else
            {
                lblStatusMessage.Content = "Update to product details has failed.";
            }
        }
        private void Hyperlink_RequestNavigate(object sender, RoutedEventArgs e)
        {
            System.Diagnostics.Process.Start((sender as Hyperlink).NavigateUri.AbsoluteUri);
        }

        private void btnLink_Click(object sender, RoutedEventArgs e)
        {
            if (hyperLinkEditMode == false)
            {
                txbHyperlink.Visibility = Visibility.Hidden;
                txtProductHyperlink.Visibility = Visibility.Visible;
                hyperLinkEditMode = true;
            }
            else
            {
                txbHyperlink.Visibility = Visibility.Visible;
                txtProductHyperlink.Visibility = Visibility.Hidden;
                hyperLinkEditMode = false;
            }
        }
    }
}
