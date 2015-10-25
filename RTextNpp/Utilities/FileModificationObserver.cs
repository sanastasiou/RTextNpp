using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using RTextNppPlugin.Utilities.Settings;
namespace RTextNppPlugin.Utilities
{
    class FileModificationObserver
    {
        #region [Data Members]
        private Dictionary<string, ModificationState> _fileList = new Dictionary<string, ModificationState>(100);
        private static object _lock = new object();
        private readonly ISettings _settings = null;
        private readonly INpp _nppHelper = null;
        #endregion
        #region [Interface]
        internal FileModificationObserver(ISettings settings, INpp nppHelper)
        {
            _settings  = settings;
            _nppHelper = nppHelper;
        }
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
        internal void OnFileOpened(string filepath)
        {
            ModificationState aFileState = _nppHelper.IsFileModified(filepath) ? ModificationState.Modified : ModificationState.Saved;
            if (FileUtilities.IsRTextFile(filepath, _settings, _nppHelper))
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
        internal void OnFilemodified(string filepath)
        {
            _fileList[filepath] = ModificationState.Modified;
        }
        /**
         * Occurs when Scintilla notifies us that a file edit has been undone.
         * \note    This does not occur for a file that has been edited and reopened without being saved after Scintilla has been closed and reopened.
         *
         * \param   filepath    The filepath.
         */
        internal void OnFileUnmodified(string filepath)
        {
            _fileList[filepath] = ModificationState.Saved;
        }
        /**
         * Saves all files under a certain workspace.
         *
         * \param   workspace   The workspace.
         */
        internal void SaveWorkspaceFiles(string workspace)
        {
            string aCurrentFile = _nppHelper.GetCurrentFilePath();
            List<string> aFileList = new List<string>(_fileList.Keys);
            foreach (var key in aFileList)
            {
                if (FileUtilities.FindWorkspaceRoot(key).Equals(workspace))
                {
                    if (_fileList[key] != ModificationState.Saved)
                    {
                        _nppHelper.SaveFile(key);
                        _fileList[key] = ModificationState.Saved;
                    }
                }
            }
            if (aCurrentFile != _nppHelper.GetCurrentFilePath())
            {
                _nppHelper.SwitchToFile(aCurrentFile);
            }
        }
        /**
         * Cleans Notepad++ backup because of a bug that currently exists. This interferes with correct handling of saved/unsaved files.
         * \note  http://sourceforge.net/p/notepad-plus/bugs/5155/
         */
        internal void CleanBackup()
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