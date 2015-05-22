using System.ComponentModel;
using System.ComponentModel.Design.Serialization;
using RTextNppPlugin.RText;

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
    class ElementHost<T, U> : System.Windows.Forms.Integration.ElementHost where T : System.Windows.Controls.UserControl
                                                                           where U : BindableObject
    {
        private T _wpfControl;
        private U _viewModel = default(U);

        public ElementHost(T control)
        {
            _wpfControl = control;
            base.Child  = _wpfControl;
            _viewModel  = (U)_wpfControl.DataContext;
        }
    } 

}
