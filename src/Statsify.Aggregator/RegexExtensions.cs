using System.Text.RegularExpressions;

namespace Statsify.Aggregator
{
    public static class RegexExtensions
    {
        public static string RegexReplace(this string s, string regex, string replacement)
        {
            var r = new Regex(regex);
            return r.Replace(s, replacement);
        }
    }
}