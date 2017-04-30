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

        public enum ScanMode
        {
          smContentTypeImport,
          smFullImportCount,
          smFullImport
        };
 
        private static int _internalZipInstance;
        private static Dictionary<string, Actions.Scan> _folderScan;

        public static void Scan(ScanMode scanMode, bool rescanExistingImports, Actions.Scan rootScan, BackgroundWorker bw)
        {

            Actions.Scan subScan;
            Actions.Scan activeScan;
            DirectoryInfo dirInfo;
            Dictionary<string, Content.Installation> scanInstallations = new Dictionary<string, Content.Installation>();
            Content.Installation currentInstallation = null;

            if (Database.ScanReferenceDataLoaded == false)
            {
                Database.LoadScanReferenceData();
            }

            string phase;
           
            switch (scanMode)
            {
                case ScanMode.smContentTypeImport:
                    phase = "importing categories only";
                    ScanProgress.TotalFileCount = rootScan.TotalFiles;
                    ScanProgress.CurrentFileCount = 0;
                    _folderScan = new Dictionary<string, Actions.Scan>();
                    _folderScan.Add(rootScan.FolderName, rootScan);
                    break;
                case ScanMode.smFullImportCount:
                    phase = "counting";
                    rootScan.TotalFiles = 0;
                    rootScan.NewFiles = 0;
                    rootScan.ProcessedFiles = 0;
                    _folderScan = new Dictionary<string, Actions.Scan>();
                    _folderScan.Add(rootScan.FolderName, rootScan);
                    break;
                case ScanMode.smFullImport:
                    phase = "importing all";
                    ScanProgress.TotalFileCount = rootScan.TotalFiles;
                    ScanProgress.CurrentFileCount = 0;
                    break;
                default:
                    phase = "unknown";
                    break;
            }

            ScanProgress.DirectioryName = "";
            ScanProgress.FileName = "";
            ScanProgress.Message = "Scan starting for " + rootScan.FolderName;
            activeScan = rootScan;
            bw.ReportProgress(ScanProgress.CompletionPct);

            // Data structure to hold names of subfolders to be 
            // examined for files.

            Stack<string> dirs = new Stack<string>();

            if (!System.IO.Directory.Exists(rootScan.FolderName))
            {
                throw new ArgumentException();
            }
            dirs.Push(rootScan.FolderName);

            while (dirs.Count > 0)
            {
                
                string currentDir = dirs.Pop();

                foreach (string installationRootDir in scanInstallations.Keys)
                {
                    if (currentDir.Contains(installationRootDir))
                    {
                        currentInstallation = scanInstallations[installationRootDir];
                    }
                }

                if (currentInstallation == null)
                {
                    // This might return an installation if we don't have one
                    // or it might STILL return null
                    currentInstallation = Installation(currentDir);
                }

                FileInfo[] newFiles;

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

                try
                {
                    dirInfo = new DirectoryInfo(currentDir);
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

                if (rootScan.FolderName != currentDir)
                {
                    switch (scanMode)
                    {
                        case ScanMode.smFullImportCount:
                            subScan = new Actions.Scan();
                            subScan.FolderName = currentDir;
                            subScan.StartScanTime = DateTime.Now;
                            subScan.IsRequestRoot = false;
                            ArtContentManager.Static.DatabaseAgents.dbaScanHistory.SetLastCompletedScanTime(subScan);
                            _folderScan.Add(subScan.FolderName, subScan);
                            activeScan = subScan;
                            break;

                        case ScanMode.smContentTypeImport:
                            subScan = new Actions.Scan();
                            subScan.FolderName = currentDir;
                            subScan.StartScanTime = DateTime.Now;
                            subScan.IsRequestRoot = false;
                            _folderScan.Add(subScan.FolderName, subScan);
                            activeScan = subScan;
                            break;

                        case ScanMode.smFullImport:
                            Debug.Assert(_folderScan.ContainsKey(currentDir.ToUpperInvariant())); // Indicates either a logic error or that someone has been adding folders between the counting and import phases. Just don't do that OK? 
                            subScan = _folderScan[currentDir.ToUpperInvariant()];
                            activeScan = subScan;
                            break;
                    }
                }
                
                // Pure directory level analysis which is for content type analysis
                // whether by specific request or as a by product of a full import 
                     
                if ((scanMode == ScanMode.smContentTypeImport) | (scanMode == ScanMode.smFullImport))
                {
                    // Where there are no subdirectories we are down to the level of an individual product directory
                    // This is not a content type so ignore it and continue

                    ScanProgress.Message = "Directory " + currentDir;

                    if (subDirs.Count() != 0)
                    {

                        if (currentInstallation != null)
                        {
                            Content.InstallationType.Category category = currentInstallation.InstallationCategory(currentDir);

                            if (category != null)
                            {

                                ScanProgress.Message = "Directory " + currentDir + " assigned to category " + category.Name;
                                ArtContentManager.Static.Database.BeginTransaction(Database.TransactionType.Active);
                                Content.ContentLocation contentLocation = new Content.ContentLocation(1, currentInstallation, currentDir, category);

                                contentLocation.SubFolderCount = subDirs.Count();
                                contentLocation.ItemCount = System.IO.Directory.GetFiles(currentDir).Count();

                                DatabaseAgents.dbaContentLocations.RecordContentLocation(contentLocation);
                                ArtContentManager.Static.Database.CommitTransaction(Database.TransactionType.Active);
                            }

                        }
                    }
                }

                // File level analysis, required for counting and full importing but not pure content type

                if (scanMode != ScanMode.smContentTypeImport)
                {

                    if (rescanExistingImports)
                    {
                        newFiles = dirInfo.GetFiles().ToArray();
                    }
                    else
                    {
                        newFiles = dirInfo.GetFiles().Where(p => p.CreationTime > activeScan.PreviousCompletedScanTime).ToArray();
                    }

                    switch (scanMode)
                    {
                        case ScanMode.smFullImportCount:

                            activeScan.TotalFiles += dirInfo.GetFiles().Length;
                            activeScan.NewFiles += newFiles.Length;

                            // When we are not the root, roll subtotals into the root total and add the sub scan
                            // The root scan will be updated outside the loop at the end of the process
                            if (activeScan != rootScan)
                            {
                                rootScan.TotalFiles += activeScan.TotalFiles;
                                rootScan.NewFiles += activeScan.NewFiles;
                                ArtContentManager.Static.DatabaseAgents.dbaScanHistory.RecordStartScan(activeScan);
                                ScanProgress.Message = "Directory " + currentDir + " [+" + activeScan.NewFiles + " (" + activeScan.TotalFiles + ")] -> " + rootScan.NewFiles + " (" + rootScan.TotalFiles + ")";
                            }

                            ScanProgress.CurrentFileCount = rootScan.TotalFiles;
                            bw.ReportProgress(ScanProgress.CompletionPct); // This will be zero

                            if (bw.CancellationPending)
                            {
                                ScanProgress.Message = "Scan cancelled after directory " + currentDir + " in the " + phase + " phase";
                                bw.ReportProgress(ScanProgress.CompletionPct);
                                return;
                            }

                            break;

                        case ScanMode.smFullImport:

                            foreach (FileInfo file in newFiles)
                            {
                                activeScan.ProcessedFiles++;
                                _internalZipInstance = 0;

                                if (activeScan != rootScan)
                                {
                                    rootScan.ProcessedFiles++;
                                }

                                ScanProgress.CurrentFileCount = rootScan.ProcessedFiles;

                                if (bw.CancellationPending)
                                {
                                    ScanProgress.Message = "Scan cancelled before file " + file.Name + " in the " + phase + " phase";
                                    bw.ReportProgress(ScanProgress.CompletionPct);
                                    if (activeScan != rootScan)
                                    {
                                        ArtContentManager.Static.DatabaseAgents.dbaScanHistory.RecordScanAbort(activeScan);
                                    }
                                    return;
                                }

                                try
                                {
                                    if (Database.ExcludedFiles.ContainsKey(file.Name))
                                    {
                                        ScanProgress.Message = "Skipping " + file;
                                    }
                                    else
                                    {
                                        Trace.WriteLine(String.Format("{0}: {1}, {2}", file.Name, file.Length, file.CreationTime));
                                        ScanProgress.Message = "Importing " + file.Name;
                                        // The creation of the file object, also saves it. Everything is encapsulated in the constructor
                                        ArtContentManager.Content.File currentFile = new Content.File(activeScan.StartScanTime, null, file, rescanExistingImports);
                                        Trace.WriteLine(currentFile.ActivePathAndName + " " + currentFile.CalculatedChecksum);
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

                            ArtContentManager.Static.Database.BeginTransaction(Database.TransactionType.Active);
                            ArtContentManager.Static.DatabaseAgents.dbaFile.UpdateAntiVerifiedFiles(currentDir, activeScan.StartScanTime);
                            ArtContentManager.Static.Database.CommitTransaction(Database.TransactionType.Active);

                            if (activeScan != rootScan)
                            {
                                activeScan.CompleteScanTime = DateTime.Now;
                                ArtContentManager.Static.DatabaseAgents.dbaScanHistory.RecordScanComplete(activeScan);
                            }
                            break;
                    }
                }

                // Push the subdirectories onto the stack for traversal. 
                // This could also be done before handing the files. 
                foreach (string str in subDirs)
                    dirs.Push(str);
            }

            switch (scanMode)
            {
                case ScanMode.smFullImportCount:
                    ArtContentManager.Static.DatabaseAgents.dbaScanHistory.UpdateInitialFileCounts(rootScan);
                    break;
                case ScanMode.smFullImport:
                    // Free up any static memory structures which are only needed by the scan
                    // This includes the dictionary of scans we persist between the count and import phases
                    Database.UnloadScanReferenceData();
                    _folderScan = null;
                    break;

            }
        }

        public static int InternalZipInstance
        {
            // This property is maintained at scan level because it needs to be global to a specific file identified at the top level.
            // It represents the count of zip files within zip files and must be unique for every zip file inside the root zip file.
            // Since zip files can potentially occur at different levels in a hierrachy, recusive logic which analyses then cannot
            // assign a unique number for them and as such they must refer to this static variable (and maintain it) which is only
            // reset to zero when analysing a new file at the top level.
            get
            {
                return _internalZipInstance;
            }
            set
            {
                _internalZipInstance = value;
            }
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

        private static Content.Installation Installation(string path)
        {
            // Returns an Installation object if we are an installation root.
            // Also returns the root object if we are within a root.
            
            // The object is automatically read from the database or written to it if new
            // and so carries an installation root ID.

            int dirStartPos;
            int dirFirstStartPos = Int32.MaxValue;
            bool dirFound = false;
            int nextDirectoryMarkerPosOrEnd;
            string firstInstallationDir = string.Empty;
            string highestRootPath;

            if (Database.Installations.ContainsKey(path))
            {
                // We are a known installation root so return the database object
                return Database.Installations[path];
            }

            // We are not known but we might be a newly identified installation root

            foreach (string indentifyingDirectoryName in Database.InstallationTypes.Keys)
            {
                dirStartPos = path.IndexOf(@"\" + indentifyingDirectoryName);

                if (dirStartPos > -1)
                {
                    // The +2 includes the initial directory symbol and one more to take us to the next directory symbol or past end of string
                    nextDirectoryMarkerPosOrEnd = dirStartPos + indentifyingDirectoryName.Length + 2;

                    if (nextDirectoryMarkerPosOrEnd > path.Length)
                    {
                        // We are the final directory and this is valid
                        dirFound = true;
                    }
                    else
                    {
                        if (path.Substring(nextDirectoryMarkerPosOrEnd) == @"\")
                        {
                            // The next character is a directory separator.
                            // This check elimiates directories that use our identifying key as a substring of their name.
                            dirFound = true;
                        }
                    }

                    if (dirFound)
                    {
                        // we've found a valid installation marker but is it the BEST one (highest level)
                        if (dirStartPos < dirFirstStartPos)
                        {
                            dirFirstStartPos = dirStartPos;
                            firstInstallationDir = indentifyingDirectoryName;
                        }
                        dirFound = false; // Reset the found indicator to false for the next iteration 
                    }
                }
            }

            if (firstInstallationDir == string.Empty)
            {
                // We are not an installation root and nor do we match valid identifying directories
                // at all at any higher level, so just return null
                return null;
            }

            // We are an installation but are we the root of one or just a pseudo root?
            // A pseudo root has an installation root vald name but is inside a higher true root.

            if ((dirFirstStartPos + firstInstallationDir.Length + 2) < path.Length)
            {
                // Pseudo root; we need to get to the true root
                highestRootPath = path.Substring(0, dirFirstStartPos + firstInstallationDir.Length + 1);
            }
            else
            {
                highestRootPath = path;
            }

            if (Database.Installations.ContainsKey(highestRootPath))
            {
                // We have already calculated the true root and it exists so return it
                return Database.Installations[highestRootPath];
            }

            // Newly discovered installation root so write a new database object
            // and add it to the dictionary of all root installations

            Content.Installation installation = new Content.Installation();
            installation.RootPath = highestRootPath;
            installation.Type = Database.InstallationTypes[firstInstallationDir];

            ArtContentManager.Static.DatabaseAgents.dbaInstallations.RecordInstallation(installation);
            Database.Installations.Add(installation.RootPath, installation);

            return installation;

        }

    }
}
