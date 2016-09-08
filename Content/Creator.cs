using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ArtContentManager.Content
{
    public class Creator
    {
        private int _ID;
        private string _CreatorNameCode;
        private string _CreatorTrueName;
        private string _CreatorDirectoryName; // Because their name code or true name may not be a valid directory name
        private string _CreatorURI;
        private string _ContactEmail;
        private string _Notes;

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
        public int ID
        {
            get { return _ID; }
            set { _ID = value; }
        }

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

        public void Save()
        {
            Static.Database.BeginTransaction(ArtContentManager.Static.Database.TransactionType.Active);

            if (this.ID == 0)
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
