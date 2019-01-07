using System.Collections.Generic;

namespace PhotoLabel.Extensions
{
    public static class List
    {
        public static void AddRange<T>(this IList<T> list, IEnumerable<T> items)
        {
            foreach (var value in items)
            {
                list.Add(value);
            }
        }
    }
}