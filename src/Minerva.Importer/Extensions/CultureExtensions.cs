using System;
using System.Collections.Generic;
using System.Text;

namespace Minerva.Importer.Extensions
{
    public static class CollectionExtensions
    {
        public static void ForEach<T>(this IEnumerable<T> collection, Action<T> action)
        {
            if (collection == null)
                return;
            foreach (var item in collection)
            {
                action(item);
            }
        }
    }
    public static class CultureExtensions
    {
        public static string ToTitleCase(this string input)
        {
            var tokens = input.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < tokens.Length; i++)
            {
                var token = tokens[i];
                tokens[i] = token.Substring(0, 1).ToUpper() + token.Substring(1);
            }

            return string.Join(" ", tokens);
        }
    }
}
