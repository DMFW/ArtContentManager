using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtContentManager.Static
{
    internal static class ScanProgress
    {
        // Write Only properties
        private static int totalFileCount;
        private static int currentFileCount;

        // Read Only properties
        private static int completionPct;

        // Read/Write properties
        private static string fileName;
        private static string directoryName;
        private static string message;

        public static int TotalFileCount
        {
            set { totalFileCount = value; }
        }

        public static int CurrentFileCount
        {
            set { currentFileCount = value; }
        }

        public static int CompletionPct 
        {
            get
            {
                if (totalFileCount == 0) { completionPct = 0; } else { completionPct = 100 * currentFileCount / totalFileCount; }
                return completionPct;
            }           
        }

        public static string FileName
        {
            get { return fileName; }
            set { fileName = value; }
        }

        public static string DirectioryName
        {
            get { return directoryName; }
            set { directoryName = value; }
        }

        public static string Message
        {
            get { return message; }
            set { message = value; }
        }

    }
}
