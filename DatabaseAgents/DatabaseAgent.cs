using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SqlClient;
using System.Diagnostics;

namespace ArtContentManager.DatabaseAgents
{
    class DatabaseAgent
    {
        protected SqlTransaction _trnActive;

        public void BeginTransaction()
        {
            _trnActive = ArtContentManager.Static.Database.DB.BeginTransaction(System.Data.IsolationLevel.ReadUncommitted);
        }

        public SqlTransaction trnActive
        {
            get { return _trnActive; }
        }

        public void CommitTransaction()
        {
            _trnActive.Commit();
            _trnActive.Dispose();
        }

        public void RollbackTransaction()
        {
            _trnActive.Rollback();
            _trnActive.Dispose();
        }

    }
}
