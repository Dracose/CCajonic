using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Cajonic.Services
{
    public static class ObservableCollectionExtension
    {
        public static void AddUniqueRange<Song>(this ObservableCollection<Song> collection, IEnumerable<Song> songs)
        {
            foreach (Song song in songs)
            {
                if (!collection.Contains(song))
                {
                    collection.Add(song);
                }
            }
        }
    }
}
