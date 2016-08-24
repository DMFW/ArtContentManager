using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;

namespace ArtContentManager.Content
{
    public class FileTextData
    {
        // A class to hold extracted results in denormalised format from analysing text files

        private string _productName;

        private List<string> _vendorNameCodes;
        private List<string> _vendorNames;
        private List<string> _contactEmails;

        private string[] _splitMarkers = new string[] { " AND ", " and", " & " };

        public string ProductName
        {
            get { return _productName; }
            set { _productName = value; }
        }

        public List<string> VendorNameCodes
        {
            get { return _vendorNameCodes; }
            set { _vendorNameCodes = value; }
        }

        public List<string> VendorNames
        {
            get { return _vendorNames; }
            set { _vendorNames = value; }
        }

        public List<string> ContactEmails
        {
            get { return _contactEmails; }
            set { _contactEmails = value; }
        }

        public void ParseText(string allText)
        {

            bool productNameFound;
            bool vendorNamesFound;
            bool contactEmailsFound;

            _vendorNameCodes = new List<string>();
            _vendorNames = new List<string>();
            _contactEmails = new List<string>();

            productNameFound = ParseProductName(allText);
            vendorNamesFound = ParseVendorNames(allText);
            contactEmailsFound = ParseContactEmails(allText);

            return;

        }

        private bool ParseProductName(string allText)
        {

            Match patternMatch;
            Regex objProductNamePattern01 = new Regex(@"(?<=Product Name:|Product Title:|Productname:)(.*?)(?=\n)");

            if (objProductNamePattern01.IsMatch(allText))
            {
                patternMatch = objProductNamePattern01.Match(allText);
                _productName = patternMatch.ToString().Trim();
                return true;
            }
            else
            {
                return false;
            }
        }

        private bool ParseVendorNames(string allText)
        {

            string vendorNames;
            string vendorNameCode = "";

            Match patternMatch;

            // First attempt to extract the vendor name(s)

            Regex objVendorNamePattern01 = new Regex(@"(?<=Vendor Name:|Author:|By:)(.*?)(?=\n)");

            if (objVendorNamePattern01.IsMatch(allText))
            {
                patternMatch = objVendorNamePattern01.Match(allText);
                vendorNames = patternMatch.ToString().Trim();
                SplitParseVendorNames(vendorNames);
                return true;
            }


            // A second stab at the vendor name is searching for "created by" and looking for the following word.
            // Here we are unable to extract a code/name pair because we only have one word and must assume it is the code.

            Regex objVendorNamePattern02 = new Regex(@"(?<=created by\s)(?<word>\b\S+\b)");

            if (objVendorNamePattern02.IsMatch(allText))
            {
                patternMatch = objVendorNamePattern02.Match(allText);
                vendorNameCode = patternMatch.ToString().Trim();
                if (vendorNameCode.Length > 0) { goto VendorCodeResolved; }
            }

            // A third and fourth stab at the vendor name is using the documentation subfolder name with forward and backslash delimitors
            // Note that in this scenario and all following ones we are going to assume the name code is directly available
            // and we do not attempt to break up brackets as per the previous more complex example. 

            Regex objVendorNamePattern03 = new Regex(@"(?<=Documentation/)(.*?)(?=\n)");

            if (objVendorNamePattern03.IsMatch(allText))
            {
                patternMatch = objVendorNamePattern03.Match(allText);
                vendorNameCode = patternMatch.ToString().Trim(new Char[] { ' ', '\\', '/', '\r' });  // Trim may need to remove directory character as well
                if (vendorNameCode.Length > 0) { goto VendorCodeResolved; }  // Unless we have a non-zero string this didn't work so try the next method
            }

            Regex objVendorNamePattern04 = new Regex(@"(?<=Documentation\\)(.*?)(?=\n)");

            if (objVendorNamePattern04.IsMatch(allText))
            {
                patternMatch = objVendorNamePattern04.Match(allText);
                vendorNameCode = patternMatch.ToString().Trim(new Char[] { ' ', '\\', '/', '\r' });  // Trim may need to remove directory character as well
                if (vendorNameCode.Length > 0) { goto VendorCodeResolved; }  // Unless we have a non-zero string this didn't work so try the next method
            }

            // Nothing worked so we don't know the vendor code or name.

            return false;

        VendorCodeResolved:

            _vendorNameCodes.Add(vendorNameCode);
            _vendorNames.Add("");
            return true;

        }

        private bool SplitParseVendorNames(string vendorNames)
        {
            // We are passed a string that may represent one or more vendors 
            // separated by some split marker strings. Split the string then process each vendor.
            // The individual split strings may themselves represent a compound name so pass
            // the result to ParseVendorFullName to resolve this.

            bool nameFound = false;

            string[] vendorFullNames = vendorNames.Split(_splitMarkers, StringSplitOptions.RemoveEmptyEntries);

            foreach (string vendorFullName in vendorFullNames)
            {
                if (vendorFullName.Trim() != String.Empty)
                {
                    ParseVendorFullName(vendorFullName);
                    nameFound = true;
                }
            }

            return nameFound;
        }

        private bool ParseVendorFullName(string vendorFullName)
        {

            // This routine is passed a candidate vendor name which may be a simple code
            // or may be a code with a true name in brackets after it.

            // If it is a compound string it resolves it into the two parts, otherwise it sets the true name to blank.

            string vendorNameCode;
            string vendorName;

            if ((vendorFullName.IndexOf("(") != -1) & (vendorFullName.IndexOf(")") != -1))
            {
                // Deals with e.g. Vendor Name: johnbarker (Barker)
                // or              Author: Atenais (Liudmila Metaeva)
                //
                // (The name code is first in both these examples, then the "true" name in brackets)

                vendorNameCode = vendorFullName.Substring(0, vendorFullName.IndexOf("(") - 1);

                int codeLength = vendorFullName.IndexOf(")") - vendorFullName.IndexOf("(") - 1;
                vendorName = vendorFullName.Substring(vendorFullName.IndexOf("(") + 1, codeLength);

            }
            else
            {
                // Assume if only one string is supplied it is the name code.
                vendorNameCode = vendorFullName;
                vendorName = "";
            }

            _vendorNameCodes.Add(vendorNameCode);
            _vendorNames.Add(vendorName);

            return true;

        }

        private bool ParseContactEmails(string allText)
        {
            Match patternMatch;
            string allContactEmails;

            Regex objContactEmail01 = new Regex(@"(?<=Contact:|Email:)(.*?)(?=\n)");

            if (objContactEmail01.IsMatch(allText))
            {
                patternMatch = objContactEmail01.Match(allText);
                allContactEmails = patternMatch.ToString();

                string[] contactEmails = allContactEmails.Split(_splitMarkers, StringSplitOptions.RemoveEmptyEntries);

                foreach (string rawContactEmail in contactEmails)
                {
                    string contactEmail = rawContactEmail.Trim(new Char[] { ' ', '\\', '/', '\r' });  // Trim may need to remove directory character as well
                    if (contactEmail.Length > 0) { _contactEmails.Add(contactEmail); }
                }
                return true;
            }
            return false;
        }
    }
}
