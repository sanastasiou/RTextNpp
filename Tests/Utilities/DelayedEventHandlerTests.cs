using System;

namespace Tests.Utilities
{
    using System.Diagnostics;
    using System.Windows.Threading;
    using NUnit.Framework;
    using RTextNppPlugin.Utilities;

    [TestFixture]
    class DelayedEventHandlerTests : TestWithActiveDispatcher
    {
        #region [Data Members]
        DelayedEventHandler _noArgHandler         = null;
        bool _isNoArgHandlerCalled                = false;
        int _count = 0;
        #endregion

        [SetUp]
        public void Init()
        {
            // VERY IMPORTANT!!!
            // For the DispatcherTimer to tick when it is not running
            // in a normal WPF application, you must give it a priority
            // higher than 'Background' (which is the default priority). 
            // In this demo we give it a priority of 'Normal'.
            _noArgHandler           = null;
            _isNoArgHandlerCalled   = false;
            _count                  = 0;
            
        }

        [Test]
        public void DelayedExecutionTest()
        {
            Assert.IsFalse(_isNoArgHandlerCalled);

            base.BeginExecuteTest(TriggerSingle);

            Assert.True(_isNoArgHandlerCalled);
        }

        [Test]
        public void DelayedExecutionTestCancel()
        {
            Assert.IsFalse(_isNoArgHandlerCalled);

            base.BeginExecuteTest(TriggerCancel);

            Assert.False(_isNoArgHandlerCalled);
        }

        [Test]
        public void DelayedExecutionTestMultipleTriggers()
        {
            Assert.IsFalse(_isNoArgHandlerCalled);

            base.BeginExecuteTest(TriggerMultiple);

            Assert.True(_isNoArgHandlerCalled);
            Assert.AreEqual(_count, 1);
            Assert.IsFalse(_noArgHandler.IsRunning);
        }

        protected override void ExecuteTestAsync()
        {
            Debug.Assert(base.IsRunningOnWorkerThread);

            // Note: The object which creates a DispatcherTimer
            // must create it with the Dispatcher for the worker
            // thread.  Creating the Ticker on the worker thread
            // ensures that its DispatcherTimer uses the worker
            // thread's Dispatcher.
            _noArgHandler = new DelayedEventHandler(new ActionWrapper(() => { _isNoArgHandlerCalled = true; ++_count; }), 100, DispatcherPriority.Normal);

            base._testDelegate.Invoke();

            // Give the Ticker some time to do its work.
            base.WaitWithoutBlockingDispatcher(TimeSpan.FromMilliseconds(500));

            // Let the base class know that the test is over
            // so that it can turn off the worker thread's
            // message pump.
            base.EndExecuteTest();

        }

        private void TriggerSingle()
        {
            _noArgHandler.TriggerHandler();
        }

        private void TriggerMultiple()
        {
            _noArgHandler.TriggerHandler();
            _noArgHandler.TriggerHandler(new ActionWrapper(() => { _isNoArgHandlerCalled = true; ++_count; }));
        }

        private void TriggerCancel()
        {
            _noArgHandler.TriggerHandler();
            _noArgHandler.Cancel();
        }

    }
}
