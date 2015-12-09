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
    class SettingsTests
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
            Settings s = new Settings(nppMock.Object);
        }
        [Test]
        public void ReadWriteTestValid()
        {
            var nppMock = new Mock<INpp>();
            nppMock.Setup<string>(x => x.GetConfigDir()).Returns(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            Settings s = new Settings(nppMock.Object);
            s.Set<bool>(false, Settings.RTextNppSettings.AutoSaveFiles);
            bool value = s.Get<bool>(Settings.RTextNppSettings.AutoSaveFiles);
            Assert.IsFalse(value);
        }
        [Test]
        public void ReadWriteString()
        {
            var nppMock = new Mock<INpp>();
            nppMock.Setup<string>(x => x.GetConfigDir()).Returns(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            Settings s = new Settings(nppMock.Object);
            s.Set<string>("SomeExtension", Settings.RTextNppSettings.ExcludeExtensions);
            string value = s.Get(Settings.RTextNppSettings.ExcludeExtensions);
            Assert.AreEqual("SomeExtension", value);
        }
        [Test]
        public void OnSettingUpdatedTest()
        {
            var nppMock = new Mock<INpp>();
            nppMock.Setup<string>(x => x.GetConfigDir()).Returns(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location));
            Settings s = new Settings(nppMock.Object);
            Settings eventSender                  = null;
            Settings.SettingChangedEventArgs args = null;
            s.OnSettingChanged += (x, y) =>
            {
                eventSender = (Settings)x;
                args        = (Settings.SettingChangedEventArgs)y;
            };
            s.Set<string>("SomeExtension", Settings.RTextNppSettings.ExcludeExtensions);
            Assert.AreEqual(eventSender, s);
            Assert.AreEqual(args.Setting, Settings.RTextNppSettings.ExcludeExtensions);
        }
    }
}
