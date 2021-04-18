using ATL;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Windows.Media.Imaging;
using Cajonic.Services;
using Cajonic.Services.Wrappers;

namespace Cajonic.Model
{
    [Serializable]
    public class Song : IEquatable<Song>
    {
        private string mFilePath;
        private string mBinaryFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Cajonic\\SaveData\\Songs.bin");

        [NonSerialized]
        private BitmapImage mArtwork;
        public BitmapImage Artwork
        {
            get => mArtwork;
            set => mArtwork = value;
        }

        private byte[] mByteArtwork;

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
            Duration = TimeSpan.FromMilliseconds(track.DurationMs);
            DiscNumber = track.DiscNumber == 0 ? null : (int?) track.DiscNumber;
            FilePath = track.Path;
            Lyrics = track.Lyrics == null ? new SerializableLyricsInfo() : new SerializableLyricsInfo(track.Lyrics);
            Comments = string.Empty;
            Artwork = track.EmbeddedPictures == null ? null : LoadImage(track.EmbeddedPictures[0].PictureData);
            mByteArtwork = ConvertToBytes(Artwork);
            //On a different thread
            BinarySerialization.WriteToBinaryFile(mBinaryFilePath, this);
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
        public SerializableLyricsInfo Lyrics { get; set; }
        public TimeSpan Duration { get; set; }
        public string DisplayDuration => Duration.Days != 0 ? Duration.ToString("dd\\:hh\\:mm\\:ss") : Duration.ToString(Duration.Hours != 0 ? "hh\\:mm\\:ss" : "mm\\:ss");

        public string FilePath
        {
            get => string.IsNullOrEmpty(mFilePath) ? string.Empty : mFilePath;
            set => mFilePath = value;
        }

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

        private static byte[] ConvertToBytes(BitmapImage bitmapImage)
        {
            BitmapEncoder encoder = new PngBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(bitmapImage));
            using MemoryStream ms = new MemoryStream();
            encoder.Save(ms);

            return ms.ToArray();
        }

        public override int GetHashCode() => FilePath?.GetHashCode() ?? base.GetHashCode();

        public bool Equals([AllowNull] Song other) => other?.FilePath == FilePath;
    }
}
