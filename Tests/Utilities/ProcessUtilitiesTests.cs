using System;
namespace Tests.Utilities
{
    using System.Diagnostics;
    using NUnit.Framework;
    using RTextNppPlugin.Utilities;
    [TestFixture]
    class ProcessUtilitiesTests
    {
        bool _isExited = false;
        [Test]
        public void KillProcessTest()
        {
            ProcessStartInfo aProcessStartInfo = new ProcessStartInfo("cmd.exe");
            aProcessStartInfo.CreateNoWindow = true;
            aProcessStartInfo.RedirectStandardError = true;
            aProcessStartInfo.RedirectStandardOutput = true;
            aProcessStartInfo.UseShellExecute = false;
            var aProcess = new System.Diagnostics.Process();
            aProcess.StartInfo = aProcessStartInfo;
            aProcess.Exited += _process_Exited;
            aProcess.Start();
            ProcessUtilities.KillAllProcessesSpawnedBy(aProcess.Id);
            while (!aProcess.HasExited) ;
            Assert.IsTrue(_isExited);
        }
        void _process_Exited(object sender, EventArgs e)
        {
            _isExited = true;
        }
        [Test]
        public void KillMultipleProcessTest()
        {
            ProcessStartInfo aProcessStartInfo = new ProcessStartInfo("cmd.exe");
            aProcessStartInfo.CreateNoWindow = true;
            aProcessStartInfo.RedirectStandardError = true;
            aProcessStartInfo.RedirectStandardOutput = true;
            aProcessStartInfo.UseShellExecute = false;
            var aProcess = new System.Diagnostics.Process();
            aProcess.StartInfo = aProcessStartInfo;
            aProcess.Exited += _process_Exited;
            aProcess.Start();
            ProcessStartInfo aProcessStartInfo2 = new ProcessStartInfo("cmd.exe", "calc");
            aProcessStartInfo2.CreateNoWindow = true;
            aProcessStartInfo2.RedirectStandardError = true;
            aProcessStartInfo2.RedirectStandardOutput = true;
            aProcessStartInfo2.UseShellExecute = false;
            var aProcess2 = new System.Diagnostics.Process();
            aProcess2.StartInfo = aProcessStartInfo;
            aProcess2.Exited += _process_Exited;
            aProcess2.Start();
            ProcessUtilities.KillAllProcessesSpawnedBy(aProcess.Id);
            while (!aProcess.HasExited) ;
            Assert.IsTrue(_isExited);
            _isExited = false;
            ProcessUtilities.KillAllProcessesSpawnedBy(aProcess2.Id);
            while (!aProcess2.HasExited) ;
            Assert.IsTrue(_isExited);
        }
        [Test]
        public void FalseProcessIdTest()
        {
            //expect no crash
            ProcessUtilities.KillAllProcessesSpawnedBy(-1);
        }
        void _process_Exited2(object sender, EventArgs e)
        {
            _isExited = true;
        }
    }
}
