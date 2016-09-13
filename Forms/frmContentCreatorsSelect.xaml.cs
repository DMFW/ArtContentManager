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
        private bool _singleSelect = false; // Set this to true through the property to enforce single selection

        public frmContentCreatorsSelect(Dictionary<int, Content.Creator> dctExistingProductCreators)
        {
 
            InitializeComponent();
            this.Title = "Select Multiple Creators"; // Default is to allow this 

            // Load all content creators
            Static.DatabaseAgents.dbaContentCreators.LoadContentCreators(true);

            // Flag the ones that belong to the requested product as already selected
            _dctExistingProductCreators = dctExistingProductCreators;
            foreach (Content.Creator productCreator in dctExistingProductCreators.Values)
            {
                DataRow IDRow = Static.DatabaseAgents.dbaContentCreators.tblContentCreators.AsEnumerable()
                    .SingleOrDefault(r => r.Field<int>("CreatorID") == productCreator.CreatorID);

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

        public bool SingleSelect
        {
            get { return _singleSelect; }
            set {    _singleSelect = value;
                    if (_singleSelect == true)
                    {
                        this.Title = "Select Single Creator";
                    }
                        else
                    {
                        this.Title = "Select Multiple Creators";
                    }
            }
        }

        private void btnAddCreator_Click(object sender, RoutedEventArgs e)
        {
            Content.Creator creatorToAdd = new Content.Creator();
            frmContentCreatorDetail frmContentCreatorDetail = new frmContentCreatorDetail(creatorToAdd);
            frmContentCreatorDetail.ShowDialog();

            Static.DatabaseAgents.dbaContentCreators.AddObjectToDataTable(creatorToAdd);
            dgContentCreators.Items.Refresh();
        }

        private void btnViewCreator_Click(object sender, RoutedEventArgs e)
        {
            // Launch the detail view form directly here

            Content.Creator creatorToView = new Content.Creator();
            creatorToView.CreatorID = (int)Static.DatabaseAgents.dbaContentCreators.tblContentCreators.Rows[dgContentCreators.SelectedIndex]["CreatorID"];

            Static.DatabaseAgents.dbaContentCreators.Load(creatorToView);
            frmContentCreatorDetail frmContentCreatorDetail = new frmContentCreatorDetail(creatorToView);
            frmContentCreatorDetail.ShowDialog();

            Static.DatabaseAgents.dbaContentCreators.UpdateObjectOnDataTable(creatorToView);
            dgContentCreators.Items.Refresh();
        }
        private void btnSelectCreator_Click(object sender, RoutedEventArgs e)
        {
            if (Static.DatabaseAgents.dbaContentCreators.tblContentCreators.Rows[dgContentCreators.SelectedIndex]["IsSelected"].ToString() == "True")
            {
                Static.DatabaseAgents.dbaContentCreators.tblContentCreators.Rows[dgContentCreators.SelectedIndex]["IsSelected"] = "False";
            }
            else
            {
                if (_singleSelect == true)
                {
                    // Automatically deselect any prior selections

                    var selectedRows = from selectedRow in Static.DatabaseAgents.dbaContentCreators.tblContentCreators.AsEnumerable()
                    where(selectedRow.Field<string>("IsSelected") == "True")
                    select(selectedRow);

                    foreach (var selectedRow in selectedRows)
                    {
                        selectedRow.SetField<string>("IsSelected", "False");
                    }

                }
                Static.DatabaseAgents.dbaContentCreators.tblContentCreators.Rows[dgContentCreators.SelectedIndex]["IsSelected"] = "True";
            }
        }

        private void btnClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

    }
}
