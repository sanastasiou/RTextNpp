using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;
using System.Threading;

namespace RTextNppPlugin.Utilities
{
    /**
     * \class   ProcessUtilities
     *
     * \brief   Process utilities.
     *
     */
    class ProcessUtilities
    {
        /**
         *
         * \brief   Kill process tree of this parent process.
         *
         *
         * \param   root    The root.
         */
        public static void KillProcessTree(System.Diagnostics.Process root)
        {
            if (root != null)
            {
                var list = new List<System.Diagnostics.Process>();
                GetProcessAndChildren(System.Diagnostics.Process.GetProcesses(), root, list, 1);

                foreach (System.Diagnostics.Process p in list)
                {
                    try
                    {
                        if (!p.HasExited)
                        {
                            p.Kill();
                            p.WaitForExit();
                        }
                    }
                    catch (Exception ex)
                    {
                        Debug.Print(ex.Message);
                    }
                }
            }
        }

        /**
         *
         * \brief   Kill all processes spawned by a parent process.
         *
         *
         * \param   parentProcessId Identifier for the parent process.
         */
        public static void KillAllProcessesSpawnedBy(UInt32 parentProcessId)
        {
            // NOTE: Process Ids are reused!
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                "SELECT * " +
                "FROM Win32_Process " +
                "WHERE ParentProcessId=" + parentProcessId);
            ManagementObjectCollection collection = searcher.Get();
            if (collection.Count > 0)
            {
                foreach (var item in collection)
                {
                    UInt32 childProcessId = (UInt32)item["ProcessId"];
                    if ((int)childProcessId != System.Diagnostics.Process.GetCurrentProcess().Id)
                    {
                        KillAllProcessesSpawnedBy(childProcessId);

                        System.Diagnostics.Process childProcess = System.Diagnostics.Process.GetProcessById((int)childProcessId);
                        childProcess.Kill();
                    }
                }
            }
        }

        /**
         *
         * \brief   Tries to execute a function within a timeout.
         *
         *
         * \tparam  T   Generic type parameter.
         * \param   func        The function which will be executed.
         * \param   timeout     The timeout.
         * \param [out] result  The result of the executed function.
         *
         * \return  true if the functions returns a value before the timeout expires, false otherwise
         */
        public static bool TryExecute<TResult>(Func<TResult> func, int timeout, out TResult result)
        {
            var t = default(TResult);
            var thread = new Thread(() => t = func());
            thread.Start();
            var completed = thread.Join(timeout);
            if (!completed) thread.Abort();
            result = t;
            return completed;
        }

        public static bool TryExecute<TParam1, TResult>( Func< TParam1, TResult> func, int timeout, TParam1 param, out TResult result )
        {
            var t = default(TResult);
            var thread = new Thread(() => t = func(param));
            thread.Start();
            var completed = thread.Join(timeout);
            if (!completed) thread.Abort();
            result = t;
            return completed;
        }

        #region Helpers
        /**
         *      List<Process> output, int indent)
         *
         * \brief   Gets the children of this parent process.
         *
         *
         * \param   plist   List of processes.
         * \param   parent  The parent.
         * \param   output  The output.
         * \param   indent  The indent.
         */
        private static void GetProcessAndChildren(System.Diagnostics.Process[] plist, System.Diagnostics.Process parent, List<System.Diagnostics.Process> output, int indent)
        {
            foreach (System.Diagnostics.Process p in plist)
            {
                try
                {                    
                    var par = Utilities.ParentProcessUtilities.GetParentProcess(p.Id);
                    if (par == null) continue;
                    if (par.Id == parent.Id)
                    {
                        GetProcessAndChildren(plist, p, output, indent + 1);
                    }
                }
                catch ( Exception ex )
                {
                    Debug.WriteLine(ex.Message);
                    //maybe access was denied, skip
                    continue;
                }
            }
            output.Add(parent);
        }

        #endregion
    }
}
