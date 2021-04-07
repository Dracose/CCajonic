using ATL;
using Cajonic.Model;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;

namespace Cajonic.Services
{
    public class SongLoader : ISongLoader
    {
        public ImmutableList<Song> Load(string path)
        {
            path = "E:\\Musique\\Bruton Music - Generation Gap (1981)\\01 - James Asher - Brick In The Wall.m4a";
            FileAttributes fileAttributes = File.GetAttributes(path);
            return fileAttributes.HasFlag(FileAttributes.Directory) ? LoadDirectory(path) : LoadSong(path);
        }

        private ImmutableList<Song> LoadDirectory(string path)
        {
            ConcurrentBag<Song> songs = new ConcurrentBag<Song>();
            ConcurrentBag<FileInfo> filesBag = new ConcurrentBag<FileInfo>();

            DirectoryInfo directory = new DirectoryInfo(path);
            filesBag.AddRange(directory.GetFiles());

            Parallel.ForEach(filesBag, file =>
            {
                switch (file.Extension.ToLower())
                {
                    case ".mp3":
                        songs.Add(LoadMp3(path));
                        break;
                    case ".flac":
                        songs.Add(LoadMp3(path));
                        break;
                    case ".wav":
                        songs.Add(LoadMp3(path));
                        break;
                    case ".m4a":
                        songs.Add(LoadMp3(path));
                        break;
                    case ".pcm":
                        songs.Add(LoadMp3(path));
                        break;
                    case ".aiff":
                        songs.Add(LoadMp3(path));
                        break;
                    case ".aac":
                        songs.Add(LoadMp3(path));
                        break;
                    case ".wma":
                        songs.Add(LoadMp3(path));
                        break;
                    default:
                        throw new TypeLoadException("This file type is not supported yet.");

                }
            });

            return songs.ToList().OrderBy(s => s.TrackNumber).ToImmutableList();
        }

        private ImmutableList<Song> LoadSong(string path)
        {
            Track audioFile = new Track(path);
            return ImmutableList.Create(new Song
            {
                Title = string.IsNullOrEmpty(audioFile.Title) ? string.Empty : audioFile.Title,
                Artist = string.IsNullOrEmpty(audioFile.Artist) ? string.Empty : audioFile.Artist,
                Album = string.IsNullOrEmpty(audioFile.Album) ? string.Empty : audioFile.Album,
                AlbumArtist = string.IsNullOrEmpty(audioFile.AlbumArtist) ? string.Empty : audioFile.AlbumArtist,
                Composer = string.IsNullOrEmpty(audioFile.Composer) ? string.Empty : audioFile.Composer,
                Genre = string.IsNullOrEmpty(audioFile.Genre) ? string.Empty : audioFile.Genre,
                Year = audioFile.Year == 0 ? null : (int?)audioFile.Year,
                TrackNumber = audioFile.TrackNumber == 0 ? null : (int?)audioFile.TrackNumber,
                Duration = TimeSpan.FromSeconds(audioFile.Duration),
                FilePath = path,
                Lyrics = audioFile.Lyrics == null ? new LyricsInfo() : audioFile.Lyrics,
                Comments = string.Empty,
                Artwork = audioFile.EmbeddedPictures == null ? null : LoadImage(audioFile.EmbeddedPictures[0].PictureData)
            });
        }

        private Song LoadMp3(string path)
        {
            Track audioFile = new Track(path);
            return new Song {
                Title = string.IsNullOrEmpty(audioFile.Title) ? string.Empty : audioFile.Title,
                Artist = string.IsNullOrEmpty(audioFile.Artist) ? string.Empty : audioFile.Artist,
                Album = string.IsNullOrEmpty(audioFile.Album) ? string.Empty : audioFile.Album,
                AlbumArtist = string.IsNullOrEmpty(audioFile.AlbumArtist) ? string.Empty : audioFile.AlbumArtist,
                Composer = string.IsNullOrEmpty(audioFile.Composer) ? string.Empty : audioFile.Composer,
                Genre = string.IsNullOrEmpty(audioFile.Genre) ? string.Empty : audioFile.Genre,
                Year = audioFile.Year == 0 ? null : (int?)audioFile.Year,
                TrackNumber = audioFile.TrackNumber == 0 ? null : (int?)audioFile.TrackNumber,
                Duration = TimeSpan.FromSeconds(audioFile.Duration),
                FilePath = path,
                Lyrics = audioFile.Lyrics == null ? new LyricsInfo() : audioFile.Lyrics,
                Comments = string.Empty,
                Artwork = audioFile.EmbeddedPictures == null ? null : LoadImage(audioFile.EmbeddedPictures[0].PictureData)
            };
        }
        /**
        private Song LoadFlac(FileInfo file) { throw new NotImplementedException(); }
        private Song LoadWav(FileInfo file) { throw new NotImplementedException(); }
        public Song LoadM4a(FileInfo file) { throw new NotImplementedException(); }
        public Song LoadOpus(FileInfo file) { throw new NotImplementedException(); }
        public Song LoadOggVorbis(FileInfo file) { throw new NotImplementedException(); }
        public Song LoadAac(FileInfo file) { throw new NotImplementedException(); }
        public Song LoadWma(FileInfo file) { throw new NotImplementedException(); }
        */

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
    }
}
