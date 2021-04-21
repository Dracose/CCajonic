using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cajonic.Model;

namespace Cajonic.Services
{
    public static class ConcurrentBagExtension
    {
        public static void AddRange<T>(this ConcurrentBag<T> bag, IEnumerable<T> toAdd)
        {
            foreach (T element in toAdd)
            {
                if (toAdd != null)
                {
                    bag.Add(element);
                }
            }
        }

        public static ConcurrentBag<Artist> ArtistMerge(this IEnumerable<Artist> artists)
        {
            //Merge multiple artists then merge multiple albums
            return new ConcurrentBag<Artist>(artists.GroupBy(x => x)
                 .Where(g => g.Count() > 1)
                 .Select(y => y.Key)
                 .ToList());
        }

        public static void AddUnique<T>(this ConcurrentDictionary<int, T> dictionary, T toAdd)
        {
            if (!dictionary.Select(x => x.Value).Contains(toAdd))
            {
                dictionary.TryAdd(dictionary.Count, toAdd);
            }
        }

        public static void AddUniqueArtist(this ConcurrentBag<Artist> bag, Artist toAdd)
        {
            if (!bag.Contains(toAdd))
            {
                bag.Add(toAdd);
            }
        }
    }
}
