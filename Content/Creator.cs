using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Data;

namespace ArtContentManager.Content
{
    public class Creator
    {
        private int _CreatorID;
        private string _CreatorNameCode;
        private string _CreatorTrueName;
        private string _CreatorDirectoryName; // Because their name code or true name may not be a valid directory name
        private string _CreatorURI;
        private string _ContactEmail;
        private string _Notes;
        private ObservableCollection<Product> _obcProductCredits;

        private int _productCount; // Virtual result obtained dynamically

        public event PropertyChangedEventHandler PropertyChanged;

        // This method is called by the Set accessor of each property.
        // The CallerMemberName attribute that is applied to the optional propertyName
        // parameter causes the property name of the caller to be substituted as an argument.
        private void NotifyPropertyChanged([CallerMemberName] String propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        [DataRowField]
        public int CreatorID
        {
            get { return _CreatorID; }
            set {
                if (value != _CreatorID)
                {
                    _CreatorID = value;
                    NotifyPropertyChanged();
                }
            }
        }

        [DataRowField]
        public string CreatorNameCode
        {
            get { return "" + _CreatorNameCode; }
            set {
                if (value != _CreatorNameCode)
                {
                    _CreatorNameCode = value;
                    NotifyPropertyChanged();
                }
            }
        }

        [DataRowField]
        public string CreatorTrueName
        {
            get { return "" + _CreatorTrueName; }
            set
            {
                if (value != _CreatorNameCode)
                {
                    _CreatorTrueName = value;
                    NotifyPropertyChanged();
                }
            }
        }

        [DataRowField]
        public string CreatorDirectoryName
        {
            get { return "" + _CreatorDirectoryName; }
            set {
                if (value != _CreatorDirectoryName)
                {
                    _CreatorDirectoryName = value;
                    NotifyPropertyChanged();
                }
            }
        }

        [DataRowField]
        public string ContactEmail
        {
            get { return "" + _ContactEmail; }
            set {
                if (value != _ContactEmail)
                {
                    _ContactEmail = value;
                    NotifyPropertyChanged();
                }
            }
        }

        [DataRowField]
        public string CreatorURI
        {
            get { return "" + _CreatorURI; }
            set
            {
                if (value != _CreatorURI)
                {
                    _CreatorURI = value;
                    NotifyPropertyChanged();
                }
            }
        }

        [DataRowField]
        public string Notes
        {
            get { return "" + _Notes; }
            set
            {
                if (value != _Notes)
                {
                    _Notes = value;
                    NotifyPropertyChanged();
                }
            }
        }
        public int ProductCount
        {
            get { return _productCount; }
        }

        public ObservableCollection<Product> ProductCredits 
        {
            get { return _obcProductCredits; }
        }   

        public void ResetToUnused()
        {
            this.CreatorNameCode = "[ID " + CreatorID + "] Unused";
            this.CreatorTrueName = "";
            this.ContactEmail = "";
            this.CreatorDirectoryName = "";
            this.CreatorURI = "";
            this.Notes = "Creator was remapped at " + DateTime.Now + " and the ID can be edited and reused";
        }

        public void LoadProductCount()
        {
            // We only do this on request as it is "expensive". It initialises a property used in detail display

            _productCount = Static.DatabaseAgents.dbaContentCreators.ProductCount(this);

        }

        public void LoadProductCreditsList()
        {
            _obcProductCredits = Static.DatabaseAgents.dbaContentCreators.LoadProductCreditsList(this);
        }

        public void Save()
        {
            Static.Database.BeginTransaction(ArtContentManager.Static.Database.TransactionType.Active);

            if (this.CreatorID == 0)
            {
                // Insert a new content creator record
                Static.DatabaseAgents.dbaContentCreators.RecordContentCreator(this);
            }
            else
            {
                // Update an existing content creator record
                Static.DatabaseAgents.dbaContentCreators.UpdateContentCreator(this);
            }

            Static.Database.CommitTransaction(ArtContentManager.Static.Database.TransactionType.Active);
        }

    }
}
