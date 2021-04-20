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
                bag.Add(element);
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

        public static void AddUnique<T>(this ConcurrentBag<T> bag, T toAdd)
        {
            if (!bag.Contains(toAdd))
            {
                bag.Add(toAdd);
            }
        }
    }
}
