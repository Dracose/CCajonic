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

        private static ImmutableList<Song> LoadDirectory(string path)
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

        private static Song LoadSong(string path)
        {
            Track track = new Track(path);
            return new Song(track);
        }

        private static Song LoadIndividualSong(string path)
        {
            if (!IsSupportedSongExtension(path))
            {
                throw new Exception("This type of file isn't supported.");
            }

            Track track = new Track(path);
            return new Song(track);

        }

        private static bool IsSupportedSongExtension(string path)
        {
            // TODO : Check which extensions to add.
            string pathExtension = Path.GetExtension(path);
            return pathExtension switch
            {
                ".mp3" => true,
                ".flac" => true,
                ".wav" => true,
                ".m4a" => true,
                ".pcm" => true,
                ".aiff" => true,
                ".aac" => true,
                ".wma" => true,
                _ => false,
            };
        }
    }
}
