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
                new ConcurrentDictionary<string, Artist>();

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

                    if (!concurrentArtistsDictionary.ContainsKey(track.Artist))
                    {
                        concurrentArtistsDictionary.TryAdd(track.Artist, new Artist(track));
                    }
                    else
                    {
                        if (track.DiscTotal != 0 && track.DiscTotal != 1)
                        {
                            concurrentArtistsDictionary[track.Artist].ArtistAlbums[track.Album].CDs
                                .TryAdd(track.DiscNumber, new Album(track));
                        }
                        else
                        {
                            concurrentArtistsDictionary[track.Artist].ArtistAlbums
                                .TryAdd(track.Album, new Album(track));
                        }
                    }

                    foreach (Artist concurrentArtist in concurrentArtistsDictionary.Values)
                    {
                        AddSongs(concurrentArtist, track, concurrentArtist.ArtistAlbums.Values);
                    }
                });
            }

            modifiedArtists.AddRange(concurrentArtistsDictionary.Values);
            modifiedArtists = new ConcurrentBag<Artist>(MergeArtists(modifiedArtists, originalArtistBag));
            artists.ReplaceRangeArtists(modifiedArtists);

            ImmutableList<Artist> artistsToSerialize = modifiedArtists.Where(x => x.IsSerialization).ToImmutableList();

            foreach (Artist artist in artistsToSerialize)
            {
                artist.SerializeArtistAsync();
            }

            return artists.SelectMany(x => x.ArtistAlbums.Values).OrderBy(x => x.Title)
                .SelectMany(x => x.AlbumSongCollection.Values).OrderBy(x => x.TrackNumber)
                .Concat(artists.SelectMany(x => x.ArtistAlbums.Values).OrderBy(x => x.Title)
                    .SelectMany(x=> x.CDs.Values).SelectMany(x => x.AlbumSongCollection.Values)).ToImmutableList();
        }

        private static void AddSongs(Artist artist, Track track, ICollection<Album> artistAlbums)
        {
            foreach (Album album in artistAlbums)
            {
                if (artist.Name != track.Artist && album.Title != track.Album)
                {
                    continue;
                }

                if (track.DiscTotal == 1 || track.DiscTotal == 0)
                {
                    Song newSong = new Song(track);
                    album.AlbumSongCollection.TryAdd(newSong.TrackNumber ?? album.AlbumSongCollection.Count, newSong);
                    newSong.Artist = artist;
                    newSong.Album = album;
                }

                else
                {
                    Song newSong = new Song(track);
                    album.CDs[track.DiscNumber].AlbumSongCollection.TryAdd(newSong.TrackNumber ?? album.AlbumSongCollection.Count, newSong);
                    newSong.Artist = artist;
                    newSong.Album = album;
                }
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

                Artist existingArtist = artists.FirstOrDefault(x => x.Name == artistToReturn.Name);
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
                                .SelectMany(x => x.Value.AlbumSongCollection.Values))
                            .ToList());

                        ConcurrentBag<Song> newSongs = new ConcurrentBag<Song>(modArtist
                            .ArtistAlbums
                            .Where(x => x.Key == albumKey)
                            .SelectMany(x => x.Value.AlbumSongCollection.Values)
                            .Concat(modArtist.ArtistAlbums.Where(x => x.Key == albumKey)
                                .SelectMany(x => x.Value.CDs)
                                .SelectMany(x => x.Value.AlbumSongCollection.Values))
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
                            .SelectMany(x => x.Value.AlbumSongCollection.Values))
                        .ToList());

                    if (!artistToReturn.ArtistAlbums.ContainsKey(albumKey))
                    {
                        artistToReturn.ArtistAlbums.TryAdd(albumKey, existingArtist.ArtistAlbums[albumKey]);
                        foreach (Album cd in existingArtist.ArtistAlbums[albumKey].CDs.Values)
                        {
                            artistToReturn.ArtistAlbums[albumKey].CDs
                                .TryAdd(cd.AlbumSongCollection.FirstOrDefault().Value.DiscNumber ?? 
                                        artistToReturn.ArtistAlbums[albumKey].CDs.Count, cd);
                        }
                    }

                    foreach (Song song in songsToPointBack)
                    {
                        if (song.DiscNumber != null)
                        {
                            if (!artistToReturn.ArtistAlbums[albumKey].CDs.ContainsKey(song.DiscNumber.Value))
                            {
                                artistToReturn.ArtistAlbums[albumKey].CDs
                                    .TryAdd(song.DiscNumber.Value, new Album(song));
                            }
                            int collectionCount = song.Album.AlbumSongCollection.Count;
                            artistToReturn.ArtistAlbums[albumKey].CDs.FirstOrDefault(x => x.Key == song.DiscNumber.Value).Value
                                .AlbumSongCollection.TryAdd(song.TrackNumber ?? collectionCount, song);
                            song.Album = artistToReturn.ArtistAlbums[albumKey];
                            song.Artist = artistToReturn;
                        }
                        else
                        {
                            int collectionCount = song.Album.AlbumSongCollection.Count;
                            artistToReturn.ArtistAlbums[albumKey].AlbumSongCollection.TryAdd(song.TrackNumber ?? collectionCount, song);
                            song.Album = artistToReturn.ArtistAlbums[albumKey];
                            song.Artist = artistToReturn;
                        }
                    }
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
