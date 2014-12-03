using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using RTextNppPlugin.WpfControls;

namespace RTextNppPlugin.WpfControls
{
    [Designer("System.Windows.Forms.Design.ControlDesigner, System.Design")]
    [DesignerSerializer("System.ComponentModel.Design.Serialization.TypeCodeDomSerializer , System.Design", "System.ComponentModel.Design.Serialization.CodeDomSerializer, System.Design")]
    public class ConsoleOutputElementHost : System.Windows.Forms.Integration.ElementHost
    {
        private ConsoleOutput _consoleOutput = new ConsoleOutput();

        public ConsoleOutputElementHost()
        {
            base.Child = _consoleOutput;
        }
    } 

}
