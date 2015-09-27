using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Statsify.Core.Util;

namespace Statsify.Core
{
    public class MetricNameSelectorParser
    {
        private static readonly Regex RangeRegex = new Regex(@"([^-])-([^-])");

        private readonly StringComparison stringComparison;

        public MetricNameSelectorParser(StringComparison stringComparison)
        {
            this.stringComparison = stringComparison;
        }

        public Predicate<string>[] Parse(string metricNameSelector)
        {
            if(metricNameSelector == null) throw new ArgumentNullException("metricNameSelector");

            return ParseImpl(metricNameSelector).ToArray();
        }

        private IEnumerable<Predicate<string>> ParseImpl(string metricNameSelector)
        {
            if(metricNameSelector == "")
            {
                yield return s => false;
                yield break;
            } // if

            var fragments = metricNameSelector.Split('.');
            foreach(var fragment in fragments)
                yield return ParseFragment(fragment);
        }

        private Predicate<string> ParseFragment(string fragment)
        {
            if(fragment == "*") return s => true;

            if(fragment.Contains("*"))
            {
                var prefix = fragment.SubstringBefore("*", "");
                var suffix = fragment.SubstringAfter("*", "");

                return s => {
                    if(!string.IsNullOrWhiteSpace(prefix) && !s.StartsWith(prefix, stringComparison)) return false;
                    if(!string.IsNullOrWhiteSpace(suffix) && !s.EndsWith(suffix, stringComparison)) return false;

                    return true;
                };
            } // if

            if(fragment.Contains("{") && fragment.Contains("}"))
            {
                var prefix = fragment.SubstringBefore("{", "");
                var suffix = fragment.SubstringAfterLast("}", "");

                var values = 
                    fragment.
                        SubstringBetween("{", "}").
                        Split(new [] { ',' }, StringSplitOptions.RemoveEmptyEntries).
                        Select(s => s.Trim()).
                        ToArray();

                return s => {
                    var i = 0;

                    if(!string.IsNullOrWhiteSpace(prefix))
                    {
                        if(!s.StartsWith(prefix, stringComparison)) return false;
                        i += prefix.Length;
                    } // if

                    var value = values.FirstOrDefault(v => s.IndexOf(v, i, stringComparison) == i);
                    if(string.IsNullOrWhiteSpace(value)) return false;

                    i += value.Length;

                    if(!string.IsNullOrWhiteSpace(suffix))
                    {
                        if(s.IndexOf(suffix, i, stringComparison) != i) return false;
                        i += suffix.Length;
                    } // if

                    return i == s.Length;
                };
            } // if

            if(fragment.Contains("[") && fragment.Contains("]"))
            {
                var prefix = fragment.SubstringBefore("[");
                var suffix = fragment.SubstringAfterLast("]");

                var ranges = fragment.SubstringBetween("[", "]");

                return s => {
                    var i = 0;

                    if(!string.IsNullOrWhiteSpace(prefix))
                    {
                        if(!s.StartsWith(prefix, stringComparison)) return false;
                        i += prefix.Length;
                    } // if

                    var matched = false;
                    for(var j = 0; j < ranges.Length;)
                    {
                        Match range = null;
                        if(j + 3 > ranges.Length || !(range = RangeRegex.Match(ranges, j, 3)).Success)
                        {
                            if(string.Equals(s[i].ToString(CultureInfo.InvariantCulture), ranges[j].ToString(CultureInfo.InvariantCulture), stringComparison))
                            {
                                matched = true;
                                i++;

                                break;
                            } // if

                            j++;
                        } // if
                        else
                        {
                            var l = (int)range.Value[0];
                            var r = (int)range.Value[2];
                            var c = (int)s[i];

                            if(l <= c && c <= r)
                            {
                                matched = true;
                                i++;

                                break;
                            } // if
                            
                            j += 3;
                        } // else
                    } // for

                    if(!matched) return false;

                    if(!string.IsNullOrWhiteSpace(suffix))
                    {
                        if(s.IndexOf(suffix, i, stringComparison) != i) return false;
                        i += suffix.Length;
                    } // if

                    return i == s.Length;
                };
            } // if

            return s => string.Equals(s, fragment, stringComparison);
        }
    }
}
