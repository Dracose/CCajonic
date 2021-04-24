using ATL;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using Cajonic.Services;
using Cajonic.Services.Wrappers;
using ProtoBuf;

namespace Cajonic.Model
{
    [ProtoContract]
    public class Song : IEquatable<Song>, IComparable<Song>
    {
        [ProtoMember(1)]
        private string mFilePath;

        private BitmapImage mByteArtwork;

        public BitmapImage ByteArtwork
        {
            get => mByteArtwork;
            set => mByteArtwork = value;
        }

        public Song(Track track)
        {
            Title = string.IsNullOrEmpty(track.Title) ? string.Empty : track.Title;
            ArtistName = string.IsNullOrEmpty(track.Artist) ? string.Empty : track.Artist;
            AlbumTitle = string.IsNullOrEmpty(track.Album) ? string.Empty : track.Album;
            AlbumArtist = string.IsNullOrEmpty(track.AlbumArtist) ? string.Empty : track.AlbumArtist;
            Composer = string.IsNullOrEmpty(track.Composer) ? string.Empty : track.Composer;
            Genre = string.IsNullOrEmpty(track.Genre) ? string.Empty : track.Genre;
            Year = track.Year == 0 ? null : (int?)track.Year;
            TrackNumber = track.TrackNumber == 0 ? null : (int?)track.TrackNumber;
            Duration = TimeSpan.FromMilliseconds(track.DurationMs);
            DiscNumber = track.DiscNumber == 0 ? null : (int?)track.DiscNumber;
            FilePath = track.Path;
            Comments = string.Empty;
            //This is very costly !
            //ByteArtwork = BitmapHelper.LoadImage(track.EmbeddedPictures.FirstOrDefault()?.PictureData);
        }

        public Song()
        {
        }

        [ProtoMember(3)]
        public string ArtistName { get; set; }
        [ProtoMember(4)]
        public string AlbumTitle { get; set; }
        [ProtoMember(5)]
        public string Title { get; set; }
        public Artist Artist { get; set; }
        [ProtoMember(7)]
        public string AlbumArtist { get; set; }
        public Album Album { get; set; }
        [ProtoMember(9)]
        public string Composer { get; set; }
        [ProtoMember(10)]
        public string Genre { get; set; }
        [ProtoMember(11)]
        public int? Year { get; set; }
        [ProtoMember(12)]
        public int? TrackNumber { get; set; }
        [ProtoMember(13)]
        public int? DiscNumber { get; set; }
        [ProtoMember(14)]
        public int? PlayCount { get; set; }
        [ProtoMember(15)]
        public string Comments { get; set; }
        [ProtoMember(17)]
        public TimeSpan Duration { get; set; }
        public string DisplayDuration => Duration.Days != 0 ? Duration.ToString("dd\\:hh\\:mm\\:ss") : Duration.ToString(Duration.Hours != 0 ? "hh\\:mm\\:ss" : "mm\\:ss");

        public string FilePath
        {
            get => string.IsNullOrEmpty(mFilePath) ? string.Empty : mFilePath;
            set => mFilePath = value;
        }

        public int CompareTo(Song other)
        {
            return string.Compare(FilePath, other?.FilePath, StringComparison.Ordinal);
        }

        public override int GetHashCode() => FilePath?.GetHashCode() ?? base.GetHashCode();

        public bool Equals([AllowNull] Song other) => other?.FilePath == FilePath;
    }
}
