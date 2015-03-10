using System;
using System.Runtime.InteropServices;

namespace RTextNppPlugin.Utilities
{
    /**
     * \struct  ParentProcessUtilities
     *
     * \brief   A utility class to determine a process parent.
     *
     */
    [StructLayout(LayoutKind.Sequential)]
    public struct ParentProcessUtilities
    {
        // These members must match PROCESS_BASIC_INFORMATION
        internal IntPtr Reserved1;
        internal IntPtr PebBaseAddress;
        internal IntPtr Reserved2_0;
        internal IntPtr Reserved2_1;
        internal IntPtr UniqueProcessId;
        internal IntPtr InheritedFromUniqueProcessId;

        /**
         *      int processInformationClass, ref ParentProcessUtilities processInformation,
         *      int processInformationLength, out int returnLength);
         *
         * \brief   NT query information process.
         *
         *
         * \param   processHandle                   Handle of the process.
         * \param   processInformationClass         The process information class.
         * \param   [in,out]  processInformation    Information describing the process.
         * \param   processInformationLength        Length of the process information.
         * \param   [out] returnLength              Length of the return.
         *
         * \return  .
         */
        [DllImport("ntdll.dll")]
        private static extern int NtQueryInformationProcess(IntPtr processHandle, int processInformationClass, ref ParentProcessUtilities processInformation, int processInformationLength, out int returnLength);

        /**
         * \enum    ProcessAccessFlags
         *
         * \brief   Bitfield of flags for specifying ProcessAccessFlags.
         */
        [Flags]
        enum ProcessAccessFlags : uint
        {
            PROCESS_QUERY_LIMITED_INFORMATION = 0x1000
        }

        /**
         *      [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);
         *
         * \brief   Opens the process.
         *
         *
         * \param   dwDesiredAccess The desired access.
         * \param   bInheritHandle  Whether this process should be inherited.
         * \param   dwProcessId     Identifier for the process.
         *
         * \return  .
         */
        [DllImport("kernel32.dll")]
        static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);

        /**
         *
         * \brief   Gets the parent process of the current process.
         *
         *
         * \return  An instance of the Process class.
         */
        public static System.Diagnostics.Process GetParentProcess()
        {
            return GetParentProcess( System.Diagnostics.Process.GetCurrentProcess().Handle);
        }

        /**
         *
         * \brief   Gets the parent process of specified process.
         *
         *
         * \param   id  The process id.
         *
         * \return  An instance of the Process class.
         */
        public static System.Diagnostics.Process GetParentProcess(int id)
        {
            try
            {
                System.Diagnostics.Process process = System.Diagnostics.Process.GetProcessById(id);
                var handle = OpenProcess(ProcessAccessFlags.PROCESS_QUERY_LIMITED_INFORMATION, false, process.Id);                
                return GetParentProcess(handle);
            }
            catch(Exception ex)
            {
                Logging.Logger.Instance.Append("GetParentProcess(int id) exception : {0}", ex.Message);
                return null;
            }
        }

        /**
         *
         * \brief   Gets the parent process of a specified process.
         *
         *
         * \exception   Win32Exception  Thrown when a window 32 error condition occurs. It is handled internally.
         * \exception   ArgumentException is thrown when the handle property cannot be accessed. This is a bug in the System.Diagnostics.Process class.
         *
         * \param   handle  The process handle.
         *
         * \return  An instance of the Process class.
         */
        public static System.Diagnostics.Process GetParentProcess(IntPtr handle)
        {
            ParentProcessUtilities pbi = new ParentProcessUtilities();
            int returnLength;
            int status = NtQueryInformationProcess(handle, 0, ref pbi, Marshal.SizeOf(pbi), out returnLength);

            try
            {
                if (status != 0)
                {
                    return null;
                }
                var aProcesses = System.Diagnostics.Process.GetProcesses();
                foreach (var p in aProcesses)
                {
                    if (p.Id == pbi.InheritedFromUniqueProcessId.ToInt32())
                    {
                        return p;
                    }
                }
                return null;
            }
            catch (ArgumentException)
            {
                // not found
                return null;
            }
        }
    }
}
