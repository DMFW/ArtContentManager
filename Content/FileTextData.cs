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
        // A simple class to hold extracted results in denormalised format from analysing text files

        private string _productName;

        private string _vendorFullName; // A working name

        private string _vendorNameCode;
        private string _vendorName;

        public string ProductName
        {
            get { return _productName; }
            set { _productName = value; }
        }

        public string VendorNameCode
        {
            get { return _vendorNameCode; }
            set { _vendorNameCode = value; }
        }

        public string VendorName
        {
            get { return _vendorName; }
            set { _vendorName = value; }
        }

        public void ParseText(string allText)
        {

            // Do our best to extract the atomic properties that may be available in the text file.

            Match patternMatch;

            // First attempt to extract the product name

            Regex objProductNamePattern01 = new Regex(@"(?<=Product Name:|Product Title:|Productname:)(.*?)(?=\n)");

            if (objProductNamePattern01.IsMatch(allText))
            {
                patternMatch = objProductNamePattern01.Match(allText);
                _productName = patternMatch.ToString().Trim();
                goto ProductNameResolved;
            }

            ProductNameResolved:

            // First attempt to extract the vendor name

            Regex objVendorNamePattern01 = new Regex(@"(?<=Vendor Name:|Author:)(.*?)(?=\n)");

            if (objVendorNamePattern01.IsMatch(allText))
            {
                patternMatch = objVendorNamePattern01.Match(allText);
                _vendorFullName = patternMatch.ToString().Trim();

                if ((_vendorFullName.IndexOf("(") != 0) & (_vendorFullName.IndexOf(")") != 0))
                {
                    // Deals with e.g. Vendor Name: johnbarker (Barker)
                    // or              Author: Atenais (Liudmila Metaeva)
                    //
                    // (The name code is first in both these examples, then the "true" name in brackets)

                    _vendorNameCode = _vendorFullName.Substring(0, _vendorFullName.IndexOf("(") - 1);

                    int codeLength = _vendorFullName.IndexOf(")") - _vendorFullName.IndexOf("(") - 1;
                    _vendorName = _vendorFullName.Substring(_vendorFullName.IndexOf("(") + 1, codeLength);
                    
                }
                else
                {
                    // Assume if only one string is supplied it is the name code.
                    _vendorNameCode = _vendorFullName;
                }
                goto VendorNameResolved;
            }

            // A second stab at the vendor name is searching for "created by" and looking for the following word.

            Regex objVendorNamePattern02 = new Regex(@"(?<=created by\s)(?<word>\b\S+\b)");

            if (objVendorNamePattern02.IsMatch(allText))
            {
                patternMatch = objVendorNamePattern02.Match(allText);
                _vendorNameCode = patternMatch.ToString().Trim();
                goto VendorNameResolved;
            }

            // A third and fourth stab at the vendor name is using the documentation subfolder name with forward and backslash delimitors
            // Note that in this scenario and all following ones we are going to assume the name code is directly available
            // and we do not attempt to break up brackets as per the previous more complex example. 

            Regex objVendorNamePattern03 = new Regex(@"(?<=Documentation/)(.*?)(?=\n)");

            if (objVendorNamePattern03.IsMatch(allText))
            {
                patternMatch = objVendorNamePattern03.Match(allText);
                _vendorNameCode = patternMatch.ToString().Trim(new Char[] { ' ', '\\', '/', '\r' });  // Trim may need to remove directory character as well
                if (_vendorNameCode.Length > 0) { goto VendorNameResolved; }                    // Unless we have a non-zero string this didn't work so try the next method
            }

            Regex objVendorNamePattern04 = new Regex(@"(?<=Documentation\\)(.*?)(?=\n)");

            if (objVendorNamePattern04.IsMatch(allText))
            {
                patternMatch = objVendorNamePattern04.Match(allText);
                _vendorNameCode = patternMatch.ToString().Trim(new Char[] { ' ', '\\', '/', '\r' });  // Trim may need to remove directory character as well
                if (_vendorNameCode.Length > 0) { goto VendorNameResolved; }                    // Unless we have a non-zero string this didn't work so try the next method
            }

            VendorNameResolved:
            return;

        }

    }
}
