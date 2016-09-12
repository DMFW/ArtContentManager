using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtContentManager.Actions
{
    class CreatorRemap
    {

        private ArtContentManager.Content.Creator _sourceCreator;
        private Dictionary<int, ArtContentManager.Content.Creator> _targetCreators;

        public Content.Creator SourceCreator
        {
            get { return _sourceCreator; }
            set { _sourceCreator = value; }
        }

        public Dictionary<int, ArtContentManager.Content.Creator> TargetCreators
        {
            get { return _targetCreators; }
            set { _targetCreators = value; }
        }

    }
}
