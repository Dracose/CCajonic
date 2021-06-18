using System.Collections.Generic;
using System.Linq;
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

        public static void ReplaceRangeArtists(this ICollection<Artist> list, IEnumerable<Artist> toAdd)
        {
            foreach (Artist element in toAdd.Where(x => x.IsSerialization))
            {
                if (list.Any(x => x.Name == element.Name))
                {
                    list.Remove(list.FirstOrDefault(x => x.Name == element.Name));
                    list.Add(element);
                }
                else
                {
                    list.Add(element);
                }
            }
        }
    }
}
