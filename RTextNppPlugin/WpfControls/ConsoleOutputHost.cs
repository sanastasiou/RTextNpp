using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using RTextNppPlugin.WpfControls;
using RTextNppPlugin.ViewModels;

namespace RTextNppPlugin.WpfControls
{
    /**
     * A console output element host.
     *
     * \tparam  T   Generic type parameter. The WPF control.
     * \tparam  U   Generic type parameter. The WPF control view model.
     */
    [Designer("System.Windows.Forms.Design.ControlDesigner, System.Design")]
    [DesignerSerializer("System.ComponentModel.Design.Serialization.TypeCodeDomSerializer , System.Design", "System.ComponentModel.Design.Serialization.CodeDomSerializer, System.Design")]
    class ConsoleOutputElementHost<T, U> : System.Windows.Forms.Integration.ElementHost where T : System.Windows.Controls.UserControl, new()
                                                                                        where U : IConsoleViewModel
    {
        private T _consoleOutput = new T();
        private U _viewModel = default(U);

        public ConsoleOutputElementHost()
        {
            base.Child = _consoleOutput;
            _viewModel = (U)_consoleOutput.DataContext;
        }

        public void addWorkspace(string workspace)
        {
            _viewModel.addWorkspace(workspace);
        }

        public void removeWorkspace(string workspace)
        {
            _viewModel.removeWorkspace(workspace);
        }
    } 

}
