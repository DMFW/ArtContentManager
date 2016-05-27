using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtContentManager.Actions
{
    class FilterExpressionRow
    {
        int _rowNumber;
        string _rowConjunction = "";
        string _rowOperator = "";
        FilterField _rowField;
        string _rowOpenBracket = "";
        string _rowCloseBracket = "";

        public int RowNumber
        {
            get { return _rowNumber; }
            set { _rowNumber = value; }
        }

        public string RowConjunction
        {
            get { return _rowConjunction; }
            set { _rowConjunction = value; }
        }

        public string RowOperator
        {
            get { return _rowOperator; }
            set { _rowOperator = value; }
        }

        public string RowOpenBracket
        {
            get { return _rowOpenBracket; }
            set { _rowOpenBracket = value; }
        }

        public FilterField RowField
        {
            get { return _rowField; }
            set { _rowField = value; }
        }

        public string RowCloseBracket
        {
            get { return _rowCloseBracket; }
            set { _rowCloseBracket = value; }
        }

    }
}
