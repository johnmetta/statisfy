using System;

namespace Statsify.Agent.Util
{
    internal static class StringExtensions
    {
        public static string SubstringBefore(this string s, string substring)
        {
            return s.SubstringBefore(substring, s);
        }

        /// <summary>
        /// Returns a substring of <paramref name="s"/> until <paramref name="substring"/>
        /// or <paramref name="default"/>, if there's no substring in <paramref name="s"/>.
        /// </summary>
        /// <param name="s"></param>
        /// <param name="substring"></param>
        /// <param name="default"></param>
        /// <returns></returns>
        /// <example>
        /// <code>
        /// "ab" == "abc".SubstringBefore("c", null);
        /// null == "abc".SubstringBefore("d", null);
        /// "xx" == "abc".SubstringBefore("e", "xx");
        /// </code>
        /// </example>
        public static string SubstringBefore(this string s, string substring, string @default)
        {
            var substringOffset = s.IndexOf(substring, StringComparison.Ordinal);

            return substringOffset == -1 ?
                @default :
                s.Substring(0, substringOffset);
        }

        public static string SubstringAfter(this string s, string substring)
        {
            var start = s.IndexOf(substring, StringComparison.Ordinal);

            return start == -1 ?
                s :
                s.Substring(start + substring.Length);
        }

        public static string SubstringBetween(this string s, string start, string end)
        {
            return s.SubstringAfter(start).SubstringBefore(end);
        }

    }
}
