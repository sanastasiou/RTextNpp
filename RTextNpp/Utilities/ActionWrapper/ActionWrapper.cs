﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
namespace RTextNppPlugin.Utilities
{
    public class ActionWrapper<R, T1> : IActionWrapper<R>
    {
        private readonly Func<T1, R> _action;
        private readonly T1 _arg;
        public R DoAction()
        {
            return _action(_arg);
        }
        public ActionWrapper(Func<T1, R> action, T1 arg)
        {
            _action = action;
            _arg    = arg;
        }
    }
    public class ActionWrapper<R, T1, T2> : IActionWrapper<R>
    {
        private readonly Func<T1, T2, R> _action;
        private readonly T1 _arg1;
        private readonly T2 _arg2;
        public R DoAction()
        {
            return _action(_arg1, _arg2);
        }
        public ActionWrapper(Func<T1, T2, R> action, T1 arg1 = default(T1), T2 arg2 = default(T2))
        {
            _action = action;
            _arg1 = arg1;
            _arg2 = arg2;
        }
    }
    public class ActionWrapper<R, T1, T2, T3> : IActionWrapper<R>
    {
        private readonly Func<T1, T2, T3, R> _action;
        private readonly T1 _arg1;
        private readonly T2 _arg2;
        private readonly T3 _arg3;
        public R DoAction()
        {
            return _action(_arg1, _arg2, _arg3);
        }
        public ActionWrapper(Func<T1, T2, T3, R> action, T1 arg1 = default(T1), T2 arg2 = default(T2), T3 arg3 = default(T3))
        {
            _action = action;
            _arg1 = arg1;
            _arg2 = arg2;
            _arg3 = arg3;
        }
    }
    public class ActionWrapper
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