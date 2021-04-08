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
                Track track = new Track(path);
                return new Song(track);
            }
            else
            {
                throw new Exception("Yo holla at your boy man");
            }
        }

        private bool IsSupportedSongExtension(string path)
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
