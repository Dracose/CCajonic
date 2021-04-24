using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Media.Imaging;
using ATL;
using Cajonic.Services;
using ProtoBuf;

namespace Cajonic.Model
{
    [ProtoContract]
    // ReSharper disable once InconsistentNaming
    public class CD : IEquatable<CD>, IComparable<CD>
    {
        public BitmapImage Artwork => mByteArtwork;

        private BitmapImage mByteArtwork
        {
            get
            {
                //if (AlbumSongCollection.Any() && AlbumSongCollection.Values.Select(item => item.ByteArtwork).Where(x => x != null).Distinct().Skip(1).Any())
                //{
                //    return AlbumSongCollection.Values.FirstOrDefault()?.ByteArtwork;
                //}

                return null;
            }
        }

        public CD()
        {
        }

        public CD(Track track)
        {
            Title = track.Album;
            ArtistName = track.Artist;
            DiscNumber = track.DiscNumber;
        }

        public CD(Song song)
        {
            Title = song.AlbumTitle;
            ArtistName = song.ArtistName;
            if (song.DiscNumber != null)
            {
                DiscNumber = song.DiscNumber.Value;
            }
        }

        [ProtoMember(1)]
        public int DiscNumber { get; set; }
        [ProtoMember(2)]
        public string Title { get; set; }
        [ProtoMember(3)]
        public string ArtistName { get; set; }
        [ProtoMember(4)]
        public ConcurrentDictionary<int, Song> SongCollection { get; set; } = new ConcurrentDictionary<int, Song>();
        [ProtoMember(6)]
        public bool IsCompilation;
        [ProtoMember(7)] public ConcurrentSet<Song> UnlistedSongs { get; set; } = new ConcurrentSet<Song>();

        public int CompareTo(CD other)
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

            if (other?.SongCollection == null || !other.SongCollection.Any())
            {
                return result;
            }

            result += SongCollection.Sum(thisAlbums => other.SongCollection.Select(x => x.Value.FilePath).ToList()
                .Sum(thisAlbums.Value.FilePath.CompareTo));

            return result;
        }

        public bool Equals(CD other)
        {
            if (other == null)
            {
                return false;
            }

            return other.Title == Title && other.ArtistName == ArtistName &&
                   other.SongCollection.Select(x => x.Value.FilePath).ToList() ==
                   (SongCollection.Select(x => x.Value.FilePath)).ToList();

        }
    }
}
