using ATL;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows.Media.Imaging;

namespace Cajonic.Model
{
    public class Song : IEquatable<Song>
    {
        public Song(Track audioFile)
        {
            Title = string.IsNullOrEmpty(audioFile.Title) ? string.Empty : audioFile.Title;
            Artist = string.IsNullOrEmpty(audioFile.Artist) ? string.Empty : audioFile.Artist;
            Album = string.IsNullOrEmpty(audioFile.Album) ? string.Empty : audioFile.Album;
            AlbumArtist = string.IsNullOrEmpty(audioFile.AlbumArtist) ? string.Empty : audioFile.AlbumArtist;
            Composer = string.IsNullOrEmpty(audioFile.Composer) ? string.Empty : audioFile.Composer;
            Genre = string.IsNullOrEmpty(audioFile.Genre) ? string.Empty : audioFile.Genre;
            Year = audioFile.Year == 0 ? null : (int?)audioFile.Year;
            TrackNumber = audioFile.TrackNumber == 0 ? null : (int?)audioFile.TrackNumber;
            Duration = TimeSpan.FromSeconds(audioFile.Duration);
            FilePath = audioFile.Path;
            Lyrics = audioFile.Lyrics == null ? new LyricsInfo() : audioFile.Lyrics;
            Comments = string.Empty;
            Artwork = audioFile.EmbeddedPictures == null ? null : LoadImage(audioFile.EmbeddedPictures[0].PictureData);
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
