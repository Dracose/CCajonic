using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace Cajonic.Services
{
    public static class ObservableCollectionExtension
    {
        public static void AddUniqueRange<TSong>(this ObservableCollection<TSong> collection, IEnumerable<TSong> songs)
        {
            foreach (TSong song in songs)
            {
                if (!collection.Contains(song))
                {
                    collection.Add(song);
                }
            }
        }
    }
}
