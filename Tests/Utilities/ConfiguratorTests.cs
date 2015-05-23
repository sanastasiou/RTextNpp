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

    [TestFixture]
    class ConfiguratorTestsNonExistingFile
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
    }
}
