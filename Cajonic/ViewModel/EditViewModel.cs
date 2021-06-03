using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using Cajonic.Model;
using Meziantou.Framework.WPF.Collections;
using ATL;

// ReSharper disable InconsistentNaming

namespace Cajonic.ViewModel
{
    public class EditViewModel
    {
        protected int? mTrackNumber;
        protected int? mDiscNumber;
        protected string mArtistName;
        protected string mAlbumTitle;
        protected string mGenre;
        protected string mAlbumArtist;
        protected int? mYear;
        protected string mTitle;

        protected int mOriginalNumberOfArtists = 0;
        protected int mOriginalNumberOfAlbums = 0;
        protected int mOriginalNumberOfGenres = 0;
        protected int mOriginalNumberOfAlbumArtists = 0;
        protected int mOriginalNumberOfYears = 0;
        protected int mOriginalNumberOfDiscs = 0;

        protected int mNumberOfArtists;
        protected int mNumberOfAlbums;
        protected int mNumberOfGenres;
        protected int mNumberOfAlbumArtists;
        protected int mNumberOfYears;
        protected int mNumberOfDiscs;

        private readonly int mOriginalSelectedSongsCount;

        private readonly ConcurrentObservableCollection<Song> mSelectedSongs;
        private readonly ConcurrentObservableCollection<Song> mAllSongs;
        private readonly ConcurrentObservableCollection<Artist> mArtists;
        private readonly ConcurrentObservableCollection<Album> mAlbums;
        private readonly Action mCloseWindow;

        protected EditViewModel(ConcurrentObservableCollection<Song> songs,
            ConcurrentObservableCollection<Song> allSongs,
            ConcurrentObservableCollection<Artist> artists,
            ConcurrentObservableCollection<Album> albums, Action closeWindow)
        {
            mSelectedSongs = songs;
            mAllSongs = allSongs;

            mOriginalSelectedSongsCount = mSelectedSongs.Count;

            Song selectedSong = mSelectedSongs.First();
            mTrackNumber = selectedSong.TrackNumber;
            mTitle = selectedSong.Title;
            mDiscNumber = selectedSong.DiscNumber;
            mArtistName = selectedSong.ArtistName;
            mAlbumTitle = selectedSong.AlbumTitle;
            mGenre = selectedSong.Genre;
            mAlbumArtist = selectedSong.AlbumArtist;
            mYear = selectedSong.Year;

            mArtists = artists;
            mAlbums = albums;
            mCloseWindow = closeWindow;
        }

