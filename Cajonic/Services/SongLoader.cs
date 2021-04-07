using AudioWorks.Api;
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
                        songs.Add(LoadMp3(file));
                        break;
                    case ".flac":
                        songs.Add(LoadMp3(file));
                        break;
                    case ".wav":
                        songs.Add(LoadMp3(file));
                        break;
                    case ".m4a":
                        songs.Add(LoadMp3(file));
                        break;
                    case ".pcm":
                        songs.Add(LoadMp3(file));
                        break;
                    case ".aiff":
                        songs.Add(LoadMp3(file));
                        break;
                    case ".aac":
                        songs.Add(LoadMp3(file));
                        break;
                    case ".wma":
                        songs.Add(LoadMp3(file));
                        break;
                    default:
                        throw new TypeLoadException("This file type is not supported yet.");

                }
            });

            return songs.ToList().OrderBy(s => s.TrackNumber).ToImmutableList();
        }

        private ImmutableList<Song> LoadSong(string path)
        {
            TaggedAudioFile audioFile = new TaggedAudioFile(path);
            return ImmutableList.Create(new Song
            {
                Title = string.IsNullOrEmpty(audioFile.Metadata.Title) ? string.Empty : audioFile.Metadata.Title,
                Artist = string.IsNullOrEmpty(audioFile.Metadata.Artist) ? string.Empty : audioFile.Metadata.Artist,
                Album = string.IsNullOrEmpty(audioFile.Metadata.Album) ? string.Empty : audioFile.Metadata.Album,
                AlbumArtist = string.IsNullOrEmpty(audioFile.Metadata.AlbumArtist) ? string.Empty : audioFile.Metadata.AlbumArtist,
                Composer = string.IsNullOrEmpty(audioFile.Metadata.Composer) ? string.Empty : audioFile.Metadata.Composer,
                Genre = string.IsNullOrEmpty(audioFile.Metadata.Genre) ? string.Empty : audioFile.Metadata.Genre,
                Year = string.IsNullOrEmpty(audioFile.Metadata.Year) ? null : audioFile.Metadata.Year.ToNullableInt(),
                TrackNumber = string.IsNullOrEmpty(audioFile.Metadata.TrackNumber) ? null : audioFile.Metadata.TrackNumber.ToNullableInt(),
                Duration = audioFile.Info.PlayLength,
                FilePath = path,
                Lyrics = string.Empty,
                Comments = string.Empty,
                Artwork = audioFile.Metadata.CoverArt == null ? null : LoadImage(audioFile.Metadata.CoverArt.Data.ToArray())
            });
        }

        private Song LoadMp3(FileInfo file)
        {
            TaggedAudioFile audioFile = new TaggedAudioFile(file.FullName);
            audioFile.LoadMetadata();
            return new Song {
                Title = string.IsNullOrEmpty(audioFile.Metadata.Title) ? string.Empty : audioFile.Metadata.Title,
                Artist = string.IsNullOrEmpty(audioFile.Metadata.Artist) ? string.Empty : audioFile.Metadata.Artist,
                Album = string.IsNullOrEmpty(audioFile.Metadata.Album) ? string.Empty : audioFile.Metadata.Album,
                AlbumArtist = string.IsNullOrEmpty(audioFile.Metadata.AlbumArtist) ? string.Empty : audioFile.Metadata.AlbumArtist,
                Composer = string.IsNullOrEmpty(audioFile.Metadata.Composer) ? string.Empty : audioFile.Metadata.Composer,
                Genre = string.IsNullOrEmpty(audioFile.Metadata.Genre) ? string.Empty : audioFile.Metadata.Genre,
                Year = string.IsNullOrEmpty(audioFile.Metadata.Year) ? null : audioFile.Metadata.Year.ToNullableInt(),
                TrackNumber = string.IsNullOrEmpty(audioFile.Metadata.TrackNumber) ? null : audioFile.Metadata.TrackNumber.ToNullableInt(),
                Duration = audioFile.Info.PlayLength,
                FilePath = file.FullName,
                Lyrics = string.Empty,
                Comments = string.Empty,
                Artwork = audioFile.Metadata.CoverArt == null ? null : LoadImage(audioFile.Metadata.CoverArt.Data.ToArray())
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
