using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace RTextNppPlugin.RTextEditor.Utilities
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
         * @brief   Searches for the first rtext file based on a directory and an extension.
         *
         * @author  Stefanos Anastasiou
         * @date    15.12.2012
         *
         * @param   currentDir  The current dir.
         * @param   extension   The extension.
         *
         * @return  The found .rtext file fullpath.
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
    }
}
