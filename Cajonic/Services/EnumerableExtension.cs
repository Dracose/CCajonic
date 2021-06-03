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

        public static ICollection<Artist> ReplaceRangeArtists(this ICollection<Artist> list, IEnumerable<Artist> toAdd)
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
            return list;
        }

        public static void AddUnique<T>(this List<T> list, T toAdd)
        {
            if (!list.Contains(toAdd))
            {
                list.Add(toAdd);
            }
        }

        public static void AddUniqueArtist(this List<Artist> list, Artist toAdd)
        {
            List<string> titles = list.SelectMany(x => x.ArtistAlbums).Select(z => z.Value.Title).ToList();
            if (!list.Contains(toAdd) && !titles.Contains(toAdd.ArtistAlbums.SelectMany(x => x.Value.Title)))
            {
                list.Add(toAdd);
            }
        }
    }
}
