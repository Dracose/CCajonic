using System;
using Meziantou.Framework.WPF.Collections;

namespace Cajonic.Model
{
    public class Playlist : IEquatable<Playlist>
    {
        public string Name { get; set; }
        public ConcurrentObservableCollection<Song> PlaylistCollection { get; set; }

        public bool Equals(Playlist other) => other?.Name == Name;
    }
}
