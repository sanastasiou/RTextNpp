﻿using System;
using System.Linq;
using System.Windows;
using System.Windows.Media;

namespace ESRLabs.RTextEditor.Utilities
{
    public class Visual
    {
        /**
         * @fn  public static T FindVisualParent<T>(DependencyObject child) where T : DependencyObject
         *
         * @brief   Finds the parent of this item.
         *
         * @author  Stefanos Anastasiou
         * @date    10.03.2013
         *
         * @tparam  T   Generic type parameter.
         * @param   child   The child.
         *
         * @return  The found visual parent&lt; t&gt;
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
         * @fn  public static string[] GetEnumValues<T>(bool includeBlank)
         *
         * @brief   Gets an array of strings based on enums, with an optional black entry.
         *
         * @author  Stefanos Anastasiou
         * @date    10.03.2013
         *
         * @tparam  T   Generic type parameter.
         * @param   includeBlank    true to include, false to exclude the blank.
         *
         * @return  The enum values&lt; t&gt;
         */
        public static string[] GetEnumValues<T>(bool includeBlank = false, params T[] skipEnums)
        {
            var values = (from val in (Enum.GetValues(typeof(T)) as T[])
                          where !skipEnums.Contains(val)
                          select val.ToString()).ToList<string>();
            //List<string> values = new List<string>((Enum.GetValues(typeof(T)) as T[]).Select( t => t.ToString()));

            if (includeBlank)
            {
                values.Insert(0, string.Empty);
            }

            return values.ToArray();
        }
    }
}