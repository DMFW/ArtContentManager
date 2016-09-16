using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;

namespace ArtContentManager.Static
{
    public static class ProductImageManager
    {

        // This static class contains routines to find the locations of image files associated with
        // a product and is used by the product class to build its own private dictionary of such images.
        // It also has an independent role in managing the tidy up of images that have been copied and renamed
        // when a product was renamed, ensuring that the images with the old names are deleted when
        // the program shuts down. Deleting them earlier is difficult due to locks in image controls.

        public const short SUBFOLDER_NAME_LENGTH = 3;
        public const short IMAGE_SUFFIX_KEY_LENGTH = 3;
        public const short PRODUCT_NAME_KEY_LENGTH = 10;

        private static List<Tuple<string, string>> _lstRenamedProducts = new List<Tuple<string,string>>();

        public static void RenameProductImages(string oldProductName, string newProductName)
        {

            CopyImagesToNewName(oldProductName, newProductName);

            // Record the old name and new name of a renamed product so we can remove the old named images later
            // But first look at existing rename instructions.

            // If we find one that is renaming FROM our new product name, remove that from the list
            // because we don't want to delete these images.
            // Example:- Product A renamed to B, then renamed to C and finally renamed back to A
            // Copies of images will exist with the names A, B and C and it is B and C we need to delete
            // We should not delete A. Check the list for an item that would trigger a delete of A
            // and remove it, when A has become a new product name. There can only be one such item
            // at most, no matter how long the cycle of renaming is, since you cannot rename from
            // an old name to a new name twice without renaming back to the old name at some point.
            // As soon as you rename back to an old name the logic below is triggered

            int renameIndex = 0;
            foreach (Tuple<string,string> renamedProductTuple in _lstRenamedProducts)
            {
                if (renamedProductTuple.Item1 == newProductName)
                {
                    // An old product name is now this new one. 
                    // We should not delete images from the name as per the original tuple added to the list, so remove it.
                    _lstRenamedProducts.RemoveAt(renameIndex);
                    break;
                }
                renameIndex++;
            }

            _lstRenamedProducts.Add(new Tuple<string,string>(oldProductName, newProductName));

        }

        private static void CopyImagesToNewName(string oldProductName, string newProductName)
        {

            // Checks to see if a product has been renamed and if so, renames and moves image resources

            if (oldProductName == newProductName)
            {
                // Nothing to do so bail out...
                return;
            }

            if (!Directory.Exists(ImageFolder(newProductName)))
            {
                Directory.CreateDirectory(ImageFolder(newProductName));
            }

            Dictionary<string, string> dctOldImageNames = ImageFiles(oldProductName);

            foreach (string oldImageFileName in dctOldImageNames.Values)
            {
                // First amend the full path to point to the new directory
                string newImageFileName = oldImageFileName.Replace(ImageFolder(oldProductName), ImageFolder(newProductName));
                // Now change the name of the file itself
                newImageFileName = newImageFileName.Replace(oldProductName.ToLowerInvariant(), newProductName.ToLowerInvariant());

                if (System.IO.File.Exists(newImageFileName))
                {
                    Trace.Write("Image file " + newImageFileName + " already existed and was not copied forward in product rename from " + oldImageFileName);
                }
                else
                {
                    System.IO.File.Copy(oldImageFileName, newImageFileName);
                }
            }
        }

