using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
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
        public const string UnknownAlbum = "Unknown Album";

        private int? mUseless;
        [ProtoMember(9)]
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

        public Album() { }

        public Album(Track track, string artistName)
        {
            Title = string.IsNullOrEmpty(track.Album) ? UnknownAlbum : track.Album;
            ArtistName = artistName;
            DateAdded = DateTime.Now;
            
            if (track.DiscNumber > 0)
            {
                CD newCd = new(track);
                CDs.TryAdd(track.DiscNumber, newCd);
            }
        }

        public Album(Song song)
        {
            Title = song.AlbumTitle;
            ArtistName = song.ArtistName;
            DateAdded = DateTime.Now;
            
            if (song.DiscNumber > 0)
            {
                CD newCd = new(song);
                CDs.TryAdd(song.DiscNumber.Value, newCd);
            }
        }

        [ProtoMember(2)]
        public string Title { get; set; }
        [ProtoMember(3)]
        public string ArtistName { get; set; }
        [ProtoMember(4)]
        public ConcurrentDictionary<int, Song> AlbumSongCollection { get; set; } = new();
        [ProtoMember(5)]
        public ConcurrentDictionary<int, CD> CDs { get; set; } = new();
        [ProtoMember(6)]
        public bool IsCompilation;
        [ProtoMember(8)] 
        public DateTime DateAdded { get; }
        [ProtoMember(7)] 
        public ConcurrentSet<Song> UnlistedSongs { get; set; } = new();


        public bool HasCDs => !CDs.IsEmpty;

        public ImmutableList<Song> AllSongs
        {
            get
            {
                if (HasCDs)
                {
                    return AlbumSongCollection.Values
                        .Concat(CDs.Values.SelectMany(x => x.SongCollection.Values).Concat(CDs.Values.SelectMany(x => x.UnlistedSongs)))
                        .Concat(UnlistedSongs)
                        .ToImmutableList();
                }

                return AlbumSongCollection.Values.Concat(UnlistedSongs).ToImmutableList();
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

            IEnumerable<string> otherCdFilePaths = other.CDs.SelectMany(x => x.Value.SongCollection).Select(x => x.Value.FilePath).ToList();
            IEnumerable<string> thisFilePaths = CDs.SelectMany(x => x.Value.SongCollection).Select(x => x.Value.FilePath).ToList();
            bool isFilePathsSame = !EnumerableExtension.Except(otherCdFilePaths, thisFilePaths).Any();

            return other.Title == Title && other.ArtistName == ArtistName && isFilePathsSame;

        }
    }
}
