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
using System.Data;

namespace ArtContentManager.Forms
{
    /// <summary>
    /// Interaction logic for frmContentProviders.xaml
    /// </summary>
    public partial class frmContentCreatorsSelect : SkinableWindow
    {
        Dictionary<int, Content.Creator> _dctExistingProductCreators;

        public frmContentCreatorsSelect(Dictionary<int, Content.Creator> dctExistingProductCreators)
        {
 
            InitializeComponent();

            // Load all content creators
            Static.DatabaseAgents.dbaContentCreators.LoadContentCreators(true);

            // Flag the ones that belong to the requested product as already selected
            _dctExistingProductCreators = dctExistingProductCreators;
            foreach (Content.Creator productCreator in dctExistingProductCreators.Values)
            {
                DataRow IDRow = Static.DatabaseAgents.dbaContentCreators.tblContentCreators.AsEnumerable()
                    .SingleOrDefault(r => r.Field<int>("CreatorID") == productCreator.ID);

                if (IDRow != null)
                {
                    IDRow.SetField("IsSelected", "True");
                }
                else
                {
                    // Something has gone wrong. We should always have the product creator in the full list of all creators
                    System.Diagnostics.Debug.Assert(true == false);
                }
                
            }
            dgContentCreators.DataContext = Static.DatabaseAgents.dbaContentCreators.tblContentCreators.DefaultView;
        }

        private void btnEditCreator_Click(object sender, RoutedEventArgs e)
        {
        }
        private void btnSelectCreator_Click(object sender, RoutedEventArgs e)
        {
            if (Static.DatabaseAgents.dbaContentCreators.tblContentCreators.Rows[dgContentCreators.SelectedIndex]["IsSelected"].ToString() == "True")
            {
                Static.DatabaseAgents.dbaContentCreators.tblContentCreators.Rows[dgContentCreators.SelectedIndex]["IsSelected"] = "False";
            }
            else
            {
                Static.DatabaseAgents.dbaContentCreators.tblContentCreators.Rows[dgContentCreators.SelectedIndex]["IsSelected"] = "True";
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
