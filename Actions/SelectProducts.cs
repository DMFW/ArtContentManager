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

            int[] testIDs = new int[6] { 249, 120, 166, 121, 107, 151 };

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
