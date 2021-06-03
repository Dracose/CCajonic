using System.Collections.Generic;
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

        public static IEnumerable<T> OrderBy<T>(this ConcurrentObservableCollection<T> enumerable, string property)
        {
            return enumerable.OrderBy(x => GetProperty(x, property));
        }
        
        public static IEnumerable<T> OrderByDescending<T>(this ConcurrentObservableCollection<T> enumerable, string property)
        {
            return enumerable.OrderByDescending(x => GetProperty(x, property));
        }
	
        private static object GetProperty(object o, string propertyName)
        {
            return o.GetType().GetProperty(propertyName)?.GetValue(o, null);
        }
    }
}
