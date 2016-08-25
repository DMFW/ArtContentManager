using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using System.IO;

namespace ArtContentManager.Content
{
    public class ImageResource
    {
        private string _FullFileName;
        private string _FileExtension;       

        public ImageResource(string fullFileName)
        {
            _FullFileName = fullFileName;
            if (System.IO.File.Exists(fullFileName))
            {
                _FileExtension = Path.GetExtension(fullFileName);
            }
        }

        public BitmapSource ImageSource
        {
            get
            {
                BitmapSource bitmapSource;
                BitmapDecoder decoder;

                Uri myUri = new Uri(_FullFileName, UriKind.RelativeOrAbsolute);

                switch (_FileExtension.ToLowerInvariant())
                {
                    case ".jpg":
                    case ".jpeg":
                        decoder = new JpegBitmapDecoder(myUri, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                        bitmapSource = decoder.Frames[0];
                        break;
                    case ".bmp":
                        decoder = new BmpBitmapDecoder(myUri, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                        bitmapSource = decoder.Frames[0];
                        break;
                    case ".png":
                        decoder = new PngBitmapDecoder(myUri, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                        bitmapSource = decoder.Frames[0];
                        break;
                    case ".tiff":
                        decoder = new TiffBitmapDecoder(myUri, BitmapCreateOptions.PreservePixelFormat, BitmapCacheOption.OnLoad);
                        bitmapSource = decoder.Frames[0];
                        break;
                    default:
                        // An image type we don't know how to handle and will have to ignore
                        bitmapSource = null;
                        break;
                }
                return bitmapSource;
            }
        }

           
    }
}
