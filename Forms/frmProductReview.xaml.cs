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
using System.Collections.ObjectModel;
using ArtContentManager.Static.DatabaseAgents;

namespace ArtContentManager.Forms
{
    /// <summary>
    /// Interaction logic for ProductReview.xaml
    /// </summary>
    public partial class frmProductReview : SkinableWindow
    {

        public frmProductReview(Actions.SelectProducts resolvedSelectedProducts)
        {

            InitializeComponent();

            foreach (Content.Product product in resolvedSelectedProducts.SelectedProducts)
            {

                Dictionary<string, string> imageFiles = product.ImageFiles(true);

                if (imageFiles.ContainsKey("tbn"))
                {
                    Content.ImageResource thumbNailImage = new Content.ImageResource(imageFiles["tbn"]);
                    if (thumbNailImage.ImageSource != null)
                    {
                        AddProductThumb(product, thumbNailImage);
                    }
                    else
                    {
                        AddProductLabel(product);
                    }
                }
                else
                {
                    AddProductLabel(product);
                }
                
            }

        }

        private void AddProductThumb(Content.Product product, Content.ImageResource thumbNailImage)
        {

            Image imgThumbnail = new System.Windows.Controls.Image();
            imgThumbnail.Tag = product;
            ugProducts.Children.Add(imgThumbnail);
            imgThumbnail.Source = thumbNailImage.ImageSource;
            imgThumbnail.MouseLeftButtonUp += new System.Windows.Input.MouseButtonEventHandler(this.ThumbnailSelected);

        }

        private void AddProductLabel(Content.Product product)
        {
            Label lblProduct = new Label();
            lblProduct.Content = product.Name;
            lblProduct.Width = double.NaN;
            lblProduct.Height = double.NaN;
            lblProduct.Margin = new Thickness(0, 21, 0, 0);
            lblProduct.Foreground = new SolidColorBrush(Colors.White);
            lblProduct.Background = new SolidColorBrush(Colors.Black);
            lblProduct.HorizontalAlignment = HorizontalAlignment.Center;
            lblProduct.VerticalAlignment = VerticalAlignment.Center;
            lblProduct.FontSize = 18;
            lblProduct.Tag = product;
            ugProducts.Children.Add(lblProduct);
            lblProduct.MouseLeftButtonUp += new System.Windows.Input.MouseButtonEventHandler(this.LabelSelected);
        }
        private void ThumbnailSelected(object sender, MouseButtonEventArgs e)
        {
            Image imgThumbnail = (Image)sender;
            Content.Product selectedProduct = (Content.Product)imgThumbnail.Tag;
            ShowProductDetails(selectedProduct);
        }

        private void LabelSelected(object sender, MouseButtonEventArgs e)
        {
            Label lblProduct = (Label)sender;
            Content.Product selectedProduct = (Content.Product)lblProduct.Tag;
            ShowProductDetails(selectedProduct);
        }

        private void ShowProductDetails(Content.Product selectedProduct)
        {

            dbaProduct.ProductLoadOptions loadOptions = new dbaProduct.ProductLoadOptions();

            loadOptions.contentFiles = true;
            loadOptions.creators = true;
            loadOptions.installationFiles = true;

            dbaProduct.Load(selectedProduct, loadOptions);

            frmProductDetails frmProductDetails = new frmProductDetails(selectedProduct);
            frmProductDetails.ShowDialog();

        }

    }
}
