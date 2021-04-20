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
                        List<Album> artistAlbums = concurrentArtist.ArtistAlbums.ToList();
                        ConcurrentBag<Album> concurrentArtistAlbums = new ConcurrentBag<Album>(artistAlbums);
                        songs.Add(CuratedSong(concurrentArtist, track, concurrentArtistAlbums));
                    }
                });
            }

            //TODO : FIX MERGE SONGS. DOESN'T ACCEPT 2 ALBUMS OF THE SAME ARTIST AT THE SAME TIME
            modifiedArtists.AddRange(MergeSongs(songs));
            artists = MergeArtists(modifiedArtists).ToImmutableList();

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
            List<Album> albums = new List<Album>();

            List<Song> songList = songs.ToList();
            foreach (Song song in songList)
            {
                artists.AddUnique(song.Artist);
            }

            foreach (Album album in artists.SelectMany(song => song.ArtistAlbums))
            {
                albums.AddUnique(album);
            }

            foreach (Album album in albums)
            {
                album.AlbumSongCollection = songList
                    .Where(x => x.AlbumTitle == album.Title && x.ArtistName == album.ArtistName).ToList();
            }

            foreach (Artist artist in artists)
            {
                List<Album> albumsInArtist = new List<Album>();
                foreach (Album album in artist.ArtistAlbums)
                {
                    if (!albums.Select(x => x.ArtistName).Contains(album.ArtistName))
                    {
                        albumsInArtist.Add(album);
                    }

                    albumsInArtist.AddUnique(albums.FirstOrDefault(x => x.Title == album.Title));
                }

                artist.ArtistAlbums = albumsInArtist;
            }

            foreach (Song song in songList)
            {
                song.Album = albums.FirstOrDefault(x => x.Title == song.AlbumTitle && song.ArtistName == x.ArtistName);
                song.Artist = artists.FirstOrDefault(x => x.Name == song.ArtistName);
            }
            
            return artists;
        }

        private static IEnumerable<Artist> MergeArtists(ICollection<Artist> artists)
        {
            List<Album> albums = new List<Album>();

            List<Album> mergedAlbums = new List<Album>();
            List<Artist> mergedArtists = new List<Artist>();

            foreach (Artist artist in artists)
            {
                albums.AddRange(artist.ArtistAlbums);
            }

            mergedAlbums.AddRange(albums);

            foreach (Album album in mergedAlbums.DistinctBy(x => x.Title))
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
                        relevantArtist.ArtistAlbums.Remove(relevantAlbum);
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
