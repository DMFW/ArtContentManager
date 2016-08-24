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
        public frmProductDetails(Content.Product displayProduct)
        {

            DataContext = displayProduct;

            InitializeComponent();

            TabItem imageTab = (TabItem)tabCtrlProduct.Items[0];
            imageTab.Header = displayProduct.Name;

            Dictionary<string, string> imageFiles = displayProduct.ImageFiles;

            if (imageFiles.ContainsKey("TBN"))
            {
                Content.ImageResource thumbNailImage = new Content.ImageResource(imageFiles["TBN"]);
                if (thumbNailImage.ImageSource != null)
                {
                    imgThumbnail.Source = thumbNailImage.ImageSource;
                }
            }

            if (imageFiles.ContainsKey("000"))
            {
                Content.ImageResource primaryImage = new Content.ImageResource(imageFiles["000"]);
                if (primaryImage.ImageSource != null)
                {
                    imgPrimaryImage.Source = primaryImage.ImageSource;
                }
            }

            splPromotionImages.Children.Clear();

            foreach (KeyValuePair<string,string> kvpImageFile in imageFiles)
            {

                if (kvpImageFile.Key != "000" & kvpImageFile.Key!="TBN")
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

           tabCtrlProductTextFiles.Items.Clear();

           for (int i=0; i < displayProduct.TextFiles.Count; i++)
           {
                TabItem tabText = new TabItem();
                tabCtrlProductTextFiles.Items.Add(tabText);
                tabText.Header = displayProduct.TextFiles[i].Name;

                ScrollViewer svwText = new ScrollViewer();
                tabText.Content = svwText;
                TextBlock tbText = new TextBlock();
                svwText.Content = tbText;
                tbText.Text = displayProduct.TextFiles[i].Text;

            }

        }

    }
}
