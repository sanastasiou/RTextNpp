using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Management;

namespace RTextNppPlugin.Utilities
{
    internal static class ProcessUtilities
    {
        /**
         *
         * \brief   Kill all processes spawned by a parent process.
         *
         *
         * \param   parentProcessId Identifier for the parent process.
         */
        internal static void KillAllProcessesSpawnedBy(int pid)
        {
            // NOTE: Process Ids are reused!
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(
                "SELECT * " +
                "FROM Win32_Process " +
                "WHERE ParentProcessId=" + pid);
            ManagementObjectCollection collection = searcher.Get();

            foreach (ManagementObject mo in collection)
            {
                KillAllProcessesSpawnedBy(Convert.ToInt32(mo["ProcessID"]));
            }
            try
            {
                Process proc = Process.GetProcessById(pid);
                if (!proc.HasExited)
                {
                    proc.Kill();
                }
            }
            catch (ArgumentException)
            {
                // Process already exited.
            }
        }
    }
}
