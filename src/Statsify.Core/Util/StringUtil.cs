namespace Statsify.Core.Util
{
    internal static class StringExtensions
    {
        public static string SubstringBefore(this string s, string substring)
        {
            var substringOffset = s.IndexOf(substring);

            return substringOffset == -1 ?
                s :
                s.Substring(0, substringOffset);
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
            var substringOffset = s.IndexOf(substring);

            return substringOffset == -1 ?
                @default :
                s.Substring(0, substringOffset);
        }

        public static string SubstringBeforeSuffix(this string s, string substring)
        {
            if (!s.EndsWithSuffix(substring)) return s;

            var substringOffset = s.LastIndexOf(substring);

            return substringOffset == -1 ?
                s :
                s.Substring(0, substringOffset);
        }

        public static string SubstringAfter(this string s, string substring)
        {
            var start = s.IndexOf(substring);

            return start == -1 ?
                s :
                s.Substring(start + substring.Length);
        }

        public static string SubstringAfterLast(this string s, string substring)
        {
            var start = s.LastIndexOf(substring);

            return start == -1 ?
                s :
                s.Substring(start + substring.Length);
        }

        public static string SubstringAfterLast(this string s, string substring, string @default)
        {
            var start = s.LastIndexOf(substring);

            return start == -1 ?
                @default :
                s.Substring(start + substring.Length);
        }

        public static string SubstringBetween(this string s, string start, string end)
        {
            return s.SubstringAfter(start).SubstringBefore(end);
        }

        public static bool EndsWithSuffix(this string s, string suffix)
        {
            return s.Length > suffix.Length && s.EndsWith(suffix);
        }
    }
}
