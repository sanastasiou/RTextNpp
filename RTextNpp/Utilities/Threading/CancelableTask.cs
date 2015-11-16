using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using RTextNppPlugin.Utilities;
using System.Threading;
using System.Diagnostics;

namespace RTextNppPlugin.Utilities.Threading
{
    internal class CancelableTask<T>
    {
        #region [Data Members]
        bool _isCancelled       = false;
        CancellationToken _cancellationToken;
        Task<T> _backgroundTask = null;
        T _taskResult           = default(T);
        Func<T> _taskCallback;
        int _cancellationDelay = default(int);

        #endregion

        internal CancelableTask (Func<T> taskCallback, int cancellationDelay)
        {
            if(taskCallback == null)
            {
                throw new ArgumentNullException("wrapper");
            }
            _taskCallback      = taskCallback;
            _cancellationDelay = cancellationDelay;
        }

        internal bool IsCancelled
        {
            get
            {
                return _isCancelled;
            }
        }

        internal T Result
        {
            get
            {
                return _taskResult;
            }
        }

        internal void Execute()
        {
            try
            {
                _cancellationToken = new CancellationTokenSource(_cancellationDelay).Token;
                _backgroundTask = Task.Run<T>(() =>
                {
                    //if do action takes longer than delay, task will be cancelled
                    var aResult = _taskCallback.Invoke();
                    _cancellationToken.ThrowIfCancellationRequested();

                    return aResult;
                }, _cancellationToken);
                _backgroundTask.Wait();
                _taskResult = _backgroundTask.Result;
            }
            catch (OperationCanceledException ex)
            {
                _isCancelled = true;
                Trace.WriteLine(String.Format("Execute exception : {0}", ex.Message));
            }
            catch(AggregateException ex)
            {
                _isCancelled = true;
                Trace.WriteLine(String.Format("Execute exception : {0}", ex.InnerException.Message)); 
            }
            finally
            {
                _backgroundTask.Dispose();
            }
        }
    }
}