        public static void DeleteOldProductNameImages()
        {
            bool deleteOK;
            System.Windows.MessageBoxResult deleteFailReply = System.Windows.MessageBoxResult.Yes;

            foreach (Tuple<string, string> renamedProductTuple in _lstRenamedProducts)
            {
                string oldProductName = renamedProductTuple.Item1;
                string newProductName = renamedProductTuple.Item2;

                Dictionary<string, string> dctOldImages = ImageFiles(oldProductName);
                Dictionary<string, string> dctNewImages = ImageFiles(newProductName);

                foreach (string oldImageFileKey in dctOldImages.Keys)
                {
                    string oldImageFileName = dctOldImages[oldImageFileKey];

                    if (!System.IO.File.Exists(dctNewImages[oldImageFileKey]))
                    {
                        Trace.WriteLine("A new image copy against the product name " + newProductName + " cannot be found for " + oldImageFileKey + Environment.NewLine + "Delete operation ignored for " + oldImageFileName);
                    }
                    else
                    {
                        string newImageFileName = dctNewImages[oldImageFileKey];
                        do
                        {
                            try
                            {
                                if (oldImageFileName.ToLowerInvariant() == newImageFileName.ToLowerInvariant())
                                {
                                    Trace.WriteLine("The new and old image names are the same except for case differences. Deleting of old image + " + oldImageFileName + " bypassed.");
                                    deleteOK = true; // Because logically this is OK and equivalent to a successful delete
                                }
                                else
                                {
                                    System.IO.File.Delete(oldImageFileName);
                                    deleteOK = true;
                                }
                            }
                            catch (Exception e)
                            {
                                deleteOK = false;
                                deleteFailReply = System.Windows.MessageBox.Show("A supporting image file (" + oldImageFileName + ") could not be deleted after copying and renaming the product." + Environment.NewLine + e.Message + Environment.NewLine + "Retry the delete?", "File Delete", System.Windows.MessageBoxButton.YesNo);
                            }
                        } while ((deleteOK == false) && (deleteFailReply == System.Windows.MessageBoxResult.Yes));
                    }
                }
            }
            _lstRenamedProducts.Clear();
        }

        public static Dictionary<string, string> ImageFiles(string productName)
        {
            // Returns a dictionary of images, indexed by a role key 
            // The role key is a three charcter string which is either a numeric presentation order
            // or the coded value "tbn" for a thumb nail image.

            // The location is determined by the productName. The routine allows the value
            // passed for the name to be specified as a parameter so that we can used the routine
            // when moving image items from one name to another. 

            Dictionary<string, string> dctImageFiles = new Dictionary<string, string>();

            // The position where we start our three character index (000, 001 ... or tbn for Thumbnail)
            int startOfImageIndex = ImageFolder(productName).Length + productName.Length + 1;

            System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(ImageFolder(productName));

            if (dir.Exists)
            {
                IEnumerable<System.IO.FileInfo> fileList = dir.GetFiles("*.*", System.IO.SearchOption.AllDirectories);

                var queryMatchingFiles =
                from file in fileList
                where (file.Name.Length == productName.Length + IMAGE_SUFFIX_KEY_LENGTH + file.Extension.Length) && (file.Name.Substring(0, productName.Length).ToLowerInvariant() == productName.ToLowerInvariant())
                select file.FullName;

                foreach (string filename in queryMatchingFiles)
                {
                    dctImageFiles.Add(filename.Substring(startOfImageIndex, IMAGE_SUFFIX_KEY_LENGTH), filename.ToLowerInvariant());
                }
            }
            return dctImageFiles;

        }

        public static string Thumbnail(string productName)
        {

            string folderName = ImageFolder(productName);
            string thumbNailSearch = productName + "tbn.*";

            System.IO.DirectoryInfo dir = new System.IO.DirectoryInfo(folderName);

            if (dir.Exists)
            {
                IEnumerable<System.IO.FileInfo> fileList = dir.GetFiles(thumbNailSearch, System.IO.SearchOption.AllDirectories);

                foreach (System.IO.FileInfo fileInfo in fileList)
                {
                    // There should only be one of these but if there are more, just return the first one
                    return fileInfo.FullName;
                }
            }
            else
            {
                return "";
            }

            return "";
        }

        private static string ImageFolder(string productName)
        {
            string subFolderName;

            if (productName.Length >= SUBFOLDER_NAME_LENGTH)
            {
                subFolderName = productName.Substring(0, SUBFOLDER_NAME_LENGTH);
            }
            else
            {
                subFolderName = "___";
            }

            return Static.DatabaseAgents.dbaSettings.Setting("ImageStoreFolder").Item1 + @"\" + subFolderName;

        }

    }
}
