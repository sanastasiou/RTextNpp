using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using System.Xml.Linq;

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

        /**
         * Gets npp configuration directory.
         *
         * \return  The npp configuration directory.
         */
        public static string GetNppConfigDirectory()
        {
            StringBuilder sbIniFilePath = new StringBuilder(Win32.MAX_PATH);
            Win32.SendMessage(Plugin.nppData._nppHandle, NppMsg.NPPM_GETPLUGINSCONFIGDIR, Win32.MAX_PATH, sbIniFilePath);
            string configDir = sbIniFilePath.ToString();

            try
            {
                if (Directory.Exists(configDir))
                {
                    return configDir;
                }
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /**
         * Query if the current file is an automate file.
         *
         * \return  true if the current file is an automate file, false if not.
         */
        public static bool IsAutomateFile()
        {
            return IsAutomateFile(GetCurrentFilePath());
        }

        /**
         * \brief   Query if 'file' is an automate file.
         *
         * \param   file    The file.
         *
         * \return  true if file parameter is an automate file, false if not.      
         */
        public static bool IsAutomateFile(string file)
        {
            try
            {
                string fileExt = Path.GetExtension(file);
                if (fileExt.StartsWith("."))
                {
                    fileExt = fileExt.Remove(0, 1);
                }
                //list of excluded extensions
                List<string> aExlusionList = new List<string>(Settings.Instance.Get(Settings.RTextNppSettings.ExcludeExtensions).Split(';'));

                //get npp configuration directory
                //get list of supported extensions
                string configDir = GetNppConfigDirectory();
                if (!String.IsNullOrEmpty(configDir))
                {
                    //try to open external lexer configuration file
                    XDocument xmlDom = XDocument.Load(configDir + @"\" + Constants.EX_LEXER_CONFIG_FILENAME);
                    if (fileExt.Equals((xmlDom.Root.Element("Languages").Element("Language").Attribute("ext").Value), StringComparison.InvariantCultureIgnoreCase))
                    {
                        return !aExlusionList.Contains(fileExt);
                    }
                    //check user defined extensions as well
                    string additionalExt = xmlDom.Root.Element("LexerStyles").Element("LexerType").Attribute("ext").Value;
                    if (!String.IsNullOrWhiteSpace(additionalExt))
                    {
                        foreach (var ext in additionalExt.Split(' '))
                        {
                            if (fileExt.Equals(ext, StringComparison.InvariantCultureIgnoreCase))
                            {
                                return !aExlusionList.Contains(fileExt);
                            }
                        }
                    }

                }
                return false;
            }
            catch (Exception ex)
            {
                Logging.Logger.Instance.Append(Logging.Logger.MessageType.Error, Constants.GENERAL_CHANNEL, "FileUtilities.IsAutomateFile exception : {0}", ex.Message);
                return false;
            }

        }

        /**
         * Gets current file path.
         *
         * \return  The file path of the currently viewed document.
         */
        public static string GetCurrentFilePath()
        {
            NppMsg msg = NppMsg.NPPM_GETFULLCURRENTPATH;
            StringBuilder path = new StringBuilder(Win32.MAX_PATH);
            Win32.SendMessage(Plugin.nppData._nppHandle, msg, 0, path);
            return path.ToString();
        }

        /**
         * Query if 'file' is file modified.
         *
         * \param   file    The file.
         *
         * \return  true if file is considered to be modified, false if not.
         */
        public static bool IsFileModified(string file)
        {
            IntPtr sci = Plugin.GetCurrentScintilla();
            return ((int)Win32.SendMessage(sci, SciMsg.SCI_GETMODIFY, 0, 0) != 0);  
        }

        /**
         * Saves the currently viewed file.
         *
         * \param   file    The file.
         */
        public static void SaveFile(string file)
        {
            Win32.SendMessage(Plugin.nppData._nppHandle, NppMsg.NPPM_SWITCHTOFILE, 0, file);
            Win32.SendMessage(Plugin.nppData._nppHandle, NppMsg.NPPM_SAVECURRENTFILE, 0, 0);
        }

        /**
         * Switches active view to file.
         *
         * \param   file    The file.
         */
        public static void SwitchToFile(string file)
        {
            Win32.SendMessage(Plugin.nppData._nppHandle, NppMsg.NPPM_SWITCHTOFILE, 0, file);
        }
    }
}
