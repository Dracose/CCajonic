using ATL;
using Cajonic.Model;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.IO;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MoreLinq;

namespace Cajonic.Services
{
    public class SongLoader : ISongLoader
    {
        public ImmutableList<Song> LoadSongs(string[] paths, ICollection<Artist> artists)
        {
            //Check subfolders
            ConcurrentSet<Song> songs = new ConcurrentSet<Song>();
            ConcurrentBag<Artist> concurrentArtists = new ConcurrentBag<Artist>();
            ConcurrentBag<Artist> modifiedArtists = new ConcurrentBag<Artist>(artists);
            ConcurrentBag<Artist> mergedSongs = new ConcurrentBag<Artist>();
            ConcurrentBag<Artist> mergedSongBag = new ConcurrentBag<Artist>();
            ConcurrentBag<Artist> originalArtistBag = new ConcurrentBag<Artist>(artists);

            foreach (string path in paths)
            {
                ConcurrentBag<FileInfo> files = new ConcurrentBag<FileInfo>();
                FileAttributes fileAttributes = File.GetAttributes(path);
                if (!fileAttributes.HasFlag(FileAttributes.Directory))
                {
                    FileInfo fileInfo = new FileInfo(path);
                    files.Add(fileInfo);
                }
                else
                {
                    DirectoryInfo directoryInfo = new DirectoryInfo(path);
                    files = new ConcurrentBag<FileInfo>(directoryInfo.GetFiles("*", SearchOption.AllDirectories));
                }

                if (files.Count == 1 && !IsSupportedSongExtension(path))
                {
                    throw new Exception("This type of file isn't supported.");
                }

                Parallel.ForEach(files, file =>
                {
                    if (!IsSupportedSongExtension(file.FullName))
                    {
                        return;
                    }

                    Track track = new Track(file.FullName);

                    Artist artist = new Artist(track);
                    concurrentArtists.Add(artist);
                    if (concurrentArtists.Count > 1)
                    {
                        concurrentArtists = concurrentArtists.ArtistMerge();
                    }

                    foreach (Artist concurrentArtist in concurrentArtists)
                    {
                        ConcurrentBag<Album> artistAlbums =
                            new ConcurrentBag<Album>(concurrentArtist.ArtistAlbums.Select(x => x.Value).ToImmutableList());
                        ConcurrentBag<Album> concurrentArtistAlbums = new ConcurrentBag<Album>(artistAlbums);
                        songs.Add(CuratedSong(concurrentArtist, track, concurrentArtistAlbums));
                    }
                    mergedSongs = MergeSongs(songs);
                });
            }

            
            modifiedArtists.AddRange(mergedSongs);
            ImmutableList<Artist> mergedArtists = MergeArtists(modifiedArtists, originalArtistBag).ToImmutableList();
            artists.ReplaceRangeArtists(mergedArtists);

            ImmutableList<Artist> artistsToSerialize = artists.Where(x => x.IsSerialization).ToImmutableList();

            foreach (Artist artist in artistsToSerialize)
            {
                artist.SerializeArtistAsync();
            }

            return songs.OrderBy(x => x.TrackNumber).ToImmutableList();
        }

        private static Song CuratedSong(Artist artist, Track track, ConcurrentBag<Album> artistAlbums)
        {
            if (artistAlbums.Select(x => x.Title).Contains(track.Album) &&
                artistAlbums.Select(x => x.ArtistName).Contains(track.Artist))
            {
                Album albumExists = artist.ArtistAlbums.FirstOrDefault(x => x.Value.Title == track.Album).Value;
                Song songAlbumExists = new Song(track, albumExists, artist);

                return songAlbumExists;
            }

            Album albumNotExists = new Album(track);

            artist.ArtistAlbums.TryAdd(artist.ArtistAlbums.Count, albumNotExists);
            Song songAlbumNotExists = new Song(track, albumNotExists, artist);
            albumNotExists.AlbumSongCollection.TryAdd(songAlbumNotExists.TrackNumber ?? albumNotExists.AlbumSongCollection.Count, songAlbumNotExists);

            return songAlbumNotExists;
        }

