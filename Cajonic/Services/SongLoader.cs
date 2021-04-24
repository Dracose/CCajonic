using ATL;
using Cajonic.Model;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.IO;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Cajonic.Services
{
    public class SongLoader : ISongLoader
    {
        public ImmutableList<Song> LoadSongs(string[] paths, ICollection<Artist> artists)
        {
            ConcurrentBag<Artist> modifiedArtists = new ConcurrentBag<Artist>(artists);
            ConcurrentBag<Artist> originalArtistBag = new ConcurrentBag<Artist>(artists);
            ConcurrentDictionary<string, Artist> concurrentArtistsDictionary =
                new ConcurrentDictionary<string, Artist>(StringComparer.InvariantCultureIgnoreCase);

            Stopwatch sw = new Stopwatch();
            sw.Start();

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
                    Artist concurrentArtist = new Artist(track);

                    if (!concurrentArtistsDictionary.ContainsKey(track.Artist))
                    {
                        if (string.IsNullOrEmpty(track.Artist))
                        {
                            // TODO: Handle Artist that doesn't exist
                        }
                        concurrentArtistsDictionary.TryAdd(track.Artist, concurrentArtist);
                    }
                    else
                    {
                        concurrentArtist = concurrentArtistsDictionary[concurrentArtist.Name];
                        if (!concurrentArtist.ArtistAlbums.ContainsKey(track.Album))
                        {
                            concurrentArtist.ArtistAlbums.TryAdd(track.Album, new Album(track));
                        }

                        if (track.DiscNumber > 0 && !concurrentArtist.ArtistAlbums[track.Album].CDs.ContainsKey(track.DiscNumber))
                        {
                            concurrentArtist.ArtistAlbums[track.Album].CDs
                                .TryAdd(track.DiscNumber, new CD(track));
                        }
                    }

                    Album relevantAlbum =
                        concurrentArtist.ArtistAlbums.Values.FirstOrDefault(x => string.Equals(x.Title, track.Album, StringComparison.InvariantCultureIgnoreCase));
                    AddSongs(concurrentArtist, track, relevantAlbum);
                });
            }

            sw.Stop();

            modifiedArtists.AddRange(concurrentArtistsDictionary.Values);
            modifiedArtists = new ConcurrentBag<Artist>(MergeArtists(modifiedArtists, originalArtistBag));
            artists.ReplaceRangeArtists(modifiedArtists);

            ImmutableList<Artist> artistsToSerialize = modifiedArtists.Where(x => x.IsSerialization).ToImmutableList();

            foreach (Artist artist in artistsToSerialize)
            {
                artist.SerializeArtistAsync();
            }

            return artists.SelectMany(x => x.ArtistAlbums.Values)
                .SelectMany(x => x.AlbumSongCollection.Values)
                .Concat(artists.SelectMany(x => x.ArtistAlbums.Values).OrderBy(x => x.Title)
                    .SelectMany(x => x.CDs.Values).SelectMany(x => x.SongCollection.Values))
                .OrderBy(x => x.DiscNumber).ThenBy(x => x.AlbumTitle).ThenBy(x => x.TrackNumber).ToImmutableList();
        }

        private static void AddSongs(Artist artist, Track track, Album relevantAlbum)
        {
            if (!string.Equals(artist.Name, track.Artist, StringComparison.InvariantCultureIgnoreCase) ||
                !string.Equals(relevantAlbum.Title, track.Album, StringComparison.InvariantCultureIgnoreCase))
            {
                return;
            }

            if (track.DiscNumber > 0)
            {
                Song newSong = new Song(track);
                if (newSong.TrackNumber == null)
                {
                    relevantAlbum.CDs[track.DiscNumber].UnlistedSongs.TryAdd(newSong);
                }
                else
                {
                    relevantAlbum.CDs[track.DiscNumber].SongCollection
                        .TryAdd(newSong.TrackNumber.Value, newSong);
                }
                newSong.Artist = artist;
                newSong.Album = relevantAlbum;
            }
            else
            {
                Song newSong = new Song(track);
                if (newSong.TrackNumber == null)
                {
                    relevantAlbum.UnlistedSongs.TryAdd(newSong);
                }
                else
                {
                    relevantAlbum.AlbumSongCollection
                        .TryAdd(newSong.TrackNumber.Value, newSong);
                }
                newSong.Artist = artist;
                newSong.Album = relevantAlbum;
            }
        }

        private static ConcurrentSet<Artist> MergeArtists(ConcurrentBag<Artist> modifiedArtists, ConcurrentBag<Artist> artists)
        {
            ConcurrentSet<Artist> mergedArtists = new ConcurrentSet<Artist>();
            foreach (Artist modArtist in modifiedArtists.Where(x => x.IsToModify))
            {
                Artist artistToReturn = modArtist;
                if (!File.Exists(artistToReturn.BinaryFilePath))
                {
                    artistToReturn.IsSerialization = true;
                    mergedArtists.Add(artistToReturn);
                    continue;
                }

                Artist existingArtist = artists
                    .FirstOrDefault(x => string.Equals(x.Name, artistToReturn.Name, StringComparison.InvariantCultureIgnoreCase));
                ConcurrentBag<string> existingArtistKeyList = new ConcurrentBag<string>(existingArtist.ArtistAlbums.Keys.ToList());

                foreach (string albumKey in existingArtistKeyList)
                {
                    if (artistToReturn.ArtistAlbums.ContainsKey(albumKey))
                    {
                        ConcurrentBag<Song> oldSongs = new ConcurrentBag<Song>(existingArtist
                            .ArtistAlbums
                            .Where(x => x.Key == albumKey)
                            .SelectMany(x => x.Value.AlbumSongCollection.Values)
                            .Concat(existingArtist.ArtistAlbums.Where(x => x.Key == albumKey)
                                .SelectMany(x => x.Value.CDs)
                                .SelectMany(x => x.Value.SongCollection.Values))
                            .ToList());

                        ConcurrentBag<Song> newSongs = new ConcurrentBag<Song>(modArtist
                            .ArtistAlbums
                            .Where(x => x.Key == albumKey)
                            .SelectMany(x => x.Value.AlbumSongCollection.Values)
                            .Concat(modArtist.ArtistAlbums.Where(x => x.Key == albumKey)
                                .SelectMany(x => x.Value.CDs)
                                .SelectMany(x => x.Value.SongCollection.Values))
                            .ToList());

                        if (oldSongs.Count > newSongs.Count)
                        {
                            Artist tempValue = existingArtist;
                            artistToReturn = tempValue;
                            existingArtist = modArtist;
                        }
                    }

                    ConcurrentBag<Song> songsToPointBack = new ConcurrentBag<Song>(existingArtist
                        .ArtistAlbums
                        .Where(x => x.Key == albumKey)
                        .SelectMany(x => x.Value.AlbumSongCollection.Values)
                        .Concat(existingArtist.ArtistAlbums.Where(x => x.Key == albumKey)
                            .SelectMany(x => x.Value.CDs)
                            .SelectMany(x => x.Value.SongCollection.Values))
                        .ToList());

                    if (!artistToReturn.ArtistAlbums.ContainsKey(albumKey))
                    {
                        artistToReturn.ArtistAlbums.TryAdd(albumKey, existingArtist.ArtistAlbums[albumKey]);
                        foreach (CD cd in existingArtist.ArtistAlbums[albumKey].CDs.Values)
                        {
                            artistToReturn.ArtistAlbums[albumKey].CDs.TryAdd(cd.SongCollection.FirstOrDefault().Value.DiscNumber ??
                                                                             artistToReturn.ArtistAlbums[albumKey].CDs.Count, cd);
                        }
                    }

                    Artist @return = artistToReturn;
                    Parallel.ForEach(songsToPointBack, song =>
                    {
                        if (song.DiscNumber != null)
                        {
                            if (!@return.ArtistAlbums[albumKey].CDs.ContainsKey(song.DiscNumber.Value))
                            {
                                @return.ArtistAlbums[albumKey].CDs
                                    .TryAdd(song.DiscNumber.Value, new CD(song));
                            }

                            if (song.TrackNumber == null)
                            {
                                @return.ArtistAlbums[albumKey].CDs.FirstOrDefault(x => x.Key == song.DiscNumber.Value)
                                    .Value.UnlistedSongs.TryAdd(song);
                            }
                            else
                            {
                                @return.ArtistAlbums[albumKey].CDs.FirstOrDefault(x => x.Key == song.DiscNumber.Value).Value
                                    .SongCollection.TryAdd(song.TrackNumber.Value, song);
                            }
                            song.Album = @return.ArtistAlbums[albumKey];
                            song.Artist = @return;
                        }
                        else
                        {
                            if (song.TrackNumber == null)
                            {
                                @return.ArtistAlbums[albumKey].UnlistedSongs.TryAdd(song);
                            }
                            else
                            {
                                @return.ArtistAlbums[albumKey].AlbumSongCollection.TryAdd(song.TrackNumber.Value, song);
                            }

                            song.Album = @return.ArtistAlbums[albumKey];
                            song.Artist = @return;
                        }
                    });
                }
                artistToReturn.IsSerialization = true;
                mergedArtists.TryAdd(artistToReturn);
            }

            return mergedArtists;
        }

        private static bool IsSupportedSongExtension(string path)
        {
            // TODO : Check which extensions to add.
            string pathExtension = Path.GetExtension(path);
            return pathExtension.ToLowerInvariant() switch
            {
                ".mp3" => true,
                ".caf" => true,
                ".aax" => true,
                ".aa" => true,
                ".flac" => true,
                ".wav" => true,
                ".m4a" => true,
                ".pcm" => true,
                ".aiff" => true,
                ".aif" => true,
                ".aifc" => true,
                ".aac" => true,
                ".wma" => true,
                _ => false,
            };
        }
    }
}
