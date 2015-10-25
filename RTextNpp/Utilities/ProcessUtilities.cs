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
        [System.Diagnostics.CodeAnalysis.ExcludeFromCodeCoverage]
        internal static void KillAllProcessesSpawnedBy(int pid)
        {
            try
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
                Process proc = Process.GetProcessById(pid);
                if (!proc.HasExited)
                {
                    proc.Kill();
                }
            }
            catch
            {
                // Process already exited, or doesn't exist
            }
        }
    }
}