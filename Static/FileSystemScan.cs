using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Data.SqlClient;
using System.IO;

namespace ArtContentManager.Static
{
    static internal class FileSystemScan
    {
        public static int fileCount;
        public static int knownFileTotalCount = 0;

        public enum ScanMode { smCount, smImport };
        public static ScanMode scanMode = ScanMode.smCount;

        public static void Scan(string root, BackgroundWorker bw)
        {

            Database.LoadScanReferenceData();

            string phase;
            ArtContentManager.DatabaseAgents.dbaFile dbaFile = null;
            DateTime scanDateTime = DateTime.Now; // This is the time the scan was initiated and the same date & time should be used throughout. 
            
            switch (scanMode)
            {
                case ScanMode.smCount :
                    phase = "counting";
                    break;
                case ScanMode.smImport :
                    phase = "importing";
                    dbaFile = new ArtContentManager.DatabaseAgents.dbaFile();
                    break;
                default:
                    phase = "unknown";
                    break;
            }

            fileCount = 0;
            ScanProgress.TotalFileCount = knownFileTotalCount;
            ScanProgress.CurrentFileCount = 0;

            ScanProgress.DirectioryName = "";
            ScanProgress.FileName = "";
            ScanProgress.Message = "Scan starting for " + root;
            bw.ReportProgress(ScanProgress.CompletionPct);

            // Data structure to hold names of subfolders to be 
            // examined for files.

            Stack<string> dirs = new Stack<string>(20);

            if (!System.IO.Directory.Exists(root))
            {
                throw new ArgumentException();
            }
            dirs.Push(root);

            while (dirs.Count > 0)
            {
                string currentDir = dirs.Pop();

                if (bw.CancellationPending)
                {
                    ScanProgress.Message = "Scan cancelled before directory " + currentDir + " in the " + phase + " phase";
                    return;
                }

                ScanProgress.DirectioryName = currentDir;
                ScanProgress.FileName = "";
                bw.ReportProgress(ScanProgress.CompletionPct);

                string[] subDirs;
                try
                {
                    subDirs = System.IO.Directory.GetDirectories(currentDir);
                }
                // An UnauthorizedAccessException exception will be thrown if we do not have 
                // discovery permission on a folder or file. It may or may not be acceptable  
                // to ignore the exception and continue enumerating the remaining files and  
                // folders. It is also possible (but unlikely) that a DirectoryNotFound exception  
                // will be raised. This will happen if currentDir has been deleted by 
                // another application or thread after our call to Directory.Exists. The  
                // choice of which exceptions to catch depends entirely on the specific task  
                // you are intending to perform and also on how much you know with certainty  
                // about the systems on which this code will run. 
                catch (UnauthorizedAccessException e)
                {
                    Trace.WriteLine(e.Message);
                    continue;
                }
                catch (System.IO.DirectoryNotFoundException e)
                {
                    Trace.WriteLine(e.Message);
                    continue;
                }

                string[] files = null;
                try
                {
                    files = System.IO.Directory.GetFiles(currentDir);
                }
                catch (UnauthorizedAccessException e)
                {

                    Trace.WriteLine(e.Message);
                    continue;
                }

                catch (System.IO.DirectoryNotFoundException e)
                {
                    Trace.WriteLine(e.Message);
                    continue;
                }

                // Perform the required action on each file here. 
                // Modify this block to perform your required task.
  
                if (scanMode == ScanMode.smCount)
                {
                    fileCount = fileCount + files.Length; // Always count the files
                    ScanProgress.CurrentFileCount = fileCount;
                    ScanProgress.Message = "Directory " + currentDir + " [+" + files.Length + "] -> " + fileCount;
                    bw.ReportProgress(ScanProgress.CompletionPct); // This will be zero 
                }

                if (scanMode == ScanMode.smImport)
                {
                    foreach (string file in files)
                    {
                        fileCount++;
                        ScanProgress.CurrentFileCount = fileCount;

                        if (bw.CancellationPending)
                        {
                            ScanProgress.Message = "Scan cancelled before file " + file + " in the " + phase + " phase";
                            bw.ReportProgress(ScanProgress.CompletionPct);
                            return;
                        }

                        try
                        {
                            if (Database.ExcludedFiles.ContainsKey(file))
                            {
                                ScanProgress.Message = "Skipping " + file;
                            }
                            else
                            {
                                System.IO.FileInfo fi = new System.IO.FileInfo(file);
                                Trace.WriteLine(String.Format("{0}: {1}, {2}", fi.Name, fi.Length, fi.CreationTime));
                                ScanProgress.Message = "Importing " + file;

                                ArtContentManager.Content.File currentFile = new Content.File(fi, file);
                                Debug.Assert(dbaFile != null); // We should have a perisistent local instance initialised outside the loop which will be caching query definitions
                                currentFile.Save(dbaFile, scanDateTime);

                                Trace.WriteLine(currentFile.ActivePathAndName + " " + currentFile.Checksum);
                            } 
                            bw.ReportProgress(ScanProgress.CompletionPct);
                        }
                        catch (System.IO.FileNotFoundException e)
                        {
                            // If file was deleted by a separate application 
                            //  or thread since the call to TraverseTree() 
                            // then just continue.
                            Trace.WriteLine(e.Message);
                            continue;
                        }
                    }

                    dbaFile.BeginTransaction();
                    dbaFile.UpdateAntiVerifiedFiles(currentDir, scanDateTime);
                    dbaFile.CommitTransaction();

                }

                // Push the subdirectories onto the stack for traversal. 
                // This could also be done before handing the files. 
                foreach (string str in subDirs)
                    dirs.Push(str);
            }

            Database.UnloadScanReferenceData(); // Just to free up some memory
        }

        public static bool IsWritableDirectory(string directory)
        {

            const string TEMP_FILE = "\\tempFile.tmp";
            bool success = false;
            string fullPath = directory + TEMP_FILE;

            if (Directory.Exists(directory))
            {
                try
                {
                    using (FileStream fs = new FileStream(fullPath, FileMode.CreateNew,
                                                                    FileAccess.Write))
                    {
                        fs.WriteByte(0xff);
                    }

                    if (File.Exists(fullPath))
                    {
                        File.Delete(fullPath);
                        success = true;
                    }
                }
                catch (Exception)
                {
                    success = false;
                }
                return success;
            }
            return false;
        }
    }
}
