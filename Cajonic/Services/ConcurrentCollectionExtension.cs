using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Cajonic.Model;

namespace Cajonic.Services
{
    public static class ConcurrentCollectionExtension
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

        public static void AddUniqueArtist(this ConcurrentBag<Artist> bag, Artist toAdd)
        {
            if (!bag.Contains(toAdd))
            {
                bag.Add(toAdd);
            }
        }

        public static void TryAddRange(this ConcurrentDictionary<int, Album> dictionary, IEnumerable<KeyValuePair<int,Album>> toAdd)
        {
            foreach ((int key, Album cd) in toAdd)
            {
                dictionary.TryAdd(key, cd);
            }
        }
    }
}
