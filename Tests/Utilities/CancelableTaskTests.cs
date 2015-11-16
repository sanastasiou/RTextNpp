namespace Tests.Utilities
{
    using System;
    using NUnit.Framework;
    using System.Threading;
    using RTextNppPlugin.Utilities.Threading;
    [TestFixture]
    class CancelableTaskTests
    {
        private bool _retValue;
        private int _workDelay;

        private bool WorkFunction()
        {
            Thread.Sleep(_workDelay);
            return _retValue;
        }

        [SetUp]
        public void Init()
        {
            _retValue = false;
            _workDelay = 0;
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
            Assert.DoesNotThrow(delegate { t = new CancelableTask<bool>(new Func<bool>(WorkFunction), 0); });
            
            _workDelay = 100;

            Assert.DoesNotThrow(delegate { t.Execute(); });

            Assert.True(t.IsCancelled);
            Assert.AreEqual(t.Result, default(bool));

        }

        [Test]
        public void NormalDelayTest()
        {
            CancelableTask<bool> t = null;
            Assert.DoesNotThrow(delegate { t = new CancelableTask<bool>(new Func<bool>(WorkFunction), 200); });

            _workDelay = 100;
            _retValue = true;

            Assert.DoesNotThrow(delegate { t.Execute(); });

            Assert.False(t.IsCancelled);
            Assert.True(t.Result);

        }

        [Test]
        public void NormalDelayTestTaskExceedsLimit()
        {
            CancelableTask<bool> t = null;
            Assert.DoesNotThrow(delegate { t = new CancelableTask<bool>(new Func<bool>(WorkFunction), 200); });

            _workDelay = 300;
            _retValue = true;

            Assert.DoesNotThrow(delegate { t.Execute(); });

            Assert.True(t.IsCancelled);
            Assert.False(t.Result);

        }
    }
}
