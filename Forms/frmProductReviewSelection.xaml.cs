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
    /// Interaction logic for frmProductReviewSelection.xaml
    /// </summary>
    public partial class frmProductReviewSelection : SkinableWindow
    {

        // The job of this form is to prepare and use _selectedProducts
        // to construct a list of Product objects, which have been loaded to basic level only
        // This is then passed down to the review screen which will show thumbnails.

        Actions.SelectProducts _selectedProducts;
        frmProductReview frmProductReview;

        public frmProductReviewSelection()
        {
            _selectedProducts = new Actions.SelectProducts();
            InitializeComponent();
        }

        private void btnShowProducts_Click(object sender, RoutedEventArgs e)
        {

            frmProductReview = new frmProductReview(_selectedProducts);
            frmProductReview.ShowDialog();
        }



    }
}
