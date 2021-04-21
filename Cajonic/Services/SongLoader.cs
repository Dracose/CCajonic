using ATL;
using Cajonic.Model;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.IO;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using MoreLinq;

namespace Cajonic.Services
{
    public class SongLoader : ISongLoader
    {
        private static readonly Object mLock = new Object();

        public ImmutableList<Song> LoadMultiple(string[] paths, ICollection<Artist> artists)
        {
            ConcurrentBag<Song> songs = new ConcurrentBag<Song>();
            ConcurrentBag<Artist> concurrentArtists = new ConcurrentBag<Artist>();
            List<Artist> modifiedArtists = new List<Artist>(artists);

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
                    files = new ConcurrentBag<FileInfo>(directoryInfo.GetFiles());
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
                            new ConcurrentBag<Album>(concurrentArtist.ArtistAlbums.ToImmutableList());
                        ConcurrentBag<Album> concurrentArtistAlbums = new ConcurrentBag<Album>(artistAlbums);
                        songs.Add(CuratedSong(concurrentArtist, track, concurrentArtistAlbums));
                    }

                    modifiedArtists.AddRange(MergeSongs(songs));

                    artists.ReplaceRangeArtists(MergeArtists(modifiedArtists).ToImmutableList());
                });
            }

            foreach (Artist artist in artists)
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
                Album albumExists = artist.ArtistAlbums.FirstOrDefault(x => x.Title == track.Album);
                Song songAlbumExists = new Song(track, albumExists, artist);

                return songAlbumExists;
            }

            Album albumNotExists = new Album(track);

            artist.ArtistAlbums.Add(albumNotExists);
            Song songAlbumNotExists = new Song(track, albumNotExists, artist);
            albumNotExists.AlbumSongCollection.Add(songAlbumNotExists);

            return songAlbumNotExists;
        }

        private static IEnumerable<Artist> MergeSongs(IEnumerable<Song> songs)
        {
            List<Artist> artists = new List<Artist>();

            List<Song> songList = songs.ToList();
            foreach (Song song in songList)
            {
                artists.AddUnique(song.Artist);
            }

            List<Album> albumsEnumerator = songList.Select(x => x.Album).DistinctBy(x => x.Title)
                .Where(x => !string.IsNullOrEmpty(x.ArtistName) && !string.IsNullOrEmpty(x.Title)).ToList();

            foreach (Album album in albumsEnumerator)
            {
                album.AlbumSongCollection = songList
                    .Where(x => x.AlbumTitle == album.Title && x.ArtistName == album.ArtistName).ToList();
            }

            foreach (Artist artist in artists)
            {
                artist.ArtistAlbums.Clear();
                ConcurrentBag<Album> albumsInArtist = new ConcurrentBag<Album>();
                foreach (Album album in albumsEnumerator.Where(album => album.ArtistName == artist.Name))
                {
                    artist.ArtistAlbums.Add(album);
                }

                ImmutableList<Album> artistAlbums = artist.ArtistAlbums.ToImmutableList();

                foreach (Album album in artistAlbums)
                {
                    if (!albumsEnumerator.Select(x => x.ArtistName).Contains(album.ArtistName))
                    {
                        albumsInArtist.Add(album);
                    }

                    albumsInArtist.AddUnique(albumsEnumerator.FirstOrDefault(x => x.Title == album.Title));
                }

                artist.ArtistAlbums = albumsInArtist;
            }

            return artists;
        }

        private static IEnumerable<Artist> MergeArtists(ICollection<Artist> artists)
        {
            List<Album> albums = new List<Album>();

            List<Album> mergedAlbums = new List<Album>();
            List<Artist> mergedArtists = new List<Artist>();

            //case where Artist has one or more albums

            foreach (Artist artist in artists)
            {
                albums.AddRange(artist.ArtistAlbums);
            }

            mergedAlbums.AddRange(albums);

            List<Album> distinctAlbums = mergedAlbums.DistinctBy(x => x?.Title).ToList();

            foreach (Album album in distinctAlbums)
            {
                album.AlbumSongCollection = albums
                    .SelectMany(z => z.AlbumSongCollection)
                    .Where(x => x.AlbumTitle == album.Title)
                    .DistinctBy(x => x.FilePath).ToList();
                foreach (Song song in album.AlbumSongCollection)
                {
                    Artist relevantArtist = artists.FirstOrDefault(x => x.Name == album.ArtistName);
                    if (relevantArtist == null)
                    {
                        continue;
                    }

                    song.Album = album;
                    Album relevantAlbum = relevantArtist.ArtistAlbums.FirstOrDefault(x => x.Title == album.Title);
                    if (relevantAlbum != null)
                    {
                        //relevantArtist.ArtistAlbums.Remove(relevantAlbum);
                    }
                    relevantArtist.ArtistAlbums.Add(album);
                    song.Artist = relevantArtist;
                    mergedArtists.AddUnique(relevantArtist);
                }
            }

            return mergedArtists;
        }

        public Song LoadIndividualSong(string path, IEnumerable<Artist> artists)
        {
            if (!IsSupportedSongExtension(path))
            {
                throw new Exception("This type of file isn't supported.");
            }

            Track track = new Track(path);

            List<Artist> artistEnumerator = artists.ToList();
            List<Artist> artistsList = artists.ToList();

            if (!artistEnumerator.Any())
            {
                Artist artist = new Artist(track);
                Album album = new Album(track);
                artist.ArtistAlbums.Add(album);
                Song song = new Song(track, album, artist);
                album.AlbumSongCollection.Add(song);

                artistsList.AddUnique(song.Artist);
                return song;
            }

            return artistEnumerator.Where(artist => artist.Name == track.Artist)
                .Select(artist =>
                {
                    Song curatedSong = CuratedSong(artist, track, new ConcurrentBag<Album>(artist.ArtistAlbums));
                    artistsList.AddUnique(curatedSong.Artist);
                    artists = artistsList;
                    return curatedSong;
                })
                .FirstOrDefault();
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
