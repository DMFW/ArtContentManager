using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace ArtContentManager.Content
{
    class ContentLocation
    {
        private int _organisationSchemeID;                   // The organisation scheme ID will always be 1 for a scan content analysis
        private Installation _installation;                  // Which specific installation root we belong to.
        private string _contentDirectoryName;                // The full name of the content directory
        private Content.InstallationType.Category _category; // Encapsuates information about the higher level directory structure that makes this a content location
        private string _contentTagPath;                      // All directories below the category identifier
        private string _contentTag;                          // The last directory in the chain
        private int _subFolderCount;                         // How many sub folders in this direcory? 
        private int _itemCount;                              // How many items in this directory?

        public ContentLocation(int organisationSchemeID, Installation installation, string contentDirectoryName, Content.InstallationType.Category category)
        {
            _organisationSchemeID = organisationSchemeID;
            _installation = installation;
            _contentDirectoryName = contentDirectoryName;
            _category = category;
            DeriveTags();
        }

        private void DeriveTags()
        {
            int startOfRelativePath = _contentDirectoryName.IndexOf(_category.RelativePath) + _category.RelativePath.Length + 1;

            if (startOfRelativePath > _contentDirectoryName.Length)
            {
                _contentTagPath = "";
            }
            else
            {
                _contentTagPath = _contentDirectoryName.Substring(_contentDirectoryName.IndexOf(_category.RelativePath) + _category.RelativePath.Length + 1);
            }

            _contentTag = _contentDirectoryName.Substring(_contentDirectoryName.LastIndexOf(@"\") + 1);
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

        public string ContentDirectoryName
        {
            get { return _contentDirectoryName; }
        }

        public InstallationType.Category Category
        {
            get { return _category; }
        }

        public string ContentTagPath
        {
            get { return _contentTagPath; }
        }

        public string ContentTag
        {
            get { return _contentTag; }
        }

        public int SubFolderCount
        {
            get { return _subFolderCount; }
            set { _subFolderCount = value; }
        }

        public int ItemCount
        {
            get { return _itemCount; }
            set { _itemCount = value; }
        }

    }
}
