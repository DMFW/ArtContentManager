using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtContentManager.Actions
{
    class FilterField
    {
        string _fieldName;
        string _fieldValue;
        string _fieldNameDescription;
        string _fieldValueDescription;

        public string FieldName
        {
            get { return _fieldName; }
            set { _fieldName = value; }
        }

        public string FieldValue
        {
            get { return _fieldValue; }
            set { _fieldValue = value; }
        }

        public string FieldNameDescription
        {
            get { return _fieldNameDescription; }
            set { _fieldNameDescription = value; }
        }

        public string FieldValueDescription
        {
            get { return _fieldValueDescription; }
            set { _fieldValueDescription = value; }
        }

    }
}
