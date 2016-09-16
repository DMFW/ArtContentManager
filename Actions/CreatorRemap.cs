using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace ArtContentManager.Actions
{
    class CreatorRemap
    {

        private Content.Creator _sourceCreator;
        private ObservableCollection<Content.Creator> _obcTargetCreators = new ObservableCollection<Content.Creator>();

        public CreatorRemap()
        {
            _obcTargetCreators = new ObservableCollection<Content.Creator>();
        }

        public Content.Creator SourceCreator
        {
            get { return _sourceCreator; }
            set { _sourceCreator = value; }
        }

        public ObservableCollection<Content.Creator> TargetCreators
        {
            get { return _obcTargetCreators; }
            set { _obcTargetCreators = value; }
        }

        public void Remap()
        {

            try
            {

                Static.Database.BeginTransaction(Static.Database.TransactionType.Active);

                foreach (Content.Creator targetCreator in _obcTargetCreators)
                {
                    Static.DatabaseAgents.dbaProduct.MapProductCreator(_sourceCreator.CreatorID, targetCreator.CreatorID);
                }

                Static.DatabaseAgents.dbaProduct.RemoveCreatorFromAllProducts(SourceCreator.CreatorID);

                SourceCreator.ResetToUnused();
                SourceCreator.Save();

                Static.Database.CommitTransaction(Static.Database.TransactionType.Active);

                MessageBox.Show("Remapping complete");
            }
            catch(Exception e)
            {
                Static.Database.RollbackTransaction(Static.Database.TransactionType.Active);
                MessageBox.Show("Remapping failed" + Environment.NewLine + e.Message);
            }
        }

    }
}
