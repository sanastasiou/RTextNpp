using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RTextNppPlugin.RText.StateEngine
{
    public enum Command
    {
        Connect,
        Connected,
        Execute,
        ExecuteFinished,
        LoadModel,
        Disconnected
    }
}