        protected void Ok()
        {
            Mouse.OverrideCursor = Cursors.Wait;

            foreach (Song song in mAllSongs.Where(x => mSelectedSongs.Contains(x)))
            {
                Song songToCopy = new()
                {
                    Genre = mOriginalNumberOfGenres > 1 ? mNumberOfGenres > 1 ? song.Genre :
                        string.IsNullOrEmpty(mGenre) ? null :
                        string.IsNullOrWhiteSpace(mGenre.Trim()) ? null : mGenre.Trim() :
                        string.IsNullOrEmpty(mGenre) ? null :
                        string.IsNullOrWhiteSpace(mGenre.Trim()) ? null : mGenre.Trim(),
                    AlbumArtist = mOriginalNumberOfAlbumArtists > 1 ? mNumberOfAlbumArtists > 1 ? song.AlbumArtist :
                        string.IsNullOrEmpty(mAlbumArtist) ? null :
                        string.IsNullOrWhiteSpace(mAlbumArtist.Trim()) ? null : mAlbumArtist.Trim() :
                        string.IsNullOrEmpty(mAlbumArtist) ? null :
                        string.IsNullOrWhiteSpace(mAlbumArtist.Trim()) ? null : mAlbumArtist.Trim(),
                    AlbumTitle = mOriginalNumberOfAlbums > 1 ? mNumberOfAlbums > 1 ? song.AlbumTitle :
                        string.IsNullOrWhiteSpace(mAlbumTitle.Trim()) ? Album.UnknownAlbum : mAlbumTitle.Trim() :
                        string.IsNullOrWhiteSpace(mAlbumTitle.Trim()) ? Album.UnknownAlbum : mAlbumTitle.Trim(),
                    ArtistName = mOriginalNumberOfArtists > 1 ? mNumberOfArtists > 1 ? song.ArtistName :
                        string.IsNullOrWhiteSpace(mArtistName.Trim()) ? Artist.UnknownArtist : mArtistName.Trim() :
                        string.IsNullOrWhiteSpace(mArtistName.Trim()) ? Artist.UnknownArtist : mArtistName.Trim(),
                    Title = mOriginalSelectedSongsCount > 1 ? song.Title :
                        string.IsNullOrEmpty(mTitle) ? null :
                        string.IsNullOrWhiteSpace(mTitle.Trim()) ? null : mTitle.Trim(),
                    DiscNumber = mOriginalNumberOfDiscs > 1 ? mNumberOfDiscs > 1 ? song.DiscNumber :
                        mDiscNumber == 0 ? null : mDiscNumber :
                        mDiscNumber == 0 ? null : mDiscNumber,
                    Year = mOriginalNumberOfYears > 1 ? mNumberOfYears > 1 ? song.Year :
                        mYear == 0 ? null : mYear :
                        mYear == 0 ? null : mYear,
                    TrackNumber = mOriginalSelectedSongsCount > 1 ? song.TrackNumber :
                        mTrackNumber == 0 ? null : mTrackNumber
                };

                (Album newAlbum, Artist newArtist) = GetNewAlbumAndArtist(songToCopy);

                song.EditSong(songToCopy, newArtist, newAlbum, mAlbums, mArtists);

                int relevantIndex = mAllSongs.IndexOf(song);
                mAllSongs.Remove(mAllSongs.First(x => x == song));
                mAllSongs.Insert(relevantIndex, song);

                Track track = new(song.FilePath)
                {
                    Title = song.Title ?? string.Empty,
                    Year = song.Year ?? 0,
                    AlbumArtist = song.AlbumArtist ?? string.Empty,
                    Genre = song.Genre ?? string.Empty,
                    DiscNumber = song.DiscNumber ?? 0,
                    Album = song.AlbumTitle,
                    Artist = song.ArtistName,
                    TrackNumber = song.TrackNumber ?? 0
                };

                if (song.DiscNumber != null && track.DiscTotal < song.DiscNumber)
                {
                    track.DiscTotal = song.DiscNumber.Value;
                }

                if (song.TrackNumber != null && track.TrackTotal < song.TrackNumber)
                {
                    track.TrackTotal = song.TrackNumber.Value;
                }

                song.HasBeenEdited = false;
                track.Save();
            }

            foreach (Artist artist in mArtists.Where(x => x.IsSerialization || x.IsDestruction))
            {
                if (artist.IsSerialization && !artist.IsDestruction)
                {
                    artist.SerializeArtistAsync();
                }

                if (!artist.IsDestruction)
                {
                    continue;
                }

                artist.DestroySerializedArtist();
                mArtists.Remove(artist);
            }

            Mouse.OverrideCursor = Cursors.Arrow;
            mCloseWindow.Invoke();
        }

        private KeyValuePair<Album, Artist> GetNewAlbumAndArtist(Song song)
        {
            IEnumerable<Artist> artistList = mArtists.ToList();
            Artist newArtist;
            Album newAlbum;

            List<string> nameList = artistList.Select(x => x.Name).ToList();
            if (!nameList.Any(x => x.Equals(string.IsNullOrEmpty(mArtistName) ? Artist.UnknownArtist : mArtistName, 
                StringComparison.InvariantCultureIgnoreCase)))
            {
                newArtist = new Artist(song);
                mArtists.Add(newArtist);
            }
            else
            {
                newArtist = artistList.First(x =>
                    x.Name.Equals(string.IsNullOrEmpty(mArtistName) ? Artist.UnknownArtist : mArtistName, StringComparison.InvariantCultureIgnoreCase));
                song.ArtistName = newArtist.Name;
            }

            if (newArtist.ArtistAlbums.Count == 0 ||
                !newArtist.ArtistAlbums.Values.Select(x => x.Title).Contains(string.IsNullOrEmpty(mAlbumTitle) 
                    ? Album.UnknownAlbum : mAlbumTitle, StringComparer.InvariantCultureIgnoreCase))
            {
                newAlbum = new Album(song);
                newArtist.ArtistAlbums.TryAdd(newAlbum.Title, newAlbum);
                mAlbums.Add(newAlbum);
            }
            else
            {
                newAlbum = newArtist.ArtistAlbums.First(x => x.Value.Title.Equals(string.IsNullOrEmpty(mAlbumTitle) 
                    ? Album.UnknownAlbum : mAlbumTitle, StringComparison.InvariantCultureIgnoreCase)).Value;
                song.AlbumTitle = newAlbum.Title;
            }

            return new KeyValuePair<Album, Artist>(newAlbum, newArtist);
        }


        protected void Cancel()
        {
            mCloseWindow.Invoke();
        }
    }
}