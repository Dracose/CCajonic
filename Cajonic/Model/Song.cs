using ATL;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows.Media.Imaging;

namespace Cajonic.Model
{
    public class Song : IEquatable<Song>
    {
        public Song(Track track)
        {
            Title = string.IsNullOrEmpty(track.Title) ? string.Empty : track.Title;
            Artist = string.IsNullOrEmpty(track.Artist) ? string.Empty : track.Artist;
            Album = string.IsNullOrEmpty(track.Album) ? string.Empty : track.Album;
            AlbumArtist = string.IsNullOrEmpty(track.AlbumArtist) ? string.Empty : track.AlbumArtist;
            Composer = string.IsNullOrEmpty(track.Composer) ? string.Empty : track.Composer;
            Genre = string.IsNullOrEmpty(track.Genre) ? string.Empty : track.Genre;
            Year = track.Year == 0 ? null : (int?)track.Year;
            TrackNumber = track.TrackNumber == 0 ? null : (int?)track.TrackNumber;
            Duration = TimeSpan.FromSeconds(track.Duration);
            FilePath = track.Path;
            Lyrics = track.Lyrics == null ? new LyricsInfo() : track.Lyrics;
            Comments = string.Empty;
            Artwork = track.EmbeddedPictures == null ? null : LoadImage(track.EmbeddedPictures[0].PictureData);
        }

        public Song() { }

        public string Title { get; set; }
        public string Artist { get; set; }
        public string AlbumArtist { get; set; }
        public string Album { get; set; }
        public string Composer { get; set; }
        public string Genre { get; set; }
        public int? Year { get; set; }
        public int? TrackNumber { get; set; }
        public int? DiscNumber { get; set; }
        public int? PlayCount { get; set; }
        public string Comments { get; set; }
        public LyricsInfo Lyrics { get; set; }
        public TimeSpan Duration { get; set; }
        public string DisplayDuration
        {
            get
            {
                return Duration.ToString("mm\\:ss");
            }
        }
        public BitmapImage Artwork { get; set; }
        public string FilePath { get; set; }

        private static BitmapImage LoadImage(byte[] imageData)
        {
            if (imageData == null || imageData.Length == 0)
            {
                return null;
            }

            BitmapImage image = new BitmapImage();
            using (MemoryStream mem = new MemoryStream(imageData))
            {
                mem.Position = 0;
                image.BeginInit();
                image.CreateOptions = BitmapCreateOptions.PreservePixelFormat;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.UriSource = null;
                image.StreamSource = mem;
                image.EndInit();
            }
            image.Freeze();
            return image;
        }

        public override int GetHashCode()
        {
            return FilePath?.GetHashCode() ?? base.GetHashCode();
        }

        public bool Equals([AllowNull] Song other)
        {
            return other?.FilePath == FilePath;
        }
    }
}
