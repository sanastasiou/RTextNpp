using System;
namespace Tests.Utilities
{
    using System.Diagnostics;
    using NUnit.Framework;
    using RTextNppPlugin.Utilities;
    [TestFixture]
    class WindowsMessageInterceptorTests
    {
        ScintillaMessageInterceptor _sciInterceptor;
        NotepadMessageInterceptor _nppInterceptor;
        [SetUp]
        public void Init()
        {
            _sciInterceptor = new ScintillaMessageInterceptor(IntPtr.Zero);
            _nppInterceptor = new NotepadMessageInterceptor(IntPtr.Zero);
        }
        [TearDown]
        public void Dispose()
        {
            _sciInterceptor.MouseWheelMoved       -= _sciInterceptor_MouseWheelMoved;
            _sciInterceptor.ScintillaFocusChanged -= _sciInterceptor_ScintillaFocusChanged;
        }
        [Test]
        public void TestMouseWheelScintilla()
        {
            _sciInterceptor.MouseWheelMoved += _sciInterceptor_MouseWheelMoved;
            Assert.IsTrue(_sciInterceptor.OnMessageReceived((uint)VisualUtilities.WindowsMessage.WM_MOUSEWHEEL, UIntPtr.Zero, IntPtr.Zero));
        }
        void _sciInterceptor_MouseWheelMoved(object source, ScintillaMessageInterceptor.MouseWheelMovedEventArgs e)
        {
            Assert.AreEqual(e.Msg, (uint)VisualUtilities.WindowsMessage.WM_MOUSEWHEEL);
            Assert.AreEqual(e.LParam, IntPtr.Zero);
            Assert.AreEqual(e.WParam, UIntPtr.Zero);
            e.Handled = true;
        }
        [Test]
        public void TestKillFocusScintilla()
        {
            _sciInterceptor.ScintillaFocusChanged += _sciInterceptor_ScintillaFocusChanged;
            Assert.IsTrue(_sciInterceptor.OnMessageReceived((uint)VisualUtilities.WindowsMessage.WM_KILLFOCUS, UIntPtr.Zero, IntPtr.Zero));
        }
        void _sciInterceptor_ScintillaFocusChanged(object source, ScintillaMessageInterceptor.ScintillaFocusChangedEventArgs e)
        {
            Assert.AreEqual(e.Focused, false);
            Assert.AreEqual(e.WindowHandle, UIntPtr.Zero);
            e.Handled = true;
        }
        [Test]
        public void TestSetFocusScintilla()
        {
            _sciInterceptor.ScintillaFocusChanged += _sciInterceptor_ScintillaFocusChangedSet;
            Assert.IsTrue(_sciInterceptor.OnMessageReceived((uint)VisualUtilities.WindowsMessage.WM_SETFOCUS, UIntPtr.Zero, IntPtr.Zero));
        }
        void _sciInterceptor_ScintillaFocusChangedSet(object source, ScintillaMessageInterceptor.ScintillaFocusChangedEventArgs e)
        {
            Assert.AreEqual(e.Focused, true);
            Assert.AreEqual(e.WindowHandle, UIntPtr.Zero);
            e.Handled = true;
        }
        [Test]
        public void TestSetFocusScintillaNoSubscribers()
        {
            Assert.IsFalse(_sciInterceptor.OnMessageReceived((uint)VisualUtilities.WindowsMessage.WM_KILLFOCUS, UIntPtr.Zero, IntPtr.Zero));
            Assert.IsFalse(_sciInterceptor.OnMessageReceived((uint)VisualUtilities.WindowsMessage.WM_SETFOCUS, UIntPtr.Zero, IntPtr.Zero));
            Assert.IsFalse(_sciInterceptor.OnMessageReceived((uint)VisualUtilities.WindowsMessage.WM_MOUSEWHEEL, UIntPtr.Zero, IntPtr.Zero));
            Assert.IsFalse(_sciInterceptor.OnMessageReceived((uint)VisualUtilities.WindowsMessage.WM_GETDLGCODE, UIntPtr.Zero, IntPtr.Zero));
        }
        [Test]
        public void TestNppNoSubscribers()
        {
            Assert.IsFalse(_nppInterceptor.OnMessageReceived(0, UIntPtr.Zero, IntPtr.Zero));
            Assert.IsFalse(_nppInterceptor.OnMessageReceived((uint)VisualUtilities.WindowsMessage.WM_GETDLGCODE, UIntPtr.Zero, IntPtr.Zero));
        }
        [Test]
        public void TestNppLoopEntered()
        {
            _nppInterceptor.MenuLoopStateChanged += _nppInterceptor_MenuLoopStateChangedEntered;
            Assert.IsTrue(_nppInterceptor.OnMessageReceived((uint)VisualUtilities.WindowsMessage.WM_ENTERMENULOOP, UIntPtr.Zero, IntPtr.Zero));
        }
        void _nppInterceptor_MenuLoopStateChangedEntered(object source, NotepadMessageInterceptor.MenuLoopStateChangedEventArgs e)
        {
            Assert.IsTrue(e.IsMenuLoopActive);
            e.Handled = true;
        }
        [Test]
        public void TestNppLoopExit()
        {
            _nppInterceptor.MenuLoopStateChanged += _nppInterceptor_MenuLoopStateChangedExit;
            Assert.IsTrue(_nppInterceptor.OnMessageReceived((uint)VisualUtilities.WindowsMessage.WM_EXITMENULOOP, UIntPtr.Zero, IntPtr.Zero));
        }
        void _nppInterceptor_MenuLoopStateChangedExit(object source, NotepadMessageInterceptor.MenuLoopStateChangedEventArgs e)
        {
            Assert.IsFalse(e.IsMenuLoopActive);
            e.Handled = true;
        }
    }
}
