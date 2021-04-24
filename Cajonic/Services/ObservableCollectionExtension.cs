using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Meziantou.Framework.WPF.Collections;

namespace Cajonic.Services
{
    public static class ObservableCollectionExtension
    {
        public static void AddUniqueRange<T>(this ConcurrentObservableCollection<T> collection, IEnumerable<T> songs)
        {
            foreach (T song in songs)
            {
                if (!collection.Contains(song))
                {
                    collection.Add(song);
                }
            }
        }

        public static void AddUnique<T>(this ObservableCollection<T> list, T toAdd)
        {
            if (!list.Contains(toAdd))
            {
                list.Add(toAdd);
            }
        }
    }
}
