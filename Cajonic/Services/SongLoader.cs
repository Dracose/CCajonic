using ATL;
using Cajonic.Model;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.IO;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace Cajonic.Services
{
    public class SongLoader : ISongLoader
    {
        public ImmutableList<Song> Load(string path)
        {
            FileAttributes fileAttributes = File.GetAttributes(path);
            return fileAttributes.HasFlag(FileAttributes.Directory) ? LoadDirectory(path) : ImmutableList.Create(LoadIndividualSong(path)); ;
        }

        private ImmutableList<Song> LoadDirectory(string path)
        {
            ConcurrentBag<Song> songs = new ConcurrentBag<Song>();
            ConcurrentBag<FileInfo> filesBag = new ConcurrentBag<FileInfo>();

            DirectoryInfo directory = new DirectoryInfo(path);
            filesBag.AddRange(directory.GetFiles());

            Parallel.ForEach(filesBag, file =>
            {
                if (IsSupportedSongExtension(file.FullName))
                {
                    songs.Add(LoadSong(file.FullName));
                }
            });

            return songs.ToList().OrderBy(s => s.TrackNumber).ToImmutableList();
        }

        private Song LoadSong(string path)
        {
            Track audioFile = new Track(path);
            return new Song(audioFile);
        }

        private Song LoadIndividualSong(string path)
        {
            if (IsSupportedSongExtension(path))
            {
                Track audioFile = new Track(path);
                return new Song(audioFile);
            }
            else
            {
                throw new Exception("Yo holla at your boy man");
            }
        }

        private bool IsSupportedSongExtension(string path)
        {
            string pathExtension = Path.GetExtension(path);
            switch (pathExtension)
            {
                case ".mp3":
                    return true;
                case ".flac":
                    return true;
                case ".wav":
                    return true;
                case ".m4a":
                    return true;
                case ".pcm":
                    return true;
                case ".aiff":
                    return true;
                case ".aac":
                    return true;
                case ".wma":
                    return true;
                default:
                    return false;
            }
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
    }
}
