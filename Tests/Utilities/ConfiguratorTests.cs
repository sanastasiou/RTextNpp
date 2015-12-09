using System;
using System.Reflection;
namespace Tests.Utilities
{
    using NUnit.Framework;
    using RTextNppPlugin.Utilities;
    using RTextNppPlugin.Utilities.Settings;
    using Moq;
    using System.Threading;
    using System.IO;
    using System.Xml;
    using RTextNppPlugin.Scintilla;
    [TestFixture]
    class ConfiguratorTests
    {
        [SetUp]
        public void Init()
        {
            string configFile = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\" + "RTextNpp.dll.config";
            if (File.Exists(configFile))
            {
                File.Delete(configFile);
            }
        }
        [Test]
        public void InitializationTest()
        {
            var nppMock = new Mock<INpp>();
            nppMock.Setup<string>(x => x.GetConfigDir()).Returns(Assembly.GetExecutingAssembly().Location);
            ConfigurationSetter s = new ConfigurationSetter(nppMock.Object);
        }
        [Test]
        public void ReadWriteTestValid()
        {
            var nppMock = new Mock<INpp>();
            nppMock.Setup<string>(x => x.GetConfigDir()).Returns(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            ConfigurationSetter s = new ConfigurationSetter(nppMock.Object);
            s.saveSetting<bool>(false, Settings.RTextNppSettings.AutoSaveFiles);
            bool value = true;
            s.readSetting<bool>(ref value, Settings.RTextNppSettings.AutoSaveFiles);
            Assert.IsFalse(value);
        }
        [Test]
        public void InvalidConfigPath()
        {
            var nppMock = new Mock<INpp>();
            nppMock.Setup<string>(x => x.GetConfigDir()).Returns("crash and burn");
            ConfigurationSetter s = new ConfigurationSetter(nppMock.Object);
            s.saveSetting<bool>(false, Settings.RTextNppSettings.AutoSaveFiles);
            bool value = true;
            s.readSetting<bool>(ref value, Settings.RTextNppSettings.AutoSaveFiles);
            //no exception should be thrown...
        }
        [Test]
        public void EnsureStabilityWithOldConfigurationFile()
        {
            var nppMock = new Mock<INpp>();
            nppMock.Setup<string>(x => x.GetConfigDir()).Returns(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            ConfigurationSetter s = new ConfigurationSetter(nppMock.Object);
            s.saveSetting<bool>(false, Settings.RTextNppSettings.AutoSaveFiles);
            //remove AutoSaveFiles setting
            XmlDocument aDoc = new XmlDocument();
            string configFile = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) + "\\" + "RTextNpp.dll.config";
            aDoc.Load(configFile);
            foreach(XmlNode kvPair in aDoc.DocumentElement.FirstChild.ChildNodes)
            {
                if(kvPair.Attributes["key"].Value.Equals(Settings.RTextNppSettings.AutoSaveFiles.ToString()))
                {
                    aDoc.DocumentElement.FirstChild.RemoveChild(kvPair);
                    aDoc.Save(configFile);
                    break;
                }
            }
            //esnure default value for setting
            bool value = false;
            s.readSetting<bool>(ref value, Settings.RTextNppSettings.AutoSaveFiles);
            Assert.IsTrue(value);
        }
        private ConfigurationSetter _multiThreadConfiguration = null;
        [Test]
        public void AddNameThreadSafetyTest()
        {
            var t1 = new Thread(SaveAutoSaveFilesSettingsMany);
            var t2 = new Thread(SaveAutoSwitchMany);
            var t3 = new Thread(SaveAutoSaveFilesSettingsMany);
            var t4 = new Thread(SaveAutoSwitchMany);
            var nppMock = new Mock<INpp>();
            nppMock.Setup<string>(x => x.GetConfigDir()).Returns(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            _multiThreadConfiguration = new ConfigurationSetter(nppMock.Object);
            t1.Priority = ThreadPriority.Lowest;
            t2.Priority = ThreadPriority.Lowest;
            //t3.Priority = ThreadPriority.Lowest;
            //t4.Priority = ThreadPriority.Lowest;
            t1.Start();
            t2.Start();
            //t3.Start();
            //t4.Start();
            t1.Join();
            t2.Join();
            //t3.Join();
            //t4.Join();
            bool aAutosaveFiles       = false;
            _multiThreadConfiguration.readSetting(ref aAutosaveFiles, Settings.RTextNppSettings.AutoSaveFiles);
            Assert.IsTrue(aAutosaveFiles);
        }
        private void SaveAutoSaveFilesSettingsMany()
        {
            for (int x = 0; x < 1000; x++)
            {
                _multiThreadConfiguration.saveSetting<bool>(true, Settings.RTextNppSettings.AutoSaveFiles);
            }
        }
        private void SaveAutoSwitchMany()
        {
            for (int x = 0; x < 1000; x++)
            {
                bool aAutosaveFiles = false;
                _multiThreadConfiguration.readSetting(ref aAutosaveFiles, Settings.RTextNppSettings.AutoSaveFiles);
                Assert.IsTrue(aAutosaveFiles);
            }
        }
    }
}
