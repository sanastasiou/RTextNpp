namespace Tests.Utilities
{
    using System;
    using NUnit.Framework;
    using System.Threading;
    using RTextNppPlugin.Utilities;
    using RTextNppPlugin.Utilities.Threading;
    [TestFixture]
    class CancelableTaskTests
    {
        private bool _retValue;

        private bool WorkFunction(int _workDelay)
        {
            Thread.Sleep(_workDelay);
            return _retValue;
        }

        [SetUp]
        public void Init()
        {
            _retValue = false;
        }

        [Test]
        public void InitializationTest()
        {
            Assert.Throws<ArgumentNullException>( delegate { CancelableTask<bool> t = new CancelableTask<bool>(null, 0); });
        }

        [Test]
        public void NullDelayTest()
        {
            CancelableTask<bool> t = null;
            Assert.DoesNotThrow(delegate { t = new CancelableTask<bool>(new ActionWrapper<bool, int>(WorkFunction, 100), 0); });
            
            Assert.DoesNotThrow(delegate { t.Execute(); });

            Assert.True(t.IsCancelled);
            Assert.AreEqual(t.Result, default(bool));

        }

        [Test]
        public void NormalDelayTest()
        {
            CancelableTask<bool> t = null;
            Assert.DoesNotThrow(delegate { t = new CancelableTask<bool>(new ActionWrapper<bool, int>(WorkFunction, 100), 200); });

            _retValue = true;

            Assert.DoesNotThrow(delegate { t.Execute(); });

            Assert.False(t.IsCancelled);
            Assert.True(t.Result);

        }

        [Test]
        public void NormalDelayTestTaskExceedsLimit()
        {
            CancelableTask<bool> t = null;
            Assert.DoesNotThrow(delegate { t = new CancelableTask<bool>(new ActionWrapper<bool, int>(WorkFunction, 2000), 100); });

            _retValue = true;

            t.Execute();

            Assert.True(t.IsCancelled);
            Assert.False(t.Result);

        }
    }
}
