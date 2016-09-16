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
    /// Interaction logic for frmTools.xaml
    /// </summary>
    public partial class frmTools : SkinableWindow
    {

        private Actions.CreatorRemap _creatorRemap; 

        public frmTools()
        {
            _creatorRemap = new Actions.CreatorRemap();
            InitializeComponent();
        }

        private void btnSelectOriginalContentCreator_Click(object sender, RoutedEventArgs e)
        {
            Dictionary<int, Content.Creator> dctProductCreators = new Dictionary<int, Content.Creator>();

            if (_creatorRemap.SourceCreator != null)
            {
                dctProductCreators.Add(_creatorRemap.SourceCreator.CreatorID, _creatorRemap.SourceCreator);
            }

            frmContentCreatorsSelect frmContentCreators = new frmContentCreatorsSelect(dctProductCreators);
            frmContentCreators.SingleSelect = true;

            frmContentCreators.ShowDialog();

            // Single select should enforce this.

            System.Diagnostics.Debug.Assert(Static.DatabaseAgents.dbaContentCreators.SelectedContentCreators().Count <= 1);
            _creatorRemap.SourceCreator = null;

            foreach (Content.Creator selectedCreator in Static.DatabaseAgents.dbaContentCreators.SelectedContentCreators())
            {
                _creatorRemap.SourceCreator = selectedCreator;
            }

            this.DataContext = _creatorRemap.SourceCreator;
        }

        private void btnSelectMapCreators_Click(object sender, RoutedEventArgs e)
        {
            Dictionary<int, Content.Creator> dctProductCreators = new Dictionary<int, Content.Creator>();

            foreach (Content.Creator productCreator in _creatorRemap.TargetCreators)
            {
                dctProductCreators.Add(productCreator.CreatorID, productCreator);
            }

            frmContentCreatorsSelect frmContentCreators = new frmContentCreatorsSelect(dctProductCreators);
            frmContentCreators.ShowDialog();

            _creatorRemap.TargetCreators.Clear();
            foreach (Content.Creator selectedCreator in Static.DatabaseAgents.dbaContentCreators.SelectedContentCreators())
            {
                _creatorRemap.TargetCreators.Add(selectedCreator);
            }

            this.dgTargetCreators.DataContext = _creatorRemap;
        }

        void btnViewCreator_Click(object sender, RoutedEventArgs e)
        {
            // Launch the detail view form directly here

            Content.Creator creatorToView = _creatorRemap.TargetCreators[dgTargetCreators.SelectedIndex];

            frmContentCreatorDetail frmContentCreatorDetail = new frmContentCreatorDetail(creatorToView);
            frmContentCreatorDetail.ShowDialog();
        }

        private void btnMap_Click(object sender, RoutedEventArgs e)
        {

            if ((_creatorRemap.SourceCreator == null) || (_creatorRemap.TargetCreators == null))
            {
                MessageBox.Show("Please select a source creator and one or more target creators to remap", "Creator remap", MessageBoxButton.OK);
                return;
            }

            MessageBoxResult msgBoxResult = MessageBox.Show("Please confirm remapping from selected source creator to target creators", "Creator remap", MessageBoxButton.OKCancel);

            if (msgBoxResult == MessageBoxResult.OK)
            {
                _creatorRemap.Remap();
            }
        }

        private void btnCancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
