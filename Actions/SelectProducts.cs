using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ArtContentManager.Static.DatabaseAgents;

namespace ArtContentManager.Actions
{
    public class SelectProducts
    {
        public SelectProducts()
        {
            // Add some dummy products for now.

            _selectedProducts = new List<Content.Product>();

            dbaProduct.ProductLoadOptions simpleLoad = new dbaProduct.ProductLoadOptions();
            simpleLoad.basic = true;

            int[] testIDs = new int[52] {460, 78, 63, 8, 1, 98, 99, 108, 127, 65, 111, 231, 183, 157, 156, 126, 114, 268, 466, 115, 150, 301, 77, 9, 110, 109, 453, 290, 125, 237, 76, 154, 369, 309, 69, 61, 67, 2, 230, 228, 235, 124, 122, 123, 221, 247, 249, 120, 166, 121, 107, 151 };

            foreach (int ID in testIDs)
            {
                Content.Product Test = new Content.Product();
                Test.ID = ID;
                ArtContentManager.Static.DatabaseAgents.dbaProduct.Load(Test, simpleLoad);
                _selectedProducts.Add(Test);
            }
        }

        List<Content.Product> _selectedProducts;

        public List<Content.Product> SelectedProducts
        {
           get { return _selectedProducts; }
        }
    }
}
