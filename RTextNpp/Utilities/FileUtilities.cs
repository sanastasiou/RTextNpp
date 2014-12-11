using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace RTextNppPlugin.RTextEditor.Utilities
{
     /**
     * @class   ProcessInfo
     *
     * @brief   Information needed to start a backend process.
     *
     * @author  Stefanos Anastasiou
     * @date    17.11.2012
     */
    public class ProcessInfo
    {
        /**
         * @fn  ProcessInfo( string workingDir, string rtextPath, string cmdLine, string error,
         *      string key )
         *
         * @brief   Constructor.
         *
         * @author  Stefanos Anastasiou
         * @date    17.11.2012
         *
         * @param   workingDir  The working dir.
         * @param   rtextPath   Full pathname of the rtext file.
         * @param   cmdLine     The command line.
         * @param   error       The error.
         * @param   key         The process key.
         * @param   port        The port number associated with this process.
         */
        public ProcessInfo(string workingDir, string rtextPath, string cmdLine, string error, string key, int port = -1)
        {
            WorkingDirectory = workingDir;
            RTextFilePath = rtextPath;
            CommandLine = cmdLine;
            Error = error;
            ProcKey = key;
            Port = port;
            Guid = null;
            Name = null;
            if (ProcKey != null)
            {
                Guid = Utilities.HashUtilities.getGUIDfromString(ProcKey);
            }
        }

        /**
         * @property public Guid Guid
         *
         * @brief   Gets or sets an unique identifier.
         *
         * @return  The GUID of the process key.
         */
        public Guid? Guid { get; private set; }

        /**
         * @property    public string workingDirectory
         *
         * @brief   Gets or sets the pathname of the working directory.
         *
         * @return  The pathname of the working directory.
         */
        public string WorkingDirectory { get; private set; }

        /**
         * @property    public string rTextFilePath
         *
         * @brief   Gets or sets the full pathname of the text file.
         *
         * @return  The full pathname of the text file.
         */
        public string RTextFilePath { get; private set; }

        /**
         * @property    public string commandLine
         *
         * @brief   Gets or sets the command line.
         *
         * @return  The command line.
         */
        public string CommandLine { get; private set; }

        /**
         * @property    public string errorOut
         *
         * @brief   Gets or sets the error.
         *
         * @return  The error out.
         */
        public string Error { get; private set; }

        /**
         * @property    public string procKey
         *
         * @brief   Gets or sets a unique proc key based on the location of the rtxet file and the associated extensions
         *
         * @return  The proc key.
         */
        public string ProcKey { get; private set; }

        /**
         * @property    public int Port
         *
         * @brief   Gets or sets the port.
         *
         * @return  The port.
         */
        public int Port { get; set; }

        /**
         * @property    public string Name
         *
         * @brief   Gets or sets the name of the process.
         *
         * @return  The name.
         */
        public string Name { get; set; }

        /**
         * \property    public string Extension
         *
         * \brief   Gets or sets the extension for which a backend process was started.
         *
         * \return  The extension.
         */
        public string Extension { get; set; }
    };

    public static class FileUtilities
    {
        private static Regex FileExtensionRegex = new Regex(@"(?<=\*)\..*?(?=,|:)", RegexOptions.Compiled); //!< The file extensions regular expression

        /**
         * @fn  string getDir(string file)
         *
         * @brief   Gets a directory from a path.
         *
         * @author  Stefanos Anastasiou
         * @date    17.12.2012
         *
         * @param   file    The file.
         * @remarks Can throw exceptions if path is invalid.
         * @return  The directory.
         */
        public static string getDir(string file) { return Path.GetDirectoryName(file); }

        /**
         * @fn  string getExt(string file)
         *
         * @brief   Gets an extension from a path.
         *
         * @author  Stefanos Anastasiou
         * @date    17.12.2012
         *
         * @param   file    The file.
         * @remarks Throws exceptions if path is invalid.
         * 
         * @return  The extension.
         */
        public static string getExt(string file) { return Path.GetExtension(file); }

        public static bool findRTextFile(out ProcessInfo infoOut, string currentDir, string extension)
        {
            if (String.IsNullOrEmpty(extension) || String.IsNullOrEmpty(currentDir))
            {
                infoOut = new ProcessInfo(null, null, null, "Directory or extension cannot be an empty or null string!", null, -1);
                return false;
            }
            try
            {
                // maybe there are more than one .rtext file with different names
                string[] rTextFiles = (Directory.GetFiles(currentDir, "*.rtext", SearchOption.TopDirectoryOnly));
                //find .rtext file , ignore capitalization
                foreach (string aFile in rTextFiles)
                {
                    //the filename and extension has to be .rtext
                    if (System.IO.Path.GetFileName(aFile).ToLower().Equals(".rtext"))
                    {
                        //check if this .rtext file has the correct extension
                        try
                        {
                            string[] aLines = File.ReadAllLines(aFile);
                            if (aLines.Count() == 0)
                            {
                                infoOut = new ProcessInfo(null, null, null, "Invalid .rtext file configuration.", null);
                                return false;
                            }
                            for (int i = 0; i < aLines.Count(); ++i)
                            {
                                //skip empty lines
                                if (String.IsNullOrEmpty(aLines[i])) continue;
                                //find endings
                                Match matchResults = FileExtensionRegex.Match(aLines[i]);
                                bool aHasFoundMatch = false;
                                string aExtensions = "";
                                while (matchResults.Success)
                                {
                                    aExtensions += "+" + matchResults.Value;
                                    if (matchResults.Value.Equals(extension))
                                    {
                                        aHasFoundMatch = true;
                                    }
                                    matchResults = matchResults.NextMatch();
                                }
                                //ok found matching extension in .rtext file, check for commandline - next line should be the command line
                                if (aHasFoundMatch && (i + 1 < aLines.Count()) && !String.IsNullOrEmpty(aLines[i + 1]))
                                {
                                    //sanity check that next line exists and that it isn't empty
                                    infoOut = new ProcessInfo(System.IO.Path.GetDirectoryName(aFile), aFile, aLines[i + 1], null, aFile + aExtensions);
                                    return true;
                                }
                            }
                            infoOut = new ProcessInfo(null, null, null, "Invalid .rtext file configuration.", null);
                            return false;
                        }
                        catch (Exception ex)
                        {
                            Debug.Print(ex.Message);
                        }
                    }
                }
                //did not find .rtext to this directory - go to parent and search again
                if (System.IO.Directory.GetParent(currentDir) != null)
                {
                    return findRTextFile(out infoOut, System.IO.Directory.GetParent(currentDir).FullName, extension);
                }
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
                infoOut = new ProcessInfo(null, null, null, ex.Message, null);
                return false;
            }
            infoOut = new ProcessInfo(null, null, null, "Could not find any suitable .rtext file.", null);
            return false;
        }

        /**
         * @fn  public string findRTextFile(string currentDir, string extension)
         *
         * @brief   Searches for the first rtext file base on a directory and an extension.
         *
         * @author  Stefanos Anastasiou
         * @date    15.12.2012
         *
         * @param   currentDir  The current dir.
         * @param   extension   The extension.
         *
         * @return  The found rtext file fullpath.
         */
        public static string findRTextFile(string currentDir, string extension)
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
                    if (System.IO.Path.GetFileName(aFile).ToLower().Equals(".rtext"))
                    {
                        //check if this .rtext file has the correct extension
                        try
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
                        catch (Exception ex)
                        {
                            Debug.Print(ex.Message);
                        }
                    }
                }
                //did not find .rtext to this directory - go to parent and search again
                if (System.IO.Directory.GetParent(currentDir) != null)
                {
                    return findRTextFile(System.IO.Directory.GetParent(currentDir).FullName, extension);
                }
            }
            catch (Exception ex)
            {
                Debug.Print(ex.Message);
            }
            return null;
        }
    }
}
