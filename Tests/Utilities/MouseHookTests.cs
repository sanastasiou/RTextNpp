using System;

namespace Tests.Utilities
{
    using System.Runtime.InteropServices;
    using CSScriptIntellisense;
    using Moq;
    using NUnit.Framework;
    using RTextNppPlugin.DllExport;
    using RTextNppPlugin.Utilities;

    [TestFixture]
    class GlobalMouseHookTests
    {
        #region [Data Members]
        int _clickCount;
        IWin32 _win32Helper;
        Mock<IWin32> _win32Mock;        

        MouseLLHookStruct _wheelStruct = new MouseLLHookStruct();
        IntPtr _wheelStructPtr = IntPtr.Zero;
        #endregion

        [SetUp]
        public void Init()
        {
            _win32Mock = new Mock<IWin32>();
            _win32Helper = _win32Mock.Object;
            _clickCount = 0;
            _wheelStructPtr = Marshal.AllocHGlobal(Marshal.SizeOf(_wheelStruct));
            Marshal.StructureToPtr(_wheelStruct, _wheelStructPtr, true);
        }

        [TearDown]
        public void Dispose()
        {
            Marshal.FreeHGlobal(_wheelStructPtr);
            _wheelStructPtr = IntPtr.Zero; 
        }

        [Test]
        public void InitializationTest()
        {
            GlobalClickInterceptor monitor = new GlobalClickInterceptor(_win32Helper);
        }

        [Test]
        public void SubscriptionTest()
        {
            GlobalClickInterceptor monitor = new GlobalClickInterceptor(_win32Helper);
            monitor.MouseClick += monitor_MouseClick;
            //second subscriber ignroed
            monitor.MouseClick += monitor_MouseClick2;

            monitor.MouseHookProc(1, (UIntPtr)Convert.ToInt32(VisualUtilities.MouseMessages.WM_LBUTTONDOWN), _wheelStructPtr);
            monitor.MouseHookProc(1, (UIntPtr)Convert.ToInt32(VisualUtilities.MouseMessages.WM_NCRBUTTONDBLCLK), _wheelStructPtr);
            monitor.MouseHookProc(1, (UIntPtr)Convert.ToInt32(VisualUtilities.MouseMessages.WM_NCMBUTTONDBLCLK), _wheelStructPtr);

            Assert.AreEqual(2, _clickCount);
        }

        [Test, Sequential]
        public void ClickInterceptorTest([Values(VisualUtilities.MouseMessages.WM_LBUTTONDOWN, 
                                             VisualUtilities.MouseMessages.WM_RBUTTONDOWN,
                                             VisualUtilities.MouseMessages.WM_NCXBUTTONDOWN,
                                             VisualUtilities.MouseMessages.WM_NCMBUTTONDOWN,
                                             VisualUtilities.MouseMessages.WM_NCRBUTTONDBLCLK,
                                             VisualUtilities.MouseMessages.WM_NCRBUTTONDOWN,
                                             VisualUtilities.MouseMessages.WM_NCLBUTTONDOWN
                                             )] VisualUtilities.MouseMessages mouseMessage)
        {
            GlobalClickInterceptor monitor = new GlobalClickInterceptor(_win32Helper);
            monitor.MouseClick += monitor_MouseClick;
            //second subscriber ignroed
            monitor.MouseClick += monitor_MouseClick2;

            monitor.MouseHookProc(1, (UIntPtr)Convert.ToInt32(mouseMessage), _wheelStructPtr);

            Assert.AreEqual(1, _clickCount);
        }

        [Test, Sequential]
        public void ClickInterceptorNoSubscriber([Values(VisualUtilities.MouseMessages.WM_LBUTTONDOWN,
                                                         VisualUtilities.MouseMessages.WM_RBUTTONDOWN,
                                                         VisualUtilities.MouseMessages.WM_NCXBUTTONDOWN,
                                                         VisualUtilities.MouseMessages.WM_NCMBUTTONDOWN,
                                                         VisualUtilities.MouseMessages.WM_NCRBUTTONDBLCLK,
                                                         VisualUtilities.MouseMessages.WM_NCRBUTTONDOWN,
                                                         VisualUtilities.MouseMessages.WM_NCLBUTTONDOWN
                                                         )] VisualUtilities.MouseMessages mouseMessage)
        {
            GlobalClickInterceptor monitor = new GlobalClickInterceptor(_win32Helper);

            monitor.MouseHookProc(1, (UIntPtr)Convert.ToInt32(mouseMessage), _wheelStructPtr);

            monitor.MouseClick -= monitor_MouseClick;            

            Assert.AreEqual(0, _clickCount);
        }

        [Test, Sequential]
        public void ClickInterceptorNoSubscriberNoValidEvent([Values(VisualUtilities.MouseMessages.WM_MOUSEMOVE,
                                                              VisualUtilities.MouseMessages.WM_MOUSEWHEEL
                                                              )] VisualUtilities.MouseMessages mouseMessage)
        {
            GlobalClickInterceptor monitor = new GlobalClickInterceptor(_win32Helper);
            monitor.MouseClick += monitor_MouseClick;
            //second subscriber ignroed
            monitor.MouseClick += monitor_MouseClick2;

            monitor.MouseHookProc(1, (UIntPtr)Convert.ToInt32(mouseMessage), _wheelStructPtr);

            Assert.AreEqual(0, _clickCount);
        }

        [Test]
        public void SubscriptionTestHandled()
        {
            GlobalClickInterceptor monitor = new GlobalClickInterceptor(_win32Helper);
            monitor.MouseClick += monitor_MouseClick2;

            monitor.MouseHookProc(1, (UIntPtr)Convert.ToInt32(VisualUtilities.MouseMessages.WM_LBUTTONDOWN), _wheelStructPtr);

            Assert.AreEqual(1, _clickCount);
        }

        [Test]
        public void UnsubscribeTestNullPtr()
        {
            GlobalClickInterceptor monitor = new GlobalClickInterceptor(_win32Helper);
            monitor.MouseClick += monitor_MouseClick2;

            monitor.MouseHookProc(1, (UIntPtr)Convert.ToInt32(VisualUtilities.MouseMessages.WM_LBUTTONDOWN), _wheelStructPtr);
            monitor.MouseClick -= monitor_MouseClick2;

            Assert.AreEqual(1, _clickCount);
        }

        [Test]
        public void UnsubscribeTest()
        {
            GlobalClickInterceptor monitor = new GlobalClickInterceptor(_win32Helper);
            _win32Mock.Setup<IntPtr>(x => x.ISetWindowsHookEx(It.IsAny<VisualUtilities.HookType>(), It.IsAny<Win32.HookProc>(), It.IsAny<IntPtr>(), It.IsAny<int>())).Returns(_wheelStructPtr);
            monitor.MouseClick += monitor_MouseClick2;

            monitor.MouseHookProc(1, (UIntPtr)Convert.ToInt32(VisualUtilities.MouseMessages.WM_LBUTTONDOWN), _wheelStructPtr);
            monitor.MouseClick -= monitor_MouseClick2;

            Assert.AreEqual(1, _clickCount);
        }

        private void monitor_MouseClick(object sender, MouseEventExtArgs e)
        {
            ++_clickCount;
        }

        private void monitor_MouseClick2(object sender, MouseEventExtArgs e)
        {
            e.Handled = true;
            ++_clickCount;
        }
    }
}
