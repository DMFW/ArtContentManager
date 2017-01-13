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
    /// Interaction logic for frmOrganisation.xaml
    /// </summary>
    public partial class frmOrganisation : SkinableWindow
    {

        public frmOrganisation()
        {
            InitializeComponent();
        }

        private void SkinableWindow_Loaded(object sender, RoutedEventArgs e)
        {
            ArtContentManager.Static.DatabaseAgents.dbaMarketPlaces.LoadMarketPlaces(true);
            dgMarketPlaces.DataContext = ArtContentManager.Static.DatabaseAgents.dbaMarketPlaces.tblMarketPlaces;

            ArtContentManager.Static.DatabaseAgents.dbaContentCreators.LoadContentCreators(true);
            dgContentCreators.DataContext = ArtContentManager.Static.DatabaseAgents.dbaContentCreators.tblContentCreators;
        }
    }
}
