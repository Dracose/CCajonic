using System;
using System.Windows.Media.Imaging;

namespace Cajonic.Model
{
    [Serializable]
    public class Album : IEquatable<Album>
    {
        public string Title { get; set; }
        public string ArtistName { get; set; }
        public BitmapImage Artwork { get; set; }
        public SongCollection AlbumCollection { get; set; }

        public bool Equals(Album other) => other?.Title == Title && other?.ArtistName == ArtistName;
    }
}
