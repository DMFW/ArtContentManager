using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtContentManager.Actions
{
    class FilterExpression
    {

        private SortedList<int, FilterExpressionRow> _lstFilterExpressionRows;
        private List<string> validationErrors; 

        public bool ExpressionIsValid()
        {

            validationErrors = new List<string>();
            int bracketDepth = 0;
            int rowCount = 0;

            foreach (FilterExpressionRow filterExpressionRow in _lstFilterExpressionRows.Values)
            {
                rowCount++;

                if (filterExpressionRow.RowOpenBracket == "(")
                {
                    bracketDepth++;
                }

                if (filterExpressionRow.RowOpenBracket == ")")
                {
                    bracketDepth--;
                }

                if (rowCount == 1)
                {
                    if (filterExpressionRow.RowConjunction != "")
                    {
                        validationErrors.Add("The first row of the expression should not contain a conjunction");
                    }
                }

            }

            if (bracketDepth != 0)
            {
                validationErrors.Add("The count of open and close brackets for the whole expression is not equal");
            }

            if (validationErrors.Count > 0) { return false; } else { return true; }

        }

        public List<string> ValidationErrors
        {
            get { return validationErrors; }
        }

        public string ResolvedExpressionForSQL()
        {
            string whereClause = "";

            foreach (FilterExpressionRow filterExpressionRow in _lstFilterExpressionRows.Values)
            {
                whereClause = whereClause + filterExpressionRow.RowConjunction + " " +
                    filterExpressionRow.RowOpenBracket +
                    filterExpressionRow.RowField.FieldName +
                    filterExpressionRow.RowOperator +
                    filterExpressionRow.RowField.FieldValue +
                    filterExpressionRow.RowCloseBracket + " ";
            }
            return whereClause;

        }

    }
}
