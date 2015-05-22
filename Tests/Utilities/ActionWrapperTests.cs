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

            Action<int> a = new Action<int>((x) => { i = x; });

            ActionWrapper<int> w = new ActionWrapper<int>(a, 5);

            w.DoAction();

            Assert.AreEqual(i, 5);
        }

        [Test]
        public void ActionWrapperTestTwoArguments()
        {
            int i = 0;
            bool b = true;

            Action<int, bool> a = new Action<int, bool>((integer, boolean) => { i = integer;
                b = boolean; });

            ActionWrapper<int, bool> w = new ActionWrapper<int, bool>(a, 5, false);

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

            Action<int, double, uint> a = new Action<int, double, uint>((integer,d,c) =>
            { 
                i = integer;
                j = d;
                l = c;
            });

            ActionWrapper<int, double, uint> w = new ActionWrapper<int, double, uint>(a, 10, 5.0, 9);

            w.DoAction();

            Assert.AreEqual(i, 10);
            Assert.AreEqual(j, 5.0);
            Assert.AreEqual(l, 9);
        }
    }
}
