using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows.Media.Imaging;

namespace Cajonic.Model
{
    [Serializable]
    public class Artist : IEquatable<Artist>
    {
        public string Name { get; set; }
        public BitmapImage ProfileImage { get; set; }
        public SongCollection ArtistCollection { get; set; }

        public bool Equals([AllowNull] Artist other) => other?.Name == Name;
    }
}
