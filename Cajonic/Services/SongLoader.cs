using ATL;
using Cajonic.Model;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Cajonic.Services
{
    public class SongLoader : ISongLoader
    {
        public ImmutableList<Song> Load(string path, ICollection<Artist> artists)
        { 
            FileAttributes fileAttributes = File.GetAttributes(path);
            return fileAttributes.HasFlag(FileAttributes.Directory) ? LoadDirectory(path, artists) : ImmutableList.Create(LoadIndividualSong(path, artists));
        }

        private static ImmutableList<Song> LoadDirectory(string path, ICollection<Artist> artists)
        {
            List<Song> songs = new List<Song>();
            DirectoryInfo directory = new DirectoryInfo(path);

            foreach(FileInfo file in directory.GetFiles())
            {
                if (!IsSupportedSongExtension(file.FullName))
                {
                    continue;
                }

                Song song = LoadSong(file.FullName, artists);
                songs.Add(song);
                if (artists != null && !artists.Contains(song.Artist))
                {
                    artists.Add(song.Artist);
                }
            }

            return songs.ToList().OrderBy(s => s.TrackNumber).ToImmutableList();
        }

        private static Song LoadSong(string path, IEnumerable<Artist> artists = null)
        {
            Track track = new Track(path);

            if (artists == null)
            {
                return new Song(track);
            }

            List<Artist> artistsList = artists.ToList();
            if (!artistsList.Any())
            {
                return new Song(track);
            }

            foreach (Artist artist in artistsList)
            {
                if (artist.ArtistAlbums.Select(x => x.Title).Contains(track.Album))
                {
                    Album relevantAlbum = artist.ArtistAlbums.FirstOrDefault(x => x.Title == track.Artist);
                    return new Song(track, relevantAlbum, artist);
                }

                if (artist.Name == track.Artist)
                {
                    return new Song(track, null, artist);
                }
            }
            
            return new Song(track);

        }

        private static Song LoadIndividualSong(string path, IEnumerable<Artist> artists = null)
        {
            if (!IsSupportedSongExtension(path))
            {
                throw new Exception("This type of file isn't supported.");
            }

            return LoadSong(path, artists);
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
