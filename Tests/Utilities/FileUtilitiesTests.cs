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
    [TestFixture]
    class FileUtilitiesTests
    {
        #region [DataMembers]
        private Mock<INpp> _nppMock           = null;
        private Mock<ISettings> _settingsMock = null;
        private XmlDocument _pluginXml        = null;
        #endregion
        [SetUp]
        public void Init()
        {
            _nppMock      = new Mock<INpp>();
            _settingsMock = new Mock<ISettings>();
            _pluginXml    = new XmlDocument();
            _pluginXml.LoadXml(Properties.Resources.RTextNpp);
            _pluginXml.Save(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\" + Constants.EX_LEXER_CONFIG_FILENAME);
            var aFile = File.Create(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\" + ".rtext");
            aFile.Write(System.Text.Encoding.ASCII.GetBytes(Properties.Resources.WorkspaceRoot), 0, Properties.Resources.WorkspaceRoot.GetByteCount());
            aFile.Close();
        }
        [Test]
        public void IsRTextFileTest()
        {
            _settingsMock.Setup<string>(x => x.Get(Settings.RTextNppSettings.ExcludeExtensions)).Returns("meta;");
            _nppMock.Setup<string>(x => x.GetConfigDir()).Returns(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            Assert.IsTrue(FileUtilities.IsRTextFile("a.atm", _settingsMock.Object, _nppMock.Object));
            Assert.IsFalse(FileUtilities.IsRTextFile("a.xml", _settingsMock.Object, _nppMock.Object));
            Assert.IsFalse(FileUtilities.IsRTextFile("a.meta", _settingsMock.Object, _nppMock.Object));
            Assert.IsFalse(FileUtilities.IsRTextFile("a", _settingsMock.Object, _nppMock.Object));
            Assert.IsTrue(FileUtilities.IsRTextFile("a.atm40", _settingsMock.Object, _nppMock.Object));
            Assert.IsFalse(FileUtilities.IsRTextFile(null, _settingsMock.Object, _nppMock.Object));
        }
        [Test]
        public void IsCurrentRTextFileTest()
        {
            _settingsMock.Setup<string>(x => x.Get(Settings.RTextNppSettings.ExcludeExtensions)).Returns("meta;");
            _nppMock.Setup<string>(x => x.GetConfigDir()).Returns(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            _nppMock.Setup<string>(x => x.GetCurrentFilePath()).Returns("a.atm");
            Assert.IsTrue(FileUtilities.IsRTextFile(_settingsMock.Object, _nppMock.Object));
        }
        [Test]
        public void FindWorkspaceTest()
        {
            Assert.AreEqual(FileUtilities.FindWorkspaceRoot(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\" + "a.atm"), Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\" + ".rtext");
            Assert.AreEqual(FileUtilities.FindWorkspaceRoot(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\" + "a.atm40"), Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\" + ".rtext");
            Assert.AreEqual(FileUtilities.FindWorkspaceRoot(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\" + "a.atm50"), String.Empty);
            File.Delete(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\" + ".rtext");
            Assert.AreEqual(FileUtilities.FindWorkspaceRoot(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\" + "a.atm"), String.Empty);
        }
        [Test]
        public void FindWorkspaceTestInvalidArguments()
        {
            Assert.AreEqual(FileUtilities.FindWorkspaceRoot("a"), String.Empty);
            Assert.AreEqual(FileUtilities.FindWorkspaceRoot(""), String.Empty);
            Assert.AreEqual(FileUtilities.FindWorkspaceRoot("."), String.Empty);
        }
    }
}
