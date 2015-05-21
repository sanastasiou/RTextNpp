using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTextNppPlugin.RText.StateEngine
{
    internal class ActionWrapper<T1> : IActionWrapper where T1 : class
    {
        private readonly Action<T1> _action;
        private readonly T1 _arg;

        public void DoAction()
        {
            _action(_arg);
        }

        public ActionWrapper(Action<T1> action, T1 arg) 
        {
            _action = action;
            _arg    = arg;
        }
    }

    internal class ActionWrapper<T1, T2> : IActionWrapper
        where T1 : class
        where T2 : class
    {
        private readonly Action<T1, T2> _action;
        private readonly T1 _arg1;
        private readonly T2 _arg2;

        public void DoAction()
        {
            _action(_arg1, _arg2);
        }

        public ActionWrapper(Action<T1, T2> action, T1 arg1, T2 arg2)
        {
            _action = action;
            _arg1 = arg1;
            _arg2 = arg2;
        }
    }

    internal class ActionWrapper<T1, T2, T3> : IActionWrapper
        where T1 : class
        where T2 : class
        where T3 : class
    {
        private readonly Action<T1, T2, T3> _action;
        private readonly T1 _arg1;
        private readonly T2 _arg2;
        private readonly T3 _arg3;

        public void DoAction()
        {
            _action(_arg1, _arg2, _arg3);
        }

        public ActionWrapper(Action<T1, T2, T3> action, T1 arg1, T2 arg2, T3 arg3)
        {
            _action = action;
            _arg1 = arg1;
            _arg2 = arg2;
            _arg3 = arg3;
        }
    }

    internal class ActionWrapper : IActionWrapper
    {
        private readonly Action _action;

        public void DoAction()
        {
            _action();
        }

        public ActionWrapper(Action action)
        {
            _action = action;
        }
    }
}
