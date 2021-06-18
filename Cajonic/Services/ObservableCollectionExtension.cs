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

        // ReSharper disable CommentTypo
        //Todo : Make "ThenBy" function more flexible (e.g. property = ArtistName; then by albumtitle, tracknumber, etc...)
        public static IEnumerable<T> OrderBy<T>(this ConcurrentObservableCollection<T> enumerable, string property)
        {
            return enumerable.OrderBy(x => GetProperty(x, property)).ThenBy(x => GetProperty(x, "TrackNumber"));
        }
        
        public static IEnumerable<T> OrderByDescending<T>(this ConcurrentObservableCollection<T> enumerable, string property)
        {
            return enumerable.OrderByDescending(x => GetProperty(x, property)).ThenBy(x => GetProperty(x, "TrackNumber"));
        }
	
        private static object GetProperty(object o, string propertyName)
        {
            return o.GetType().GetProperty(propertyName)?.GetValue(o, null);
        }
    }
}