        private static ConcurrentBag<Artist> MergeSongs(ConcurrentSet<Song> songs)
        {
            ConcurrentBag<Artist> artists = new ConcurrentBag<Artist>();

            foreach (Song song in songs)
            {
                artists.AddUniqueArtist(song.Artist);
            }

            ConcurrentBag<Album> albumsEnumerator = new ConcurrentBag<Album>(songs.Select(x => x.Album).DistinctBy(x => x.Title)
                .Where(x => !string.IsNullOrEmpty(x.ArtistName) && !string.IsNullOrEmpty(x.Title)).ToList());

            foreach (Album album in albumsEnumerator)
            {
                album.AlbumSongCollection.Clear();
                foreach (Song concurrentSong in songs.Where(x => x.AlbumTitle == album.Title && x.ArtistName == album.ArtistName))
                {
                    album.AlbumSongCollection.TryAdd(concurrentSong.TrackNumber ?? album.AlbumSongCollection.Count,
                        concurrentSong);
                }
            }

            foreach (Artist artist in artists)
            {
                artist.ArtistAlbums.Clear();
                ConcurrentDictionary<int, Album> albumsInArtist = new ConcurrentDictionary<int, Album>();
                foreach (Album album in albumsEnumerator.Where(album => album.ArtistName == artist.Name))
                {
                    artist.ArtistAlbums.TryAdd(artist.ArtistAlbums.Count, album);
                }

                ConcurrentBag<Album> artistAlbums =
                    new ConcurrentBag<Album>(artist.ArtistAlbums.Select(x => x.Value).ToImmutableList());

                foreach (Album album in artistAlbums)
                {
                    if (!albumsEnumerator.Select(x => x.ArtistName).Contains(album.ArtistName))
                    {
                        albumsInArtist.TryAdd(albumsInArtist.Count, album);
                    }

                    albumsInArtist.AddUnique(albumsEnumerator.FirstOrDefault(x => x.Title == album.Title));
                }

                artist.ArtistAlbums = albumsInArtist;
            }

            return artists;
        }

        private static IEnumerable<Artist> MergeArtists(ConcurrentBag<Artist> modifiedArtists, ConcurrentBag<Artist> artists)
        {
            ConcurrentBag<Artist> mergedArtists = new ConcurrentBag<Artist>();

            //No overlap first

            foreach (Artist modArtist in modifiedArtists)
            {
                if (!File.Exists(modArtist.BinaryFilePath))
                {
                    modArtist.IsSerialization = true;
                    mergedArtists.Add(modArtist);
                    continue;
                }

                // small foreach loop here for the no overlap

                foreach (Artist artist in artists)
                {
                    if (modArtist.BinaryFilePath != artist.BinaryFilePath)
                    {
                        continue;
                    }

                    foreach (Album modAlbum in modArtist.ArtistAlbums.Values)
                    {
                        if (artist.ArtistAlbums.Values.Select(x => x.Title).Contains(modAlbum.Title))
                        {
                            List<string> filePaths = artist.ArtistAlbums.Where(x => x.Value.Title == modAlbum.Title)
                                .SelectMany(z => z.Value.AlbumSongCollection).Select(x => x.Value.FilePath).ToList();

                            List<string> otherFilePaths = modAlbum.AlbumSongCollection.Where(x => artist.ArtistAlbums.Values.Select(x => x.Title)
                            .Contains(modAlbum.Title)).Select(x => x.Value.FilePath).ToList();

                            if (!filePaths.Except(otherFilePaths).Any())
                            {
                                continue;
                            }
                        }

                        string titleToCompare = artist.ArtistAlbums.Values.FirstOrDefault(x => x.Title == modAlbum.Title)?.Title;

                        if (titleToCompare != modAlbum.Title)
                        {
                            continue;
                        }

                        modAlbum.AlbumSongCollection.AddRange(
                            artist.ArtistAlbums.Values.SelectMany(x => x.AlbumSongCollection));
                        modAlbum.AlbumSongCollection.ForEach(x => x.Value.Artist = modArtist);
                        modAlbum.AlbumSongCollection.ForEach(x => x.Value.Album = modAlbum);

                        modArtist.IsSerialization = true;
                        mergedArtists.Add(modArtist);
                    }
                }


                List<Artist> relevantArtists = modifiedArtists.Where(x => x.BinaryFilePath == modArtist.BinaryFilePath).ToList();
                List<Artist> artistsToMerge =
                    relevantArtists.Where(x => x.ArtistAlbums != modArtist.ArtistAlbums).ToList();

                foreach (Artist artist in artistsToMerge)
                {
                    if (artist.ArtistAlbums.Values.Select(x => x.Title)
                        .Contains(artist.ArtistAlbums.SelectMany(x => x.Value.ArtistName)))
                    {
                        continue;
                    }


                    foreach (Album album in artist.ArtistAlbums.Values.Where(x => x.ArtistName == artist.Name))
                    //album doesn't exist in one or the other
                    {
                        string titleToCompare = modArtist.ArtistAlbums.Values.FirstOrDefault(x => x.ArtistName == album.ArtistName)?.Title;
                        if (titleToCompare == album.Title)
                        {
                            continue;
                        }

                        modArtist.ArtistAlbums.TryAdd(modArtist.ArtistAlbums.Count, album);
                        modArtist.IsSerialization = true;
                    }
                }
                mergedArtists.AddUniqueArtist(modArtist);
            }

            return mergedArtists;
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
