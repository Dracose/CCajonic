using System;
using System.Windows.Media.Imaging;

namespace Cajonic.Model
{
    public class Song
    {
        public string Artist { get; set; }
        public string AlbumArtist { get; set; }
        public string Album { get; set; }
        public string Composer { get; set; }
        public string Genre { get; set; }
        public int Year { get; set; }
        public int TrackNumber { get; set; }
        public int DiscNumber { get; set; }
        public int PlayCount { get; set; }
        public string Comments { get; set; }
        public string Lyrics { get; set; }
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
    }
}
