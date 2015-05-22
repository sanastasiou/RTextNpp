using System.Diagnostics.CodeAnalysis;
using System.Windows;

namespace RTextNppPlugin.Utilities
{
    /**
     * \brief   A binding proxy. This class is just used, so that a Data context that is usually not being inherited
     *          through the visual tree, can be accessed from children elements of the element which hold a reference to said DataContext.
     *          <a href="http://www.thomaslevesque.com/2011/03/21/wpf-how-to-bind-to-data-when-the-datacontext-is-not-inherited/">How to bind to data when the data context is not inherited. </a>
     */
    public class BindingProxy : Freezable
    {
        #region Overrides of Freezable

        [ExcludeFromCodeCoverage]
        protected override Freezable CreateInstanceCore()
        {
            return new BindingProxy();
        }

        #endregion

        public object Data
        {
            get { return (object)GetValue(DataProperty); }
            set { SetValue(DataProperty, value); }
        }

        // Using a DependencyProperty as the backing store for Data.  This enables animation, styling, binding, etc...
        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register("Data", typeof(object), typeof(BindingProxy), new UIPropertyMetadata(null));
    }
}
