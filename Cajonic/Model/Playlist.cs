using System;
using System.Collections.Generic;
using System.Text;

namespace Cajonic.Model
{
    public class Playlist : IEquatable<Playlist>
    {
        public string Name { get; set; }
        public SongCollection PlaylistCollection { get; set; }

        public bool Equals(Playlist other) => other?.Name == Name;
    }
}
