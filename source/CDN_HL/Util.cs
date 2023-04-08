using System;
using System.Collections;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace CDN_HL
{
    class Util
    {
        //full path of a file 
        //full path of a file 
        static string _strErrLogPath = System.Configuration.ConfigurationManager.AppSettings.Get("ErrLogPath");
        static string _strErrLogFile = System.Configuration.ConfigurationManager.AppSettings.Get("ErrLogFile");
        static string _strErrLogFilePath = "";

        public static string GetAppRelativePath(string strEndPath)
        {
            return Path.Combine(Environment.CurrentDirectory, strEndPath);
        }
        static public bool IsDirExist(string strDirPath)
        {
            if (!Directory.Exists(strDirPath))
                return false;

            return true;
        }

        /// <summary>
        /// if Directory (FolderPath) NOT EXIST, then create directory
        /// </summary>
        public static void DirCheck(string strDirPath)
        {
            if (!Directory.Exists(strDirPath))
                Directory.CreateDirectory(strDirPath);
        }

        public static string[] GetSubDir(string strDirPath)
        {
            if (Directory.Exists(strDirPath))
            {
                string[] aStrDefaultPath = { strDirPath };

                string[] aStrSubDirFound = Directory.GetDirectories(strDirPath);

                if (aStrSubDirFound.Length > 0)
                    return aStrSubDirFound;
                else
                    return aStrDefaultPath;
            }
            else
                return null;
        }
        public static bool IsFileNameExist(string strDirPath, string strFileName)
        {
            string strFileNamePath = System.IO.Path.Combine(strDirPath, strFileName);

            if (File.Exists(strFileNamePath))
                return true;

            return false;
        }

        /// <summary>
        /// if FileName NOT EXIST, then create filename
        /// </summary>
        /// <param name="strFileName"></param>
        /// <returns></returns>
        public static void FileNameCheck(string strDirPath, string strFileName)
        {
            string strFileNamePath = System.IO.Path.Combine(strDirPath, strFileName);

            if (!File.Exists(strFileNamePath))
                File.Create(strFileNamePath);
        }

        /// <summary>
        /// return ArrayList of all FileNames found in the directory
        /// </summary>
        /// <param name="strDirPath"></param>
        /// <param name="strFileName"></param>
        /// <returns></returns>
        public static ArrayList GetFileNameList(string strDirPath)
        {
            ArrayList aLstImgFileName = new ArrayList();

            DirCheck(strDirPath);

            //FileInfo[] files = dir.GetFiles().OrderBy(p=>p.CreationTime).ToArray();

            //string[] arrStrScanImgFileNameList = Directory.GetFiles(strDirPath).OrderBy(p=>p.).ToArray();

            string[] arrStrScanImgFileNameList = Directory.GetFiles(strDirPath).OrderBy(d => d).ToArray();

            if (arrStrScanImgFileNameList.Length > 0)
            {
                foreach (string strImgFileName in arrStrScanImgFileNameList)
                {
                    aLstImgFileName.Add(strImgFileName.Replace(strDirPath, ""));
                }
            }
            else
                aLstImgFileName.Add("File Not Found.");

            return aLstImgFileName;
        }

        /// <summary>
        /// return ArrayList of all FileNames found by (strImgFileNameSearch) in the directory
        /// </summary>
        /// <param name="strImgFilePath"></param>
        /// <param name="strImgFileNameSearch"></param>
        /// <returns></returns>
        public static ArrayList SearchFileName(string strImgFilePath, string strImgFileNameSearch)
        {
            ArrayList aLstImgFileName = new ArrayList();

            var strSearchList = strImgFileNameSearch.Split('|');

            if (IsDirExist(strImgFilePath))
            {
                foreach (var strToSearch in strSearchList)
                {
                    string[] arrStrScanImgFileNameList = Directory.GetFiles(strImgFilePath, strToSearch);
                    if (arrStrScanImgFileNameList.Length > 0)
                    {
                        foreach (string strImgFileName in arrStrScanImgFileNameList)
                            aLstImgFileName.Add(strImgFileName.TrimStart('\\').Replace(strImgFilePath, ""));
                    }
                    else
                        aLstImgFileName.Add("File Not Found.");
                }
            }
            else
                aLstImgFileName.Add("File Path Not Found.");

            return aLstImgFileName;
        }

        /// <summary>
        /// Remove these char. from a Filename: [(),+] and also remove "Loc ..." 
        /// </summary>
        /// <param name="strOrigFilename"></param>
        /// <returns></returns>
        public static string RenameFile(string strOrigFilename)
        {
            string strNewFilename = "";

            try
            {
                string strLoc = "+";

                int ixBeginLoc = GetLocationIndex(strOrigFilename);
                int ixEndLoc = strOrigFilename.LastIndexOf(".");

                if (ixBeginLoc > 0)
                    strLoc = strOrigFilename.Substring(ixBeginLoc, (ixEndLoc - ixBeginLoc));

                strNewFilename = strOrigFilename.Replace(strLoc, "").Replace("(", "").Replace(")", "").Replace(",", "").Replace("+", "").Replace("(", "").Replace(")", "");
            }
            catch (Exception ex)
            {
                string strErr = ex.Message + " | " + ex.InnerException.ToString();
                ErrLog(strErr);
            }

            return strNewFilename;  // + "\r\n";
        }

        /// <summary>
        /// This function is for cleaning the image file name - CDP
        /// CDN no need to going thru this process
        /// </summary>
        /// <param name="strImgFileName"></param>
        /// <returns></returns>
        public static int GetLocationIndex(string strImgFileName)
        {
            int ixLoc = strImgFileName.LastIndexOf(",Loc ");

            if (ixLoc == -1)
            {
                ixLoc = strImgFileName.LastIndexOf(", Loc ");

                if (ixLoc == -1)
                {
                    ixLoc = strImgFileName.LastIndexOf(", Loc ");

                    if (ixLoc == -1)
                    {
                        ixLoc = strImgFileName.LastIndexOf("] Loc ");
                    }
                }
            }

            return ixLoc;
        }
        public static void MoveFile(string strSourcePath, string strDestPath, string strFileName)
        {
            DirCheck(strDestPath);

            string strSourceFileName = System.IO.Path.Combine(strSourcePath, strFileName);
            string strDestFileName = System.IO.Path.Combine(strDestPath, strFileName);

            if (IsDirExist(strSourcePath))
            {
                if (IsFileNameExist(strSourcePath, strFileName))
                {
                    try
                    {
                        if (!System.IO.File.Exists(strDestFileName))
                            File.Move(strSourceFileName, strDestFileName);
                        else
                        {
                            //the FileName existed in the Destination folder path, [then we don't need to move the file]
                            //Just delete the Source File so it won't be in the img folder
                            File.Delete(strSourceFileName);
                        }
                    }
                    catch (Exception ex)
                    {
                        string strErrMsg = ex.ToString();
                        ErrLog(strErrMsg);
                        throw new Exception(strErrMsg);
                    }
                }
                else
                {
                    string strErrMsg = string.Format("Move File Error - Source FileName Not Found:\r\n{0}", strSourceFileName);
                    ErrLog(strErrMsg);
                    throw new Exception(strErrMsg);
                }
            }
            else
            {
                string strErrMsg = string.Format("Move File Error - Source Path Not Found:\r\n{0}", strSourcePath);
                ErrLog(strErrMsg);
                throw new Exception(strErrMsg);
            }

        }
        /// <summary>
        /// Copies an existing file to a new file. Overwriting a file of the same name is allowed.
        /// </summary>
        /// <param name="strSourcePath"></param>
        /// <param name="strDestPath"></param>
        /// <param name="strSrcFileName"></param>
        /// <param name="strDestFileName"></param>
        public static void CopyAndRenameFile(string strSourcePath, string strDestPath, string strSrcFileName, string strDestFileName)
        {
            DirCheck(strDestPath);

            string strSourceFileName = System.IO.Path.Combine(strSourcePath, strSrcFileName);
            string strDestinFileName = System.IO.Path.Combine(strDestPath, strDestFileName);

            if (IsDirExist(strSourcePath))
            {
                if (IsFileNameExist(strSourcePath, strSrcFileName))
                {
                    try
                    {
                        File.Copy(strSourceFileName, strDestinFileName, true);
                    }
                    catch (Exception ex)
                    {
                        string strErrMsg = ex.ToString();
                        ErrLog(strErrMsg);
                        throw new Exception(strErrMsg);
                    }
                }
                else
                {
                    string strErrMsg = string.Format("Move File Error - Source FileName Not Found:\r\n{0}", strSourceFileName);
                    ErrLog(strErrMsg);
                    throw new Exception(strErrMsg);
                }
            }
            else
            {
                string strErrMsg = string.Format("Move File Error - Source Path Not Found:\r\n{0}", strSourcePath);
                ErrLog(strErrMsg);
                throw new Exception(strErrMsg);
            }

        }
        public static void FileDelete(string strSourcePath, string strSrcFileName)
        {
            DirCheck(strSourcePath);

            string strSourceFileName = System.IO.Path.Combine(strSourcePath, strSrcFileName);

            if (IsDirExist(strSourcePath))
            {
                if (IsFileNameExist(strSourcePath, strSrcFileName))
                {
                    try
                    {
                        File.Delete(strSourceFileName);
                    }
                    catch (Exception ex)
                    {
                        string strErrMsg = ex.ToString();
                        ErrLog(strErrMsg);
                        throw new Exception(strErrMsg);
                    }
                }
                else
                {
                    string strErrMsg = string.Format("File Delete Error - Source FileName Not Found:\r\n{0}", strSourceFileName);
                    ErrLog(strErrMsg);
                    throw new Exception(strErrMsg);
                }
            }
            else
            {
                string strErrMsg = string.Format("File Delete Error - Source Path Not Found:\r\n{0}", strSourcePath);
                ErrLog(strErrMsg);
                throw new Exception(strErrMsg);
            }

        }
        /// <summary>
        /// Save the existing file as the new filename with option of overwriting the file of the same name is allowed.
        /// then delete the SourceFileName
        /// </summary>
        /// <param name="strSourcePath"></param>
        /// <param name="strDestPath"></param>
        /// <param name="strSrcFileName"></param>
        /// <param name="strDestFileName"></param>
        public static void FileSaveAs(string strSourcePath, string strDestPath, string strSrcFileName, string strDestFileName)
        {
            DirCheck(strDestPath);

            string strSourceFileName = System.IO.Path.Combine(strSourcePath, strSrcFileName);
            string strDestinFileName = System.IO.Path.Combine(strDestPath, strDestFileName);

            if (IsDirExist(strSourcePath))
            {
                if (IsFileNameExist(strSourcePath, strSrcFileName))
                {
                    try
                    {
                        File.Copy(strSourceFileName, strDestinFileName, true);
                        File.Delete(strSourceFileName);
                    }
                    catch (Exception ex)
                    {
                        string strErrMsg = ex.ToString();
                        ErrLog(strErrMsg);
                        throw new Exception(strErrMsg);
                    }
                }
                else
                {
                    string strErrMsg = string.Format("File SavAs Error - Source FileName Not Found:\r\n{0}", strSourceFileName);
                    ErrLog(strErrMsg);
                    throw new Exception(strErrMsg);
                }
            }
            else
            {
                string strErrMsg = string.Format("File SavAs Error - Source Path Not Found:\r\n{0}", strSourcePath);
                ErrLog(strErrMsg);
                throw new Exception(strErrMsg);
            }

        }

        public static void FileSaveAsAndMove(string strSourcePath, string strDestPath, string strSrcFileName, string strDestFileName)
        {
            DirCheck(strDestPath);

            string strSourceFileName = System.IO.Path.Combine(strSourcePath, strSrcFileName);
            string strDestinFileNameMove = System.IO.Path.Combine(strDestPath, strDestFileName);

            if (IsDirExist(strSourcePath))
            {
                if (IsFileNameExist(strSourcePath, strSrcFileName))
                {
                    try
                    {
                        if (!System.IO.File.Exists(strDestFileName))
                            
                            File.Move(strSourceFileName, strDestinFileNameMove);    //file move and save as
                        else
                        {
                            string strErrMsg = string.Format("Move File Error - Destination FileName Found:\r\n{0}", strDestinFileNameMove);
                            ErrLog(strErrMsg);
                            throw new Exception(strErrMsg);
                        }
                    }
                    catch (Exception ex)
                    {
                        string strErrMsg = ex.ToString() + "\r\n\r\nSource FileName: " + strSrcFileName + "\r\n\r\nDestination FileName: " + strDestFileName;
                        ErrLog(strErrMsg);
                        throw new Exception(strErrMsg);
                    }
                }
                else
                {
                    string strErrMsg = string.Format("Move File Error - Source FileName Not Found:\r\n{0}", strSrcFileName);
                    ErrLog(strErrMsg);
                }
            }
            else
            {
                string strErrMsg = string.Format("Move File Error - Source Path Not Found:\r\n{0}", strSourcePath);
                ErrLog(strErrMsg);
            }

        }

        public static string SubFolerFileCopyToNewLoc(string strSourcePath, string strDestPath)
        {
            string strErrMsg = "Done.";

            DirCheck(strDestPath);

            string[] aStrFolderScr = GetSubDir(strSourcePath);

            if (aStrFolderScr != null)
            {
                foreach (string strScrFolderPath in aStrFolderScr)
                {
                    ArrayList aLstScrFilename = GetFileNameList(strScrFolderPath);

                    if (aLstScrFilename.Count > 0)
                    {
                        foreach (string strSrcFileName in aLstScrFilename)
                        {
                            string strSourcePathFileName = System.IO.Path.Combine(strScrFolderPath, strSrcFileName.TrimStart('\\'));
                            string strDestinPathFileName = System.IO.Path.Combine(strDestPath, strSrcFileName.TrimStart('\\'));

                            if (IsFileNameExist(strScrFolderPath, strSrcFileName.TrimStart('\\')))
                                File.Copy(strSourcePathFileName, strDestinPathFileName, true);
                            else
                            {
                                strErrMsg = string.Format("SubFolerFileCopyToNewLoc() Error - Source FileName Not Found:\r\n{0}", strSourcePathFileName);
                                ErrLog(strErrMsg);
                                throw new Exception(strErrMsg);
                            }
                        }
                    }
                    else
                    {
                        strErrMsg = string.Format("SubFolerFileCopyToNewLoc() Error - Sub File Not Found:\r\n{0}", strScrFolderPath);
                        ErrLog(strErrMsg);
                        throw new Exception(strErrMsg);
                    }
                }
            }
            else
            {
                strErrMsg = string.Format("SubFolerFileCopyToNewLoc() Error - Sub Folder Not Found: {0}", strSourcePath);
                ErrLog(strErrMsg);
                throw new Exception(strErrMsg);
            }

            return strErrMsg;
        }

        public static ArrayList RemoveDS_StoreFile(string strScrFolderPath, ArrayList aLstrFilename)
        {
            string strDeleteFileName = "";
            int ix = 0;

            foreach (string strCurFileName in aLstrFilename)
            {
                if (strCurFileName.TrimStart('\\') == ".DS_Store")
                {
                    strDeleteFileName = System.IO.Path.Combine(strScrFolderPath, strCurFileName.TrimStart('\\'));

                    File.Delete(@strDeleteFileName);

                    aLstrFilename.RemoveAt(ix);
                    break;
                }
            }

            return aLstrFilename;
        }
        public static string RemoveDupFiles(string strSourcePath)
        {
            string strErrMsg = "Done Remove Duplicate.";

            string[] aStrFolderScr = GetSubDir(strSourcePath);

            if (aStrFolderScr != null)
            {
                string strFistFileNameInFolerPath = ""; //C:\DP_Project\DP_HL_Wall_Layout\Tram
                string strNextFileNameInFolerPath = "";
                string strDeleteFileName = "";
                string strFirstExt = "";

                //string strExt = "";

                int ixFileCount = 0;

                foreach (string strScrFolderPath in aStrFolderScr)
                {
                    ixFileCount = 0;
                    ArrayList aLstScrFilename = GetFileNameList(strScrFolderPath);

                    if (aLstScrFilename.Count > 0)
                    {
                        aLstScrFilename = RemoveDS_StoreFile(strScrFolderPath, aLstScrFilename);

                        string strFirstFileName = aLstScrFilename[ixFileCount].ToString().TrimStart('\\');

                        try
                        {
                            //if (strFirstFileName == ".DS_Store")
                            //    strFistFileNameInFolerPath = strFirstFileName;
                            //{}

                            strFirstExt = strFirstFileName.Substring(strFirstFileName.LastIndexOf("."));
                            strFistFileNameInFolerPath = strFirstFileName.Substring(0, strFirstFileName.LastIndexOf("."));

                            foreach (string strCurFileNameInList in aLstScrFilename)
                            {
                                if (ixFileCount > 0)
                                {
                                    string strNextExt = strCurFileNameInList.Substring(strCurFileNameInList.LastIndexOf("."));
                                    string strNextFileName = strCurFileNameInList.TrimStart('\\');

                                    strNextFileNameInFolerPath = strNextFileName.Substring(0, strNextFileName.LastIndexOf("."));

                                    //if (strFistFileNameInFolerPath == ".DS_Store")
                                    //{
                                    //    strDeleteFileName = System.IO.Path.Combine(strScrFolderPath, strFistFileNameInFolerPath);

                                    //    File.Delete(@strDeleteFileName);

                                    //    strFistFileNameInFolerPath = strNextFileNameInFolerPath;
                                    //    strFirstExt = strNextExt;
                                    //}
                                    //else if (strNextFileNameInFolerPath == ".DS_Store")
                                    //{
                                    //    strDeleteFileName = System.IO.Path.Combine(strScrFolderPath, strNextFileNameInFolerPath);

                                    //    File.Delete(@strDeleteFileName);
                                    //}else 

                                    if (strFistFileNameInFolerPath.Contains(strNextFileNameInFolerPath))
                                    {
                                        strDeleteFileName = System.IO.Path.Combine(strScrFolderPath, strFistFileNameInFolerPath + strFirstExt);

                                        if (File.Exists(@strDeleteFileName))
                                        {
                                            File.Delete(@strDeleteFileName);
                                        }
                                        //System.IO.Directory.Delete(strDeleteFileName);

                                        //File.Delete(strDeleteFileName);
                                    }
                                    else if (strNextFileNameInFolerPath.Contains(strFistFileNameInFolerPath))
                                    {
                                        strDeleteFileName = System.IO.Path.Combine(strScrFolderPath, strNextFileNameInFolerPath + strNextExt);

                                        File.Delete(strDeleteFileName);

                                        strFistFileNameInFolerPath = strNextFileNameInFolerPath;
                                        strFirstExt = strNextExt;
                                    }
                                    else
                                    {
                                        strFistFileNameInFolerPath = strNextFileNameInFolerPath;
                                        strFirstExt = strNextExt;
                                    }
                                }
                                ixFileCount++;
                            }
                        }
                        catch (Exception ex)
                        {
                            string strExcpt = ex.ToString();
                            ErrLog(strExcpt);
                        }
                    }
                    else
                    {
                        strErrMsg = string.Format("SubFolerFileCopyToNewLoc() Error - Sub File Not Found: {0}", strScrFolderPath);
                        ErrLog(strErrMsg);
                    }
                }
            }
            else
            {
                strErrMsg = string.Format("SubFolerFileCopyToNewLoc() Error - Sub Folder Not Found: {0}", strSourcePath);
                ErrLog(strErrMsg);
            }

            return strErrMsg;
        }

        static public void WriteToFile(string strFilePath, string strFileName, string strMessage)
        {
            DirCheck(strFilePath);
            FileNameCheck(strFilePath, strFileName);
            string strWriteFilePath = System.IO.Path.Combine(strFilePath, strFileName);

            using (FileStream fs = new FileStream(strWriteFilePath, FileMode.Append, FileAccess.Write))
            {
                StreamWriter sw = new StreamWriter(fs);
                sw.Write(DateTime.Now + " " + strMessage);
                sw.Flush();
                fs.Close();
            }
        }

        private static void WriteErrMsg(string strErrMsg)
        {
            using (FileStream fs = new FileStream(_strErrLogFilePath, FileMode.Append, FileAccess.Write))
            {
                StreamWriter sw = new StreamWriter(fs);
                sw.Write(DateTime.Now + " " + strErrMsg);
                sw.Flush();
                fs.Close();
            }

            //////Read and Write at the same time
            ////https://stackoverflow.com/questions/33633344/read-and-write-to-a-file-in-the-same-stream
            ////string filePath = "test.txt";
            ////FileStream fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            ////StreamReader sr = new StreamReader(fs);
            ////StreamWriter sw = new StreamWriter(fs);
            ////newString = sr.ReadToEnd() + "somethingNew";
            ////sw.Write(newString);
            ////sw.Flush(); //HERE
            ////fs.Close();
        }
        public static void ErrLog(string strErrMsg)
        {
            DirCheck(_strErrLogPath);
            FileNameCheck(_strErrLogPath, _strErrLogFile);
            _strErrLogFilePath = System.IO.Path.Combine(_strErrLogPath, _strErrLogFile);

            WriteErrMsg(strErrMsg);
        }
        /// <summary>
        ///  Jan, Feb, Mar, Apr, May, Jun, Jul, Aug, Sep, Oct, Nov, Dec
        /// </summary>
        /// <param name="strDate"></param>
        /// <param name="refStringUSDate"></param>
        /// <returns></returns>
        public static bool IsUSMonthFound(string strDate, ref string refStringUSDate)
        {
            bool bValid = false;

            if (strDate.ToLower().IndexOf("jan") >= 0)
                bValid = true;
            else if (strDate.ToLower().IndexOf("feb") >= 0)
                bValid = true;
            else if (strDate.ToLower().IndexOf("mar") >= 0)
                bValid = true;
            else if (strDate.ToLower().IndexOf("apr") >= 0)
                bValid = true;
            else if (strDate.ToLower().IndexOf("may") >= 0)
                bValid = true;
            else if (strDate.ToLower().IndexOf("jun") >= 0)
                bValid = true;
            else if (strDate.ToLower().IndexOf("jul") >= 0)
                bValid = true;
            else if (strDate.ToLower().IndexOf("aug") >= 0)
                bValid = true;
            else if (strDate.ToLower().IndexOf("sep") >= 0)
                bValid = true;
            else if (strDate.ToLower().IndexOf("oct") >= 0)
                bValid = true;
            else if (strDate.ToLower().IndexOf("nov") >= 0)
                bValid = true;
            else if (strDate.ToLower().IndexOf("dec") >= 0)
                bValid = true;

            if (bValid)
            {
                try
                {
                    DateTime dt = DateTime.Parse(strDate);
                    refStringUSDate = dt.ToString("MM/dd/yyyy");
                }
                catch (Exception e)
                {
                    bValid = false;
                    string strExcpt = e.ToString();
                    ErrLog(strExcpt);
                }
            }

            return bValid;
        }
        public static bool IsValidDate(string strDate)
        {
            bool bValid = true;

            try
            {
                DateTime dt = DateTime.Parse(strDate);
            }
            catch (Exception e)
            {
                bValid = false;
                string strExcpt = e.ToString();
                ErrLog(strExcpt);
            }
            return bValid;
        }
        /// <summary>
        /// strVNDate: DD-MM-YY transform to: MM/DD/YYYY or YYYY
        /// </summary>
        /// <param name="strVNDate"></param>
        /// <param name="refStringUSDate"></param>
        /// <returns></returns>
        public static bool IsValidDateFmt(string strVNDate, ref string refStringUSDate)
        {
            bool bValid = false;
            string strDD = "";
            string strMM = "";
            string strYY = "";
            string strNewDate = "";

            strVNDate = strVNDate.Replace(".", "/").Replace("-", "/");

            //strVNDate: DD-MM-YY
            //"26-5-2006 ÂL"
            //"22-10-ÂL"
            //"7-4-22"
            //"7-4-2022"
            string[] arrStrTempDate = strVNDate.Split('/');

            if (arrStrTempDate.Length == 3)
            {
                //strVNDate: DD/MM/YY  >> 30/04/75  or  DD/MM/YYYY  >> 30/04/1975
                strDD = arrStrTempDate[0];
                strMM = arrStrTempDate[1];
                strYY = arrStrTempDate[2];

                //US Date: MM/DD/YYYY  >> 04/30/75
                strNewDate = string.Format("{0}/{1}/{2}", strMM, strDD, strYY);

                if (IsValidDate(strNewDate))
                {
                    bValid = true;
                    refStringUSDate = strNewDate;
                }
            }
            else if (arrStrTempDate.Length == 1)
            {
                //strVNDate: YYYY
                try
                {
                    int i = Convert.ToInt32(arrStrTempDate[0]);
                    bValid = true;
                    refStringUSDate = arrStrTempDate[0];
                }
                catch (Exception e)
                {
                    string strExcpt = e.ToString();
                    ErrLog(strExcpt);
                }
            }

            return bValid;
        }
        public static bool GetUSDateFmt(string strVNDate, ref string refStringUSDate)
        {
            bool bValue = false;

            //strVNDate: DD-MM-YY
            //"22-10-ÂL"
            //"7-4-22"
            //"7-4-2022"
            if (!IsValidDateFmt(strVNDate, ref refStringUSDate))
            {
                //strVNDate = strVNDate.Replace(".", "/").Replace("-", "/");

                ////"26-5-2006 ÂL"
                //string[] arrStrTempDate = strVNDate.Split(' ');

                //foreach(string strTempDate in arrStrTempDate)
                //{
                //    if (IsValidDateFmt(strVNDate, ref refStringUSDate))
                //    {

                //    }
                //    else
                //    {

                //        //strVNDate: DD-MM-YY  >> 30/04/75  or  DD/MM/YYYY  >> 30/04/1975
                //        strDD = arrStrTempDate[0];
                //        strMM = arrStrTempDate[1];
                //        strYY = arrStrTempDate[2];

                //        //return: MM.DD.YY  >> 04.30.75
                //        refStringUSDate = string.Format("{0}/{1}/{2}", strMM, strDD, strYY);
                //    }
                //}
            }

            return bValue;
        }

        #region Resize and convert images

        /// <summary>
        /// Resize an image based on the provided image size 
        /// and save the image as the provided destFileName
        /// </summary>
        /// <param name="sourceFileName"></param>
        /// <param name="destFileName"></param>
        /// <param name="imageSizeLength"></param>
        /// <returns>True if succedded; Otherwise, False.</returns>
        public static bool ResizeImageAndSave(string sourceFileName, string destFileName, int imageSizeLength = 400)
        {
            // File already existed; No modification or nothing to do
            if (File.Exists(destFileName))
                return false;

            var resizedImg = ResizeImage(new Bitmap(Image.FromFile(sourceFileName)), new Size(imageSizeLength, imageSizeLength));
            if (resizedImg == null)
                return false;

            var result = ConvertBitmapImageToJPGAndSave(resizedImg, destFileName);
            resizedImg.Dispose();
            return result;
        }

        /// <summary>
        /// Resize an image based on the provide size
        /// </summary>
        /// <param name="imgToResize">An image to resize</param>
        /// <param name="size">The size of the image</param>
        /// <returns>A bitmap of a new resized image</returns>
        private static Bitmap ResizeImage(Image imgToResize, Size size)
        {
            int sourceWidth = imgToResize.Width;
            int sourceHeight = imgToResize.Height;
            float nPercentW = size.Width / (float)sourceWidth;
            float nPercentH = size.Height / (float)sourceHeight;
            float nPercent;
            if (nPercentH < nPercentW)
                nPercent = nPercentH;
            else
                nPercent = nPercentW;

            int destWidth = (int)(sourceWidth * nPercent);
            int destHeight = (int)(sourceHeight * nPercent);
            Bitmap imgBitmap = new Bitmap(destWidth, destHeight);
            Graphics graphics = Graphics.FromImage(imgBitmap);
            graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
            graphics.DrawImage(imgToResize, 0, 0, destWidth, destHeight);
            graphics.Dispose();
            return imgBitmap;
        }

        /// <summary>
        /// Convert an bitmap image and save as .JPG using the provided new ImageFileName
        /// </summary>
        /// <param name="imgBitmap"The image bitmap></param>
        /// <param name="newImageFileName">The new image file name</param>
        /// <returns>True if succeeded; Otherwise, False.</returns>
        private static bool ConvertBitmapImageToJPGAndSave(Bitmap imgBitmap, string newImageFileName)
        {
            ImageCodecInfo jpgEncoder = GetEncoder(ImageFormat.Jpeg);
            var myEncoder = Encoder.Quality;
            var myEncoderParameters = new EncoderParameters(1);

            // Save the bitmap as a JPG file with 100% quality level compression.
            var myEncoderParameter = new EncoderParameter(myEncoder, 100L);
            myEncoderParameters.Param[0] = myEncoderParameter;

            if (File.Exists(newImageFileName))
                return false; // File already existed; There will be no image coversion.

            imgBitmap.Save(newImageFileName, jpgEncoder,
                myEncoderParameters);
            imgBitmap.Dispose();
            return true;
        }

        /// <summary>
        /// Get an image encoder based on the provided image format
        /// </summary>
        /// <param name="format">The image format</param>
        /// <returns>The image encoder info</returns>
        private static ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
                if (codec.FormatID == format.Guid)
                    return codec;
            return null;
        }

        public static void LogAMessage(string fileName, string message)
        {
            using (StreamWriter sw = File.AppendText(fileName))
                sw.WriteLine($"{DateTime.Now}: {message}");
        }
        #endregion new methods
    }
}
