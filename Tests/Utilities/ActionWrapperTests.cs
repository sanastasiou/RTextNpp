using System;
namespace Tests.Utilities
{
    using NUnit.Framework;
    using RTextNppPlugin.Utilities;
    [TestFixture]
    class ActionWrapperTests
    {
        [Test]
        public void ActionWrapperTestNoArguments()
        {
            int i = 0;
            Action a = new Action(() => { i++; });
            ActionWrapper w = new ActionWrapper(a);
            w.DoAction();
            Assert.AreEqual(i, 1);
        }
        [Test]
        public void ActionWrapperTestOneArgument()
        {
            int i = 0;
            Func<int, object> a = new Func<int, object>((x) => { i = x; return null; });
            ActionWrapper<object, int> w = new ActionWrapper<object, int>(a, 5);
            w.DoAction();
            Assert.AreEqual(i, 5);
        }
        [Test]
        public void ActionWrapperTestTwoArguments()
        {
            int i = 0;
            bool b = true;
            Func<int, bool, object> a = new Func<int, bool, object>((integer, boolean) =>
            {
                i = integer;
                b = boolean; return null;
            });
            ActionWrapper<object, int, bool> w = new ActionWrapper<object, int, bool>(a, 5, false);
            w.DoAction();
            Assert.AreEqual(i, 5);
            Assert.AreEqual(b, false);
        }
        [Test]
        public void ActionWrapperTestThreeArguments()
        {
            int i = 0;
            double j = 0.0;
            uint l = 5;
            Func<int, double, uint, object> a = new Func<int, double, uint, object>((integer, d, c) =>
            {
                i = integer;
                j = d;
                l = c;
                return null;
            });
            ActionWrapper<object, int, double, uint> w = new ActionWrapper<object, int, double, uint>(a, 10, 5.0, 9);
            w.DoAction();
            Assert.AreEqual(i, 10);
            Assert.AreEqual(j, 5.0);
            Assert.AreEqual(l, 9);
        }
    }
}
