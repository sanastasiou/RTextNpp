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

            AssociatedObject.Columns.CollectionChanged += OnColumnsCollectionChanged;

            foreach (var column in AssociatedObject.Columns)
            {
                AddListener(column);
            }
        }

        void OnColumnsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            switch (e.Action)
            {
                case NotifyCollectionChangedAction.Add:
                    foreach (var column in e.NewItems.OfType<DataGridColumn>())
                    {
                        AddListener(column);
                    }
                    break;
                case NotifyCollectionChangedAction.Remove:
                    foreach (var column in e.OldItems.OfType<DataGridColumn>())
                    {
                        RemoveListener(column);
                    }
                    break;
                case NotifyCollectionChangedAction.Replace:
                    foreach (var column in e.NewItems.OfType<DataGridColumn>())
                    {
                        AddListener(column);
                    }
                    foreach (var column in e.OldItems.OfType<DataGridColumn>())
                    {
                        RemoveListener(column);
                    }
                    break;
            }
        }

        protected override void OnDetaching()
        {
            base.OnDetaching();

            foreach (var column in AssociatedObject.Columns)
            {
                RemoveListener(column);
            }
        }

        private void AddListener(DataGridColumn column)
        {
            Descriptor.AddValueChanged(column, ResizeGrid);
        }

        private void RemoveListener(DataGridColumn column)
        {
            Descriptor.RemoveValueChanged(column, ResizeGrid);
        }

        private void ResizeGrid(object sender, EventArgs e)
        {
            //foreach (var column in AssociatedObject.Columns)
            //{
            //    column.MinWidth = 10;
            //    column.Width = new DataGridLength(1, DataGridLengthUnitType.SizeToCells);
            //}

            //caclulate min possible width which fits all displayed objects
            var columnsWidth = AssociatedObject.Columns.Sum(c => c.ActualWidth);
            AssociatedObject.MaxWidth = columnsWidth + 2;
            AssociatedObject.InvalidateMeasure();
        }
    }

}
