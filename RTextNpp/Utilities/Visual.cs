using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;
using System.Windows.Interop;

namespace RTextNppPlugin.Utilities
{
    public class VisualUtilities
    {
        public static void SetOwnerFromNppPlugin(System.Windows.Window window)
        {
            WindowInteropHelper helper = new WindowInteropHelper(window);            
            helper.Owner = Plugin.nppData._nppHandle;
        }

        /**
         *
         * \brief   Finds the parent of this item.
         *
         *
         * \tparam  T   Generic type parameter.
         * \param   child   The child.
         *
         * \return  The found visual parent&lt; t&gt;
         */
        public static T FindVisualParent<T>(DependencyObject child ) where T : DependencyObject
        {
            DependencyObject parentObject = VisualTreeHelper.GetParent(child);
            if (parentObject == null) return null;
            T parent = parentObject as T;
            if (parent != null)
                return parent;
            else
                return FindVisualParent<T>(parentObject);
        }

        /**
         *
         * \brief   Gets an array of strings based on enums, with an optional black entry.
         *
         *
         * \tparam  T   Generic type parameter.
         * \param   includeBlank    true to include, false to exclude the blank.
         *
         * \return  The enum values&lt; t&gt;
         */
        public static string[] GetEnumValues<T>(bool includeBlank = false, params T[] skipEnums)
        {
            var values = (from val in (Enum.GetValues(typeof(T)) as T[])
                          where !skipEnums.Contains(val)
                          select val.ToString()).ToList<string>();
            
            if (includeBlank)
            {
                values.Insert(0, string.Empty);
            }

            return values.ToArray();
        }

        public static T GetVisualChild<T>(Visual parent) where T : Visual
        {
            T child = default(T);
            int numVisuals = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < numVisuals; i++)
            {
                Visual v = (Visual)VisualTreeHelper.GetChild(parent, i);
                child = v as T;
                if (child == null)
                {
                    child = GetVisualChild<T>(v);
                }
                if (child != null)
                {
                    break;
                }
            }
            return child;
        } 
    }
}
