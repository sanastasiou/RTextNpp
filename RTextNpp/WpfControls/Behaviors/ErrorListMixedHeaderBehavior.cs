using System;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows.Interactivity;

namespace RTextNppPlugin.WpfControls.Behaviors
{
    class ErrorListMixedHeaderBehavior : Behavior<DataGrid>
    {
        private static readonly DependencyPropertyDescriptor Descriptor;

        static ErrorListMixedHeaderBehavior()
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
            AssociatedObject.Columns[2].Width = new DataGridLength(1, DataGridLengthUnitType.SizeToHeader);
            AssociatedObject.Columns[1].Width = new DataGridLength(1, DataGridLengthUnitType.SizeToCells);
            AssociatedObject.Columns[0].Width = new DataGridLength(1, DataGridLengthUnitType.SizeToCells);
        }
    }
}
