using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media.Imaging;
using ATL;
using Cajonic.Services;
using ProtoBuf;

namespace Cajonic.Model
{
    [ProtoContract]
    public class Album : IEquatable<Album>, IComparable<Album>
    {
        public BitmapImage Artwork => mByteArtwork != null ? BitmapHelper.LoadImage(mByteArtwork) : null;

        private byte[] mByteArtwork;

        public Album()
        {
            Title = string.Empty;
            ArtistName = string.Empty;
            mByteArtwork = null;
            mByteArtwork = null;
        }

        public Album(Song song)
        {
            Title = song.AlbumTitle;
            ArtistName = song.ArtistName;
            mByteArtwork = song.ByteArtwork;
            AlbumSongCollection.Add(song);
        }

        public Album(Track track)
        {
            Title = track.Album;
            ArtistName = track.Artist;
        }

        [ProtoMember(2)]
        public string Title { get; set; }
        [ProtoMember(3)]
        public string ArtistName { get; set; }
        [ProtoMember(4)]
        public List<Song> AlbumSongCollection { get; set; } = new List<Song>();

        public int CompareTo(Album other)
        {
            int result = string.Compare(ArtistName, other?.ArtistName, StringComparison.Ordinal);
            if (result != 0)
            {
                return result;
            }

            result = string.Compare(Title, other?.Title, StringComparison.Ordinal);
            if (result != 0)
            {
                return result;
            }

            if (other?.AlbumSongCollection == null || !other.AlbumSongCollection.Any())
            {
                return result;
            }

            result += AlbumSongCollection.Sum(thisAlbums => other.AlbumSongCollection.Sum(thisAlbums.CompareTo));

            return result;
        }

        public bool Equals(Album other) => other?.Title == Title && other?.ArtistName == ArtistName;
    }
}
