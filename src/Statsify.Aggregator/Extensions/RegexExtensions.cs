using System.Text.RegularExpressions;

namespace Statsify.Aggregator.Extensions
{
    public static class RegexExtensions
    {
        public static string RegexReplace(this string s, Regex regex, string replacement)
        {
            return regex.Replace(s, replacement);
        }
    }
}