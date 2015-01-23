/**
 * \file    LevenshteinDistanceExtensions.cs
 *
 * \brief   Implements the levenshtein distance extensions class.
 */

using System;
using System.Linq;

/**
 * \namespace   RTextNppPlugin.RTextEditor.Utilities
 *
 */
namespace RTextNppPlugin.Utilities
{
    public static class ContainsWithIgnoreCase
    {
        public static bool Contains(this string source, string toCheck, StringComparison comp)
        {
            return source.IndexOf(toCheck, comp) >= 0;
        }
    }

    /**
     * \class   LevenshteinDistanceExtensions
     *
     * \brief   Levenshtein distance string extensions.
     *
     */
    public static class LevenshteinDistanceExtensions
    {
        /**
         *      bool caseSensitive = false)
         *
         * \brief   Levenshtein Distance algorithm with transposition. <br />
         *          A value of 1 or 2 is okay, 3 is iffy and greater than 4 is a poor match.
         *
         *
         * \param   input            the input string
         * \param   comparedTo       the string to compare the input with
         * \param   caseSensitive    whether the matching should be case sensitive
         *
         * \return  The number of edits need to be done in the compared string, in order to match the input string.
         */
        public static int LevenshteinDistance(this string input, string comparedTo, bool caseSensitive = false)
        {
            if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(comparedTo)) return -1;
            if (!caseSensitive)
            {
                input = input.ToLower();
                comparedTo = comparedTo.ToLower();
            }
            int inputLen = input.Length;
            int comparedToLen = comparedTo.Length;

            int[,] matrix = new int[inputLen, comparedToLen];

            //initialize
            for (int i = 0; i < inputLen; i++) matrix[i, 0] = i;
            for (int i = 0; i < comparedToLen; i++) matrix[0, i] = i;

            //analyze
            for (int i = 1; i < inputLen; i++)
            {
                var si = input[i - 1];
                for (int j = 1; j < comparedToLen; j++)
                {
                    var tj = comparedTo[j - 1];
                    int cost = (si == tj) ? 0 : 1;

                    var above = matrix[i - 1, j];
                    var left = matrix[i, j - 1];
                    var diag = matrix[i - 1, j - 1];
                    var cell = FindMinimum(above + 1, left + 1, diag + cost);

                    //transposition
                    if (i > 1 && j > 1)
                    {
                        var trans = matrix[i - 2, j - 2] + 1;
                        if (input[i - 2] != comparedTo[j - 1]) trans++;
                        if (input[i - 1] != comparedTo[j - 2]) trans++;
                        if (cell > trans) cell = trans;
                    }
                    matrix[i, j] = cell;
                }
            }
            return matrix[inputLen - 1, comparedToLen - 1];
        }

        /**
         *
         * \brief   Searches for the first minimum.
         *
         *
         * \param   p   A variable-length parameters list containing p.
         *
         * \return  The found minimum.
         */
        private static int FindMinimum(params int[] p)
        {
            if (null == p) return int.MinValue;
            int min = int.MaxValue;
            for (int i = 0; i < p.Length; i++)
            {
                if (min > p[i]) min = p[i];
            }
            return min;
        }
    }

    /**
     * \class   DiceCoefficientExtensions
     *
     * \brief   Dice coefficient string extension.
     *
     */
    public static class DiceCoefficientExtensions
    {
        /**
         *
         * \brief   Dice Coefficient based on bigrams. <br />
         *          A good value would be 0.33 or above, a value under 0.2 is not a good match, from 0.2
         *          to 0.33 is iffy.
         *
         *
         * \param   input       The input string.
         * \param   comparedTo  The string to compare the input with.
         *
         * \return  Dice's coefficient.
         */
        public static double DiceCoefficient(this string input, string comparedTo)
        {
            var ngrams = input.ToBiGrams();
            var compareToNgrams = comparedTo.ToBiGrams();
            return ngrams.DiceCoefficient(compareToNgrams);
        }

        /**
         *
         * \brief   Dice Coefficient used to compare nGrams arrays produced in advance.
         *
         *
         * \param   nGrams          .
         * \param   compareToNGrams .
         *
         * \return  .
         */
        public static double DiceCoefficient(this string[] nGrams, string[] compareToNGrams)
        {
            int matches = 0;
            foreach (var nGram in nGrams)
            {
                if (compareToNGrams.Any(x => x == nGram)) matches++;
            }
            if (matches == 0) return 0.0d;
            double totalBigrams = nGrams.Length + compareToNGrams.Length;
            return (2 * matches) / totalBigrams;
        }

        /**
         *
         * \brief   A string extension method that converts an input to a bi grams.
         *
         *
         * \param   input   The input to act on.
         *
         * \return  input as a string[].
         */
        public static string[] ToBiGrams(this string input)
        {
            // nLength == 2
            //   from Jackson, return %j ja ac ck ks so on n#
            //   from Main, return #m ma ai in n#
            input = SinglePercent + input + SinglePound;
            return ToNGrams(input, 2);
        }

        /**
         *
         * \brief   A string extension method that converts an input to a triangle grams.
         *
         *
         * \param   input   The input to act on.
         *
         * \return  input as a string[].
         */
        public static string[] ToTriGrams(this string input)
        {
            // nLength == 3
            //   from Jackson, return %%j %ja jac ack cks kso son on# n##
            //   from Main, return ##m #ma mai ain in# n##
            input = DoublePercent + input + DoublePount;
            return ToNGrams(input, 3);
        }

        /**
         *
         * \brief   Converts this object to a n grams.
         *
         *
         * \param   input   The input.
         * \param   nLength The length.
         *
         * \return  The given data converted to a string[].
         */
        private static string[] ToNGrams(string input, int nLength)
        {
            int itemsCount = input.Length - 1;
            string[] ngrams = new string[input.Length - 1];
            for (int i = 0; i < itemsCount; i++) ngrams[i] = input.Substring(i, nLength);
            return ngrams;
        }

        private const string SinglePercent = "%";
        private const string SinglePound = "#";
        private const string DoublePercent = "&&";
        private const string DoublePount = "##";
    }
}
