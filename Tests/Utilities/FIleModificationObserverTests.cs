using System;
using System.Reflection;
namespace Tests.Utilities
{
    using NUnit.Framework;
    using RTextNppPlugin.Utilities;
    using RTextNppPlugin.Utilities.Settings;
    using RTextNppPlugin;
    using Moq;
    using System.Threading;
    using System.IO;
    using System.Xml;
    using MoqExtensions;
    using RTextNppPlugin.Scintilla;
    [TestFixture]
    class FIleModificationObserverTests
    {
        #region [DataMembers]
        private Mock<INpp> _nppMock                = null;
        private Mock<ISettings> _settingsMock      = null;
        private XmlDocument _pluginXml             = null;
        private FileModificationObserver _observer = null;
        #endregion
        [SetUp]
        public void Init()
        {
            _nppMock = new Mock<INpp>();
            _settingsMock = new Mock<ISettings>();
            _pluginXml = new XmlDocument();
            _pluginXml.LoadXml(Properties.Resources.RTextNpp);
            _pluginXml.Save(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\" + Constants.EX_LEXER_CONFIG_FILENAME);
            var aFile = File.Create(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\" + ".rtext");
            aFile.Write(System.Text.Encoding.ASCII.GetBytes(Properties.Resources.WorkspaceRoot), 0, Properties.Resources.WorkspaceRoot.GetByteCount());
            aFile.Close();
        }
        [Test]
        public void InitializationTest()
        {
            string aFilePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\" + "a.atm";
            _observer = new FileModificationObserver(_settingsMock.Object, _nppMock.Object);
        }
        [Test]
        public void FileModifiedTest()
        {
            string aFilePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\" + "a.atm";
            string workspace = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\" + ".rtext";
            _observer = new FileModificationObserver(_settingsMock.Object, _nppMock.Object);
            _settingsMock.Setup<string>(x => x.Get(Settings.RTextNppSettings.ExcludeExtensions)).Returns("meta;");
            _nppMock.Setup<string>(x => x.GetConfigDir()).Returns(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            _nppMock.Setup<bool>(x => x.IsFileModified(aFilePath)).Returns(true);
            _observer.OnFileOpened(aFilePath);
            _observer.OnFilemodified(aFilePath);
            _observer.SaveWorkspaceFiles(workspace);
            _nppMock.Verify(m => m.SaveFile(aFilePath), Times.Once());
            _observer.SaveWorkspaceFiles(workspace);
            //still only a single call
            _nppMock.Verify(m => m.SaveFile(aFilePath), Times.Once());
        }
        [Test]
        public void FileUnmodifiedTest()
        {
            string aFilePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\" + "a.atm";
            string workspace = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\" + ".rtext";
            _observer = new FileModificationObserver(_settingsMock.Object, _nppMock.Object);
            _settingsMock.Setup<string>(x => x.Get(Settings.RTextNppSettings.ExcludeExtensions)).Returns("meta;");
            _nppMock.Setup<string>(x => x.GetConfigDir()).Returns(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            _nppMock.Setup<bool>(x => x.IsFileModified(aFilePath)).Returns(true);
            _observer.OnFileOpened(aFilePath);
            _observer.OnFilemodified(aFilePath);
            _observer.OnFileUnmodified(aFilePath);
            _observer.SaveWorkspaceFiles(workspace);
            _nppMock.Verify(m => m.SaveFile(aFilePath), Times.Never());
        }
        [Test]
        public void FileOpenedTest()
        {
            string aFilePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\" + "a.atm";
            string workspace = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\" + ".rtext";
            _observer = new FileModificationObserver(_settingsMock.Object, _nppMock.Object);
            _settingsMock.Setup<string>(x => x.Get(Settings.RTextNppSettings.ExcludeExtensions)).Returns("meta;");
            _nppMock.Setup<string>(x => x.GetConfigDir()).Returns(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            _nppMock.Setup<bool>(x => x.IsFileModified(aFilePath)).Returns(false);
            _observer.OnFileOpened(aFilePath);
            _observer.OnFileOpened(aFilePath);
            _observer.SaveWorkspaceFiles(workspace);
            _nppMock.Verify(m => m.SaveFile(aFilePath), Times.Never());
        }
        [Test]
        public void SwitchToCurrentFileTest()
        {
            string aFilePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\" + "a.atm";
            string aFilePathb = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\" + "b.atm";
            string workspace = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\" + ".rtext";
            _observer = new FileModificationObserver(_settingsMock.Object, _nppMock.Object);
            _settingsMock.Setup<string>(x => x.Get(Settings.RTextNppSettings.ExcludeExtensions)).Returns("meta;");
            _nppMock.Setup<string>(x => x.GetConfigDir()).Returns(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            _nppMock.Setup<bool>(x => x.IsFileModified(aFilePath)).Returns(true);
            _nppMock.Setup<bool>(x => x.IsFileModified(aFilePathb)).Returns(true);
            _observer.OnFileOpened(aFilePath);
            _observer.OnFileOpened(aFilePathb);
            _nppMock.Setup<string>(x => x.GetCurrentFilePath()).ReturnsInOrder(aFilePath, aFilePathb);
            _observer.SaveWorkspaceFiles(workspace);
            _nppMock.Verify(m => m.SaveFile(aFilePath), Times.Once());
            _nppMock.Verify(m => m.SaveFile(aFilePathb), Times.Once());
            _nppMock.Verify(m => m.SwitchToFile(aFilePath), Times.Once());
        }
        [Test]
        public void CleanBackUpTestDirNotExist()
        {
            _observer = new FileModificationObserver(_settingsMock.Object, _nppMock.Object);
            string defaultBackupPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + Constants.NPP_BACKUP_DIR;
            if (!Directory.Exists(defaultBackupPath))
            {
                _observer.CleanBackup();
            }
            else
            {
                Directory.Move(defaultBackupPath, defaultBackupPath + "copy");
                _observer.CleanBackup();
                Directory.Move(defaultBackupPath + "copy", defaultBackupPath);
            }
        }
        [Test]
        public void CleanBackUpTest()
        {
            string aFilePath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\" + "a.atm";
            string aFilePathb = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\" + "b.atm";
            string workspace = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\" + ".rtext";
            string defaultBackupPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + Constants.NPP_BACKUP_DIR;
            _observer = new FileModificationObserver(_settingsMock.Object, _nppMock.Object);
            _settingsMock.Setup<string>(x => x.Get(Settings.RTextNppSettings.ExcludeExtensions)).Returns("meta;");
            _nppMock.Setup<string>(x => x.GetConfigDir()).Returns(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            _nppMock.Setup<bool>(x => x.IsFileModified(aFilePath)).Returns(false);
            _nppMock.Setup<bool>(x => x.IsFileModified(aFilePathb)).Returns(false);
            _observer.OnFileOpened(aFilePath);
            _observer.OnFileOpened(aFilePathb);
            //create backup directory if it doesn't exist
            if (!Directory.Exists(defaultBackupPath))
            {
                Directory.CreateDirectory(defaultBackupPath);
            }
            string aDummyBackup = defaultBackupPath + "\\" + "a.atm.backup";
            var aBackup = File.Create(aDummyBackup);
            aBackup.Close();
            _observer.CleanBackup();
            Assert.IsFalse(File.Exists(aDummyBackup));
            aBackup = File.Create(aDummyBackup);
            _observer.CleanBackup();
            //handle is used by another process here, exception will be thrown upon attemp to delete the file
            Assert.IsTrue(File.Exists(aDummyBackup));
            aBackup.Close();
            File.Delete(aDummyBackup);
        }
    }
}
