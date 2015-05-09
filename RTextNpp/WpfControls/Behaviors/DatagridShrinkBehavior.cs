using System;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Interactivity;

namespace RTextNppPlugin.WpfControls.Behaviors
{
    public class UpdateWidthOnColumnResizedBehavior : Behavior<DataGrid>
    {
        private static readonly DependencyPropertyDescriptor Descriptor;

        static UpdateWidthOnColumnResizedBehavior()
        {
            Descriptor = DependencyPropertyDescriptor.FromProperty(DataGridColumn.ActualWidthProperty, typeof(DataGridColumn));
        }

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.IsVisibleChanged += AssociatedObject_IsVisibleChanged;
        }

        void AssociatedObject_IsVisibleChanged(object sender, System.Windows.DependencyPropertyChangedEventArgs e)
        {
            ResizeGrid(sender, new EventArgs());
        }        

        protected override void OnDetaching()
        {
            base.OnDetaching();
        }

        private void ResizeGrid(object sender, EventArgs e)
        {
            foreach (var column in AssociatedObject.Columns)
            {
                column.Width = new DataGridLength(1, DataGridLengthUnitType.SizeToCells);
            }
        }
    }

}
