using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtContentManager.Content
{
    public class ImageResource
    {
        private System.Drawing.Image _ImageContent;

        public System.Drawing.Image ImageContent
        {
            get { return _ImageContent; }
            set { _ImageContent = value; }
        }

    }
}
