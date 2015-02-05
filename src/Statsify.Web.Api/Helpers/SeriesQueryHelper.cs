namespace Statsify.Web.Api.Helpers
{
    using System;

    internal class SeriesQueryHelper
    {
        public static string ExtractSeriesListExpressionFromQuery(string query)
        {
            if (String.IsNullOrWhiteSpace(query))
                return query;

            var start = query.LastIndexOf('(') + 1;

            query = query.Substring(start);

            var bracketIndex = query.IndexOf(')');
            var commaIndex = query.IndexOf(',');

            var end = 0;

            if (bracketIndex > 0 && commaIndex > 0)
                end = Math.Min(bracketIndex, commaIndex);

            if (bracketIndex > 0 && commaIndex < 0)
                end = bracketIndex;

            if (bracketIndex < 0 && commaIndex > 0)
                end = commaIndex;

            if (end > 0)
                query = query.Substring(0, end);

            return query.Trim();
        }

        public static string ReplaceSeriesListExpression(string query, string expressionFormat)
        {
            if (String.IsNullOrWhiteSpace(query))
                return query;

            var start = query.LastIndexOf('(') + 1;

            var bracketIndex = query.IndexOf(')');
            var commaIndex = query.IndexOf(',');

            var end = 0;

            if (bracketIndex > 0 && commaIndex > 0)
                end = Math.Min(bracketIndex, commaIndex);

            if (bracketIndex > 0 && commaIndex < 0)
                end = bracketIndex;

            if (bracketIndex < 0 && commaIndex > 0)
                end = commaIndex;

            if (bracketIndex < 0 && commaIndex < 0)
                end = query.Length;

            var left = query.Substring(0, start);

            var expression = query.Substring(start, end - start);

            var right = query.Substring(end, query.Length - end);

            return left + String.Format(expressionFormat, expression) + right;
        }
    }
}
