using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        public BitmapImage Artwork => mByteArtwork;

        private int? mUseless;
        [ProtoMember(8)]
        public int? AlbumYear
        {
            get
            {
                return AllSongs.Select(x => x.Year).Distinct().Skip(1).Any()
                    ? AllSongs.Select(x => x.Year).FirstOrDefault()
                    : null;
            }
            set => mUseless = value;
        }

        private BitmapImage mByteArtwork =>
            //if (AlbumSongCollection.Any() && AlbumSongCollection.Values.Select(item => item.ByteArtwork).Where(x => x != null).Distinct().Skip(1).Any())
            //{
            //    return AlbumSongCollection.Values.FirstOrDefault()?.ByteArtwork;
            //}
            null;

        public Album()
        {
        }

        public Album(Track track)
        {
            Title = track.Album;
            ArtistName = track.Artist;
        }

        public Album(Song song)
        {
            Title = song.AlbumTitle;
            ArtistName = song.ArtistName;
            CDs = null;
        }

        [ProtoMember(2)]
        public string Title { get; set; }
        [ProtoMember(3)]
        public string ArtistName { get; set; }
        [ProtoMember(4)]
        public ConcurrentDictionary<int, Song> AlbumSongCollection { get; set; } = new ConcurrentDictionary<int, Song>();
        [ProtoMember(5)]
        public ConcurrentDictionary<int, CD> CDs { get; set; } = new ConcurrentDictionary<int, CD>();
        [ProtoMember(6)]
        public bool IsCompilation;

        [ProtoMember(7)] 
        public ConcurrentSet<Song> UnlistedSongs { get; set; } = new ConcurrentSet<Song>();


        public bool HasCDs => !CDs.IsEmpty;

        public ImmutableList<Song> AllSongs
        {
            get
            {
                if (HasCDs)
                {
                    return AlbumSongCollection.Values.Concat(CDs.Values.SelectMany(x => x.SongCollection.Values))
                        .ToImmutableList();
                }

                return AlbumSongCollection.Values.ToImmutableList();
            }
        }

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

            result += AlbumSongCollection.Sum(thisAlbums => other.AlbumSongCollection.Select(x => x.Value.FilePath).ToList().Sum(thisAlbums.Value.FilePath.CompareTo));

            return result;
        }

        public bool Equals(Album other)
        {
            if (other == null)
            {
                return false;
            }

            IEnumerable<string> otherCdFilePaths = other.CDs.SelectMany(x => x.Value.SongCollection).Select(x => x.Value.FilePath);
            IEnumerable<string> thisFilePaths = CDs.SelectMany(x => x.Value.SongCollection).Select(x => x.Value.FilePath);
            bool isFilePathsSame = EnumerableExtension.Except(otherCdFilePaths, thisFilePaths).Any();

            return other.Title == Title && other.ArtistName == ArtistName &&
                   other.AlbumSongCollection.Select(x => x.Value.FilePath).ToList() ==
                   (AlbumSongCollection.Select(x => x.Value.FilePath)).ToList() &&
                   other.CDs.Keys.ToList() == CDs.Keys.ToList() && isFilePathsSame;

        }
    }
}
