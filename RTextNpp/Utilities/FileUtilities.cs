using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Xml.Linq;
using CSScriptIntellisense;
using System.Diagnostics;
using RTextNppPlugin.Utilities.Settings;
using AJ.Common;

namespace RTextNppPlugin.Utilities
{
    internal static class FileUtilities
    {
        internal static Regex FileExtensionRegex = new Regex(@"(?<=\*)\..*?(?=,|:)", RegexOptions.Compiled); //!< The file extensions regular expression

        /**
         * Searches for workspace root.
         *
         * \param   file    The file.
         *
         * \return  The found workspace root ( file path of .rtext file ).
         */
        internal static string FindWorkspaceRoot(string file)
        {
            try
            {
                return FindWorkspaceRoot(Path.GetDirectoryName(file), Path.GetExtension(file));
            }
            catch (Exception ex)
            {
                Logging.Logger.Instance.Append(Logging.Logger.MessageType.Error, Constants.GENERAL_CHANNEL, String.Format("FileUtilities.findRTextFile(string file) - Exception {0}", ex.Message));
                return String.Empty;
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
                return String.Empty;
            }

            // maybe there are more than one .rtext file with different names
            string[] rTextFiles = (System.IO.Directory.GetFiles(currentDir, "*" + Constants.WORKSPACE_TYPE, SearchOption.TopDirectoryOnly));
            //find .rtext file , ignore capitalization
            foreach (string aFile in rTextFiles)
            {
                //the filename and extension has to be .rtext
                if (Path.GetFileName(aFile).ToLower().Equals(Constants.WORKSPACE_TYPE))
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
            return String.Empty;
        }

        /**
         * \brief   Query if 'settings' is r text file.
         *
         * \param   settings    Provides access to persistent settings.
         *
         * \return  true if the current file is an rtext file, false if not.
         */
        internal static bool IsRTextFile(ISettings settings, INpp nppHelper)
        {
            return IsRTextFile(nppHelper.GetCurrentFilePath(), settings, nppHelper);
        }

        /**
         * \brief   Query if 'file' is an rtext file.
         *
         * \param   file        The file.
         * \param   settings    Options for controlling the operation.
         *
         * \return  true if file parameter is an rtext file, false if not.
         */
        internal static bool IsRTextFile(string file, ISettings settings, INpp nppHelper)
        {
            try
            {
                string fileExt = Path.GetExtension(file);
                if (fileExt.StartsWith("."))
                {
                    fileExt = fileExt.Remove(0, 1);
                }
                //list of excluded extensions
                List<string> aExlusionList = new List<string>(settings.Get(Settings.Settings.RTextNppSettings.ExcludeExtensions).Split(new char[] { ';' }, StringSplitOptions.RemoveEmptyEntries));

                //get npp configuration directory
                //get list of supported extensions
                string configDir = nppHelper.GetConfigDir();

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
                    foreach (var ext in additionalExt.SplitString(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        if (fileExt.Equals(ext, StringComparison.InvariantCultureIgnoreCase))
                        {
                            return !aExlusionList.Contains(fileExt);
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
    }
}
