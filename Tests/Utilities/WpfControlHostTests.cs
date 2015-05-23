using System;

namespace Tests.Utilities
{
    using System.Threading;
    using System.Windows.Forms;
    using Moq;
    using NUnit.Framework;
    using RTextNppPlugin.Utilities;
    using RTextNppPlugin.Utilities.Settings;
    using RTextNppPlugin.Utilities.WpfControlHost;

    [TestFixture]
    class WpfControlHostTests
    {
        [Test, STAThread]
        public void InitializationTest()
        {
            var nppMock = new Mock<INpp>();
            Form f = new Form();
            WpfControlHostBase<Form> aHost = new WpfControlHostBase<Form>(f, nppMock.Object);

            Assert.IsFalse(aHost.Handle == IntPtr.Zero);

            aHost.Dispose();
            //except no exception to be thrown
            aHost.Dispose();

            f = new Form();
            var uiThread = new Thread(() => Application.Run(f));
            uiThread.SetApartmentState(ApartmentState.STA);
            uiThread.Start();
            Thread.Sleep(100);

            IntPtr handle = IntPtr.Zero;
            aHost = new WpfControlHostBase<Form>(f, nppMock.Object);

            var thread = new Thread(() => handle = aHost.Handle);

            // Execute
            thread.Start();
            thread.Join();
            f.Close();
            uiThread.Join();
            Assert.IsFalse(handle == IntPtr.Zero);
        }

        [Test, STAThread]
        public void CommandIdTest()
        {
            var nppMock = new Mock<INpp>();
            Form f = new Form();
            WpfControlHostBase<Form> aHost = new WpfControlHostBase<Form>(f, nppMock.Object);

            aHost.CmdId = 5;

            Assert.AreEqual(aHost.CmdId, 5);

            f.Close();

            f = new Form();
            var uiThread = new Thread(() => Application.Run(f));
            uiThread.SetApartmentState(ApartmentState.STA);
            uiThread.Start();
            Thread.Sleep(100);

            aHost = new WpfControlHostBase<Form>(f, nppMock.Object);

            var thread = new Thread(() => { aHost.CmdId = 10; });

            // Execute
            thread.Start();
            thread.Join();
            Assert.AreEqual(aHost.CmdId, 10);
            f.Close();
            uiThread.Join();
            
        }

        [Test, STAThread]
        public void IsVisibleTest()
        {
            var nppMock = new Mock<INpp>();
            Form f = new Form();
            WpfControlHostBase<Form> aHost = new WpfControlHostBase<Form>(f, nppMock.Object);

            f.Visible = false;

            aHost.CmdId = 5;

            aHost.OnVisibilityChanged(null, new EventArgs());

            nppMock.Verify(m => m.ChangeMenuItemCheck(5, true), Times.Never());
            nppMock.Verify(m => m.ChangeMenuItemCheck(5, false), Times.Once());

            f.Visible = true;

            aHost.OnVisibilityChanged(null, new EventArgs());

            nppMock.Verify(m => m.ChangeMenuItemCheck(5, true), Times.AtLeastOnce());

            DispatcherUtil.DoEvents();

            Thread.Sleep(2000);

            nppMock.Verify(m => m.ChangeMenuItemCheck(5, true), Times.AtLeastOnce());

            DispatcherUtil.DoEvents();

            f = new Form();
            var uiThread = new Thread(() => Application.Run(f));
            uiThread.SetApartmentState(ApartmentState.STA);
            uiThread.Start();
            Thread.Sleep(100);

            aHost = new WpfControlHostBase<Form>(f, nppMock.Object);

            var thread = new Thread(() => { aHost.OnVisibilityChanged(null, new EventArgs()); });

            // Execute
            thread.Start();
            thread.Join();
            nppMock.Verify(m => m.ChangeMenuItemCheck(5, true), Times.AtLeastOnce());
            f.Close();
            uiThread.Join();

        }

        [Test, STAThread]
        public void VisibleTest()
        {
            var nppMock = new Mock<INpp>();
            Form f = new Form();
            WpfControlHostBase<Form> aHost = new WpfControlHostBase<Form>(f, nppMock.Object);

            f.Visible = true;

            Assert.AreEqual(aHost.Visible, f.Visible);

            aHost = new WpfControlHostBase<Form>(f, nppMock.Object);

            bool isHostVisible = false;

            var thread = new Thread(() => { isHostVisible = aHost.Visible; });

            // Execute
            thread.Start();
            DispatcherUtil.DoEvents();
            thread.Join();
            Assert.True(isHostVisible);
            f.Close();
        }

        [Test, STAThread]
        public void GetElementHostTest()
        {
            var nppMock = new Mock<INpp>();
            Form f = new Form();
            WpfControlHostBase<Form> aHost = new WpfControlHostBase<Form>(f, nppMock.Object);

            Assert.AreEqual(f, aHost.ElementHost);
        }

        [Test, STAThread]
        public void FocusTest()
        {
            var nppMock = new Mock<INpp>();
            Form f = new Form();
            WpfControlHostBase<Form> aHost = new WpfControlHostBase<Form>(f, nppMock.Object);

            f.Show();

            Assert.True(aHost.Focus());

            bool focus = false;

            var thread = new Thread(() => { focus = aHost.Focus(); });

            // Execute
            thread.Start();
            DispatcherUtil.DoEvents();
            thread.Join();
            Assert.True(focus);
            f.Close();
        }

        [Test, STAThread]
        public void PersistentHostTest()
        {
            var nppMock = new Mock<INpp>();
            var nppIsMock = new Mock<ISettings>();
            Form f = new Form();

            PersistentWpfControlHost<Form> aHost = new PersistentWpfControlHost<Form>(Settings.RTextNppSettings.ConsoleWindowActive, f, nppIsMock.Object, nppMock.Object);
            aHost.OnVisibilityChanged(null, new EventArgs());
        }
    }
}
