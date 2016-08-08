using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ArtContentManager.Content
{
    class ContentType
    {
        private int _organisationSchemeID;
        private Installation _installation;
        private string _directoryPath;
        private string _description;

        public ContentType(int organisationSchemeID, Installation installation, string directoryPath)
        {
            _organisationSchemeID = organisationSchemeID;
            _installation = installation;
            _directoryPath = directoryPath;
            _description = "";
        }

        public int OrganisationSchemeID
        {
            get { return _organisationSchemeID; }
            set { _organisationSchemeID = value; }
        }

        public Installation Installation
        {
            get { return _installation; }
            set { _installation = value; }
        }

        public string DirectoryPath
        {
            get { return _directoryPath; }
            set { _directoryPath = value; }
        }

        public string Description
        {
            get { return _description; }
            set { _description = value; }
        }

    }
}
