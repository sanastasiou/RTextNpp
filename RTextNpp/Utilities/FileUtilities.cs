using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace RTextNppPlugin.Utilities
{    
    public static class FileUtilities
    {
        public static Regex FileExtensionRegex = new Regex(@"(?<=\*)\..*?(?=,|:)", RegexOptions.Compiled); //!< The file extensions regular expression

        /**
         * Searches for workspace root.
         *
         * \param   file    The file.
         *
         * \return  The found workspace root ( file path of .rtext file ).
         */
        public static string FindWorkspaceRoot(string file)
        {
            try
            {
                return FindWorkspaceRoot(Path.GetDirectoryName(file), Path.GetExtension(file));
            }
            catch (Exception ex)
            {
                Logging.Logger.Instance.Append(Logging.Logger.MessageType.Error, Constants.GENERAL_CHANNEL, String.Format("FileUtilities.findRTextFile(string file) - Exception {0}", ex.Message));
                return null;
            }
        }

        /**
         *
         * \brief   Searches for the first rtext file based on a directory and an extension.
         *
         *
         * \param   currentDir  The current dir.
         * \param   extension   The extension.
         *
         * \return  The found .rtext file fullpath.
         */
        private static string FindWorkspaceRoot(string currentDir, string extension)
        {
            if (String.IsNullOrEmpty(extension) || String.IsNullOrEmpty(currentDir))
            {
                return null;
            }
            try
            {
                // maybe there are more than one .rtext file with different names
                string[] rTextFiles = (System.IO.Directory.GetFiles(currentDir, "*.rtext", SearchOption.TopDirectoryOnly));
                //find .rtext file , ignore capitalization
                foreach (string aFile in rTextFiles)
                {
                    //the filename and extension has to be .rtext
                    if (Path.GetFileName(aFile).ToLower().Equals(".rtext"))
                    {
                        string[] aLines = File.ReadAllLines(aFile);
                        for (int i = 0; i < aLines.Count(); ++i)
                        {
                            //skip empty lines
                            if (String.IsNullOrEmpty(aLines[i])) continue;
                            //find endings
                            Match matchResults = FileExtensionRegex.Match(aLines[i]);
                            while (matchResults.Success)
                            {
                                if (matchResults.Value.Equals(extension))
                                {
                                    return aFile;
                                }
                                matchResults = matchResults.NextMatch();
                            }
                        }
                    }
                }
                //did not find .rtext to this directory - go to parent and search again
                if (Directory.GetParent(currentDir) != null)
                {
                    return FindWorkspaceRoot(Directory.GetParent(currentDir).FullName, extension);
                }
            }
            catch (Exception ex)
            {
                Logging.Logger.Instance.Append(Logging.Logger.MessageType.Error, Constants.GENERAL_CHANNEL, "FileUtilities.FindWorkspaceRoot({0}, {1} : Exception : {2}", currentDir, extension, ex.Message);
            }
            return null;
        }

        /**
         * Gets list of open files from npp instance.
         *
         * \param [in,out]  nppData Notepad++ instance data.
         *
         * \return  The list of open files, an empty list in case no file is open.
         */
        public static List<string> GetListOfOpenFiles(ref NppData nppData)
        {
            List<string> aFiles = new List<string>();

            int nbFile = (int)Win32.SendMessage(nppData._nppHandle, NppMsg.NPPM_GETNBOPENFILES, 0, 0);
            if (nbFile > 0)            
            {
                using (ClikeStringArray cStrArray = new ClikeStringArray(nbFile, Win32.MAX_PATH))
                {
                    if (Win32.SendMessage(nppData._nppHandle, NppMsg.NPPM_GETOPENFILENAMES, cStrArray.NativePointer, nbFile) != IntPtr.Zero)
                    {
                        aFiles = new List<string>(cStrArray.ManagedStringsUnicode);
                    }
                }
            }
            return aFiles;
        }
    }
}
