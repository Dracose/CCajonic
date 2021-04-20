using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using Cajonic.Model;

namespace Cajonic.Services
{
    public static class EnumerableExtension
    {
        public static IEnumerable<T> Except<T>(this IEnumerable<T> orgList, IEnumerable<T> toRemove)
        {
            List<T> list = orgList.OrderBy(x => x).ToList();
            foreach (T item in toRemove)
            {
                int index = list.BinarySearch(item);
                if (index >= 0)
                {
                    list.RemoveAt(index);
                }
            }
            return list;
        }

        public static ICollection<T> AddRange<T>(this ICollection<T> list, IEnumerable<T> toAdd)
        {
            foreach (T element in toAdd)
            {
                list.Add(element);
            }

            return list;
        }

        public static void AddUnique<T>(this List<T> list, T toAdd)
        {
            if (!list.Contains(toAdd))
            {
                list.Add(toAdd);
            }
        }
    }
}
