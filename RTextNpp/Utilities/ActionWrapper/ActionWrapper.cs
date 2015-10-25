using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace RTextNppPlugin.Utilities
{
    public class ActionWrapper<T1> : IActionWrapper
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
    public class ActionWrapper<T1, T2> : IActionWrapper
    {
        private readonly Action<T1, T2> _action;
        private readonly T1 _arg1;
        private readonly T2 _arg2;
        public void DoAction()
        {
            _action(_arg1, _arg2);
        }
        public ActionWrapper(Action<T1, T2> action, T1 arg1 = default(T1), T2 arg2 = default(T2))
        {
            _action = action;
            _arg1 = arg1;
            _arg2 = arg2;
        }
    }
    public class ActionWrapper<T1, T2, T3> : IActionWrapper
    {
        private readonly Action<T1, T2, T3> _action;
        private readonly T1 _arg1;
        private readonly T2 _arg2;
        private readonly T3 _arg3;
        public void DoAction()
        {
            _action(_arg1, _arg2, _arg3);
        }
        public ActionWrapper(Action<T1, T2, T3> action, T1 arg1 = default(T1), T2 arg2 = default(T2), T3 arg3 = default(T3))
        {
            _action = action;
            _arg1 = arg1;
            _arg2 = arg2;
            _arg3 = arg3;
        }
    }
    public class ActionWrapper : IActionWrapper
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