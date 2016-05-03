﻿using System;
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

        public enum ScanMode { smCount, smImport };
        public static ScanMode scanMode = ScanMode.smCount;

        public static void Scan(Actions.Scan rootScan, BackgroundWorker bw)
        {

            Actions.Scan activeScan;
            Actions.Scan subScan;
            DirectoryInfo dirInfo;
            Database.LoadScanReferenceData();

            string phase;
            ArtContentManager.DatabaseAgents.dbaFile dbaFile = null;
            ArtContentManager.DatabaseAgents.dbaScanHistory dbaScanHistory = null;
            dbaScanHistory = new ArtContentManager.DatabaseAgents.dbaScanHistory();

            Database.LoadScanReferenceData();

            switch (scanMode)
            {
                case ScanMode.smCount :
                    phase = "counting";
                    rootScan.TotalFiles = 0;
                    rootScan.NewFiles = 0;
                    rootScan.ProcessedFiles = 0;
                    break;
                case ScanMode.smImport :
                    phase = "importing";
                    ScanProgress.TotalFileCount = rootScan.TotalFiles;
                    ScanProgress.CurrentFileCount = 0;
                    dbaFile = new ArtContentManager.DatabaseAgents.dbaFile();
                    break;
                default:
                    phase = "unknown";
                    break;
            }

            ScanProgress.DirectioryName = "";
            ScanProgress.FileName = "";
            ScanProgress.Message = "Scan starting for " + rootScan.FolderName;
            bw.ReportProgress(ScanProgress.CompletionPct);

            // Data structure to hold names of subfolders to be 
            // examined for files.

            Stack<string> dirs = new Stack<string>(20);

            if (!System.IO.Directory.Exists(rootScan.FolderName))
            {
                throw new ArgumentException();
            }
            dirs.Push(rootScan.FolderName);

            while (dirs.Count > 0)
            {
                string currentDir = dirs.Pop();
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

                if (rootScan.FolderName == currentDir)
                {
                    activeScan = rootScan;
                    subScan = null;
                }
                else
                {
                    subScan = new Actions.Scan();
                    subScan.FolderName = currentDir;
                    subScan.StartScanTime = rootScan.StartScanTime; // Inherit this rather than using true time.
                    subScan.IsRequestRoot = false;
                    dbaScanHistory.SetLastCompletedScanTime(subScan);
                    activeScan = subScan;
                }

                newFiles = dirInfo.GetFiles().Where(p => p.CreationTime > activeScan.PreviousCompletedScanTime).ToArray();

                activeScan.TotalFiles += dirInfo.GetFiles().Length;
                activeScan.NewFiles += newFiles.Length;

                // Perform the required action on each file here. 
                // Modify this block to perform your required task.

                switch (scanMode)
                {
         
                    case ScanMode.smCount:
                        
                        // When we are not the root, roll subtotals into the root total and add the sub scan
                        // The root scan will be updated outside the loop at the end of the process
                        if (activeScan != rootScan)
                        {
                            rootScan.TotalFiles += subScan.TotalFiles;
                            rootScan.NewFiles += subScan.NewFiles;
                            dbaScanHistory.RecordStartScan(subScan);
                            ScanProgress.Message = "Directory " + currentDir + " [+" + subScan.NewFiles + " (" + subScan.TotalFiles + ")] -> " + rootScan.NewFiles + " (" + rootScan.TotalFiles + ")";
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

                    case ScanMode.smImport:

                        foreach (FileInfo file in newFiles)
                        {
                            activeScan.ProcessedFiles++;

                            if (activeScan != rootScan)
                            {
                                rootScan.ProcessedFiles++;
                            }

                            ScanProgress.CurrentFileCount = rootScan.ProcessedFiles;

                            if (bw.CancellationPending)
                            {
                                ScanProgress.Message = "Scan cancelled before file " + file.Name + " in the " + phase + " phase";
                                bw.ReportProgress(ScanProgress.CompletionPct);
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

                                    ArtContentManager.Content.File currentFile = new Content.File(file, file.FullName);
                                    Debug.Assert(dbaFile != null); // We should have a perisistent local instance initialised outside the loop which will be caching query definitions
                                    currentFile.Save(dbaFile, activeScan.StartScanTime);

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
                        dbaFile.UpdateAntiVerifiedFiles(currentDir, activeScan.StartScanTime);
                        dbaFile.CommitTransaction();

                        if (activeScan != rootScan)
                        {
                            activeScan.CompleteScanTime = DateTime.Now;
                            dbaScanHistory.RecordScanComplete(activeScan);
                        }

                        break;
                }

                // Push the subdirectories onto the stack for traversal. 
                // This could also be done before handing the files. 
                foreach (string str in subDirs)
                    dirs.Push(str);
            }

            Database.UnloadScanReferenceData(); // Just to free up some memory

            if (scanMode == ScanMode.smCount)
            {
                dbaScanHistory.UpdateInitialFileCounts(rootScan);
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
    }
}
