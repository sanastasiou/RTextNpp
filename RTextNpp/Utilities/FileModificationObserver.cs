using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;

namespace RTextNppPlugin.Utilities
{
    class FileModificationObserver
    {
        #region [Data Members]

        private Dictionary<string, ModificationState> _fileList = new Dictionary<string, ModificationState>(100);
        static object _lock = new object();
        #endregion

        #region [Interface]
        enum ModificationState
        {
            Unknown,
            Modified,
            Saved
        };

        /**
         * Executes the file opened action.
         * Adds a file to the observeration list.
         * Initial file state is unknown. Scintilla does not report correct file status for files that have not been saved after Notepad++ has been shutdown.
         *
         * \param   filepath    The filepath.
         */
        public void OnFileOpened(string filepath)
        {
            ModificationState aFileState = FileUtilities.IsFileModified(filepath) ? ModificationState.Modified : ModificationState.Saved;
            if (FileUtilities.IsAutomateFile(filepath))
            {
                if (!_fileList.ContainsKey(filepath))
                {
                    _fileList.Add(filepath, aFileState);
                }
                else
                {
                    _fileList[filepath] = aFileState;
                }
            }
        }

        /**
         * Occurs when Scintilla notifies us that a file has been edited.
         * \note    This does not occur for a file that has been edited and reopened without being saved after Scintilla has been closed and reopened.
         *
         * \param   filepath    The filepath.
         */
        public void OnFilemodified(string filepath)
        {
            OnFileOpened(filepath);
        }

        /**
         * Occurs when Scintilla notifies us that a file edit has been undone.
         * \note    This does not occur for a file that has been edited and reopened without being saved after Scintilla has been closed and reopened.
         *
         * \param   filepath    The filepath.
         */
        public void OnFileUnmodified(string filepath)
        {
            OnFileOpened(filepath);
        }

        /**
         * Saves all files under a certain workspace.
         *
         * \param   workspace   The workspace.
         */
        public void SaveWorkspaceFiles(string workspace)
        {
            string aCurrentFile = FileUtilities.GetCurrentFilePath();
            List<string> aFileList = new List<string>(_fileList.Keys);
            foreach (var key in aFileList)
            {
                if (FileUtilities.FindWorkspaceRoot(key).Equals(workspace))
                {
                    if (_fileList[key] != ModificationState.Saved)
                    {
                        FileUtilities.SaveFile(key);
                        _fileList[key] = ModificationState.Saved;
                    }
                }
            }
            if (aCurrentFile != FileUtilities.GetCurrentFilePath())
            {
                FileUtilities.SwitchToFile(aCurrentFile);
            }
        }

        /**
         * Saves all not save automate files.
         */
        public void SaveAllFiles()
        {
            string aCurrentFile = FileUtilities.GetCurrentFilePath();
            List<string> aFileList = new List<string>(_fileList.Keys);
            foreach (var key in aFileList)
            {
                if (_fileList[key] != ModificationState.Saved)
                {
                    FileUtilities.SaveFile(key);
                    _fileList[key] = ModificationState.Saved;
                }
            }
            if (aCurrentFile != FileUtilities.GetCurrentFilePath())
            {
                FileUtilities.SwitchToFile(aCurrentFile);
            }
        }

        /**
         * Cleans Notepad++ backup because of a bug that currently exists. This interferes with correct handling of saved/unsaved files.
         * \note  http://sourceforge.net/p/notepad-plus/bugs/5155/
         */
        public void CleanBackup()
        {
            string aAppDataDir = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + Constants.NPP_BACKUP_DIR;
            try
            {
                if (Directory.Exists(aAppDataDir))
                {
                    //clean backup
                    List<string> aFileList = new List<string>(_fileList.Keys);
                    //get list of backup files
                    List<string> aBackUpFiles = new List<string>((System.IO.Directory.GetFiles(aAppDataDir, "*.*", SearchOption.TopDirectoryOnly)));
                    foreach (var file in aFileList)
                    {
                        string aCurrentFile = Path.GetFileName(file);
                        //if a file exist which starts with the filename remove it
                        foreach (var backupFile in aBackUpFiles.Where( i => Path.GetFileName(i).StartsWith(aCurrentFile)))
                        {
                            File.Delete(backupFile);
                        }
                    }
                }
            }
            catch(Exception ex)
            {
                Logging.Logger.Instance.Append("CleanBackup() exception : {0}", ex.Message);
            }
        }
        
        #endregion
    }
}
