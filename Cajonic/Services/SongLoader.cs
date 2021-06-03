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
        public ImmutableList<Song> LoadSongs(string[] paths, ICollection<Artist> artists, ICollection<Song> allSongs)
        {
            ConcurrentBag<Artist> modifiedArtists = new(artists);
            ConcurrentBag<Artist> originalArtistBag = new(artists);
            ConcurrentDictionary<string, Artist> concurrentArtistsDictionary =
                new(StringComparer.InvariantCultureIgnoreCase);

            ConcurrentBag<FileInfo> files = new();
            Parallel.ForEach(paths, path =>
            {
                FileAttributes fileAttributes = File.GetAttributes(path);
                if (!fileAttributes.HasFlag(FileAttributes.Directory))
                {
                    FileInfo fileInfo = new(path);
                    files.Add(fileInfo);
                }
                else
                {
                    DirectoryInfo directoryInfo = new(path);
                    files.AddRange(directoryInfo.GetFiles("*", SearchOption.AllDirectories));
                }
            });

            if (files.Count == 1 && paths.Length == 1 && !IsSupportedSongExtension(files.First().FullName))
            {
                throw new Exception("This type of file isn't supported.");
            }

            Parallel.ForEach(files, file =>
            {
                if (!IsSupportedSongExtension(file.FullName))
                {
                    return;
                }

                if (allSongs.Select(x => x.FilePath).Contains(file.FullName))
                {
                    return;
                }

                Track track = new(file.FullName);
                Artist concurrentArtist = new(track);

                if (!concurrentArtistsDictionary.ContainsKey(concurrentArtist.BinaryFilePath))
                {
                    concurrentArtistsDictionary.TryAdd(concurrentArtist.BinaryFilePath, concurrentArtist);
                    concurrentArtist = concurrentArtistsDictionary[concurrentArtist.BinaryFilePath];
                }
                else
                {
                    concurrentArtist = concurrentArtistsDictionary[concurrentArtist.BinaryFilePath];
                    if (string.IsNullOrEmpty(track.Album) &&
                        !concurrentArtist.ArtistAlbums.ContainsKey(Album.UnknownAlbum))
                    {
                        Album unknownAlbum = new(track, concurrentArtist.Name);
                        concurrentArtist.ArtistAlbums.TryAdd(unknownAlbum.Title, unknownAlbum);
                    }

                    else if (!string.IsNullOrEmpty(track.Album) &&
                             !concurrentArtist.ArtistAlbums.ContainsKey(track.Album))
                    {
                        Album newAlbum = new(track, concurrentArtist.Name);
                        concurrentArtist.ArtistAlbums.TryAdd(track.Album, newAlbum);
                    }

                    if (track.DiscNumber > 0 &&
                        !concurrentArtist.ArtistAlbums[track.Album].CDs.ContainsKey(track.DiscNumber))
                    {
                        CD newCd = new(track);
                        concurrentArtist.ArtistAlbums[track.Album].CDs.TryAdd(track.DiscNumber, newCd);
                    }
                }

                Album relevantAlbum = string.IsNullOrEmpty(track.Album)
                    ? concurrentArtist.ArtistAlbums[Album.UnknownAlbum]
                    : concurrentArtist.ArtistAlbums.Values.FirstOrDefault(x =>
                        string.Equals(x.Title, track.Album, StringComparison.InvariantCultureIgnoreCase));
                AddSongs(concurrentArtist, track, relevantAlbum);
            });


            modifiedArtists.AddRange(concurrentArtistsDictionary.Values);
            modifiedArtists = new ConcurrentBag<Artist>(MergeArtists(modifiedArtists, originalArtistBag));
            artists.ReplaceRangeArtists(modifiedArtists);

            ImmutableList<Artist> artistsToSerialize = modifiedArtists.Where(x => x.IsSerialization).ToImmutableList();

            foreach (Artist artist in artistsToSerialize)
            {
                artist.SerializeArtistAsync();
            }

            ImmutableList<Song> songsToReturn = artists.SelectMany(x => x.ArtistAlbums.Values)
                .SelectMany(x => x.AllSongs)
                .OrderBy(x => x.ArtistName)
                .ThenBy(x => x.AlbumTitle)
                .ThenBy(x => x.DiscNumber)
                .ThenBy(x => x.TrackNumber).ToImmutableList();

            return songsToReturn;
        }

        private static void AddSongs(Artist artist, Track track, Album relevantAlbum)
        {
            if (!string.Equals(artist.Name, track.Artist, StringComparison.InvariantCultureIgnoreCase) &&
                !string.IsNullOrEmpty(track.Artist) ||
                !string.Equals(relevantAlbum.Title, track.Album, StringComparison.InvariantCultureIgnoreCase) &&
                !string.IsNullOrEmpty(track.Album))
            {
                return;
            }

            if (track.DiscNumber > 0)
            {
                Song newSong = new(track);
                if (newSong.TrackNumber == null)
                {
                    relevantAlbum.CDs[track.DiscNumber].UnlistedSongs.TryAdd(newSong);
                }
                else
                {
                    bool result = relevantAlbum.CDs[track.DiscNumber].SongCollection
                        .TryAdd(newSong.TrackNumber.Value, newSong);

                    if (!result)
                    {
                        newSong.TrackNumber = null;
                        relevantAlbum.CDs[track.DiscNumber].UnlistedSongs.TryAdd(newSong);
                    }
                }

                newSong.Artist = artist;
                newSong.Album = relevantAlbum;
            }
            else
            {
                Song newSong = new(track);
                if (newSong.TrackNumber == null)
                {
                    relevantAlbum.UnlistedSongs.TryAdd(newSong);
                }
                else
                {
                    bool result = relevantAlbum.AlbumSongCollection.TryAdd(newSong.TrackNumber.Value, newSong);
                    if (!result)
                    {
                        newSong.TrackNumber = null;
                        relevantAlbum.UnlistedSongs.TryAdd(newSong);
                    }
                }

                if (relevantAlbum.Title == Album.UnknownAlbum)
                {
                    newSong.AlbumTitle = Album.UnknownAlbum;
                }

                if (artist.Name == Artist.UnknownArtist)
                {
                    newSong.ArtistName = Artist.UnknownArtist;
                }

                newSong.Artist = artist;
                newSong.Album = relevantAlbum;
            }
        }

        private static ConcurrentSet<Artist> MergeArtists(ConcurrentBag<Artist> modifiedArtists, ConcurrentBag<Artist> artists)
        {
            ConcurrentSet<Artist> mergedArtists = new();
            ConcurrentBag<Artist> artistsToModify = new(modifiedArtists.Where(x => x.IsToModify));
            Parallel.ForEach(artistsToModify, modArtist =>
            {
                Artist artistToReturn = modArtist;
                if (!File.Exists(artistToReturn.BinaryFilePath))
                {
                    artistToReturn.IsSerialization = true;
                    mergedArtists.Add(artistToReturn);
                    return;
                }

                Artist existingArtist = artists
                    .FirstOrDefault(x =>
                        string.Equals(x.Name, artistToReturn.Name, StringComparison.InvariantCultureIgnoreCase));
                ConcurrentBag<string> existingArtistKeyList = new(existingArtist.ArtistAlbums.Keys.ToList());

                foreach (string albumKey in existingArtistKeyList)
                {
                    if (artistToReturn.ArtistAlbums.ContainsKey(albumKey))
                    {
                        ConcurrentBag<Song> oldSongs = new(existingArtist
                            .ArtistAlbums
                            .Where(x => x.Key == albumKey)
                            .SelectMany(x => x.Value.AlbumSongCollection.Values)
                            .Concat(existingArtist.ArtistAlbums.Where(x => x.Key == albumKey)
                                .SelectMany(x => x.Value.CDs)
                                .SelectMany(x => x.Value.SongCollection.Values))
                            .ToList());

                        ConcurrentBag<Song> newSongs = new(modArtist
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

                    ConcurrentBag<Song> songsToPointBack = new(existingArtist
                        .ArtistAlbums
                        .Where(x => x.Key == albumKey)
                        .SelectMany(x => x.Value.AllSongs)
                        .ToList());

                    if (!artistToReturn.ArtistAlbums.ContainsKey(albumKey))
                    {
                        artistToReturn.ArtistAlbums.TryAdd(albumKey, existingArtist.ArtistAlbums[albumKey]);
                        foreach (CD cd in existingArtist.ArtistAlbums[albumKey].CDs.Values)
                        {
                            artistToReturn.ArtistAlbums[albumKey].CDs.TryAdd(
                                cd.SongCollection.FirstOrDefault().Value.DiscNumber ??
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
                                @return.ArtistAlbums[albumKey].CDs.FirstOrDefault(x => x.Key == song.DiscNumber.Value)
                                    .Value
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
            });

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