﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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

        public int ID
        {
            get { return _ID; }
            set { _ID = value; }
        }

        public string CreatorNameCode
        {
            get { return "" + _CreatorNameCode; }
            set { _CreatorNameCode = value; }
        }

        public string CreatorTrueName
        {
            get { return "" + _CreatorTrueName; }
            set { _CreatorTrueName = value; }
        }

        public string CreatorDirectoryName
        {
            get { return "" + _CreatorDirectoryName; }
            set { _CreatorDirectoryName = value; }
        }

        public string ContactEmail
        {
            get { return "" + _ContactEmail; }
            set { _ContactEmail = value; }
        }

        public string CreatorURI
        {
            get { return "" + _CreatorURI; }
            set { _CreatorURI = value; }
        }
        public string Notes
        {
            get { return "" + _Notes; }
            set { _Notes = value; }
        }

    }
}
