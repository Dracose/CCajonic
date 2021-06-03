using System;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Cajonic.Model;
using Cajonic.Services;
using Meziantou.Framework.WPF.Collections;

namespace Cajonic.ViewModel
{
    public sealed class EditSongsViewModel : EditViewModel, INotifyPropertyChanged
    {
        public ICommand OkButton { get; }
        public ICommand CancelButton { get; }
        public ICommand EditedArtistCommand { get; }
        public ICommand EditedAlbumCommand { get; }
        public ICommand EditedAlbumArtistCommand { get; }
        public ICommand EditedGenreCommand { get; }
        public ICommand EditedYearCommand { get; }
        public ICommand EditedDiscsCommand { get; }


        public EditSongsViewModel(ConcurrentObservableCollection<Song> songs,
            ConcurrentObservableCollection<Song> allSongs, ConcurrentObservableCollection<Artist> artists,
            ConcurrentObservableCollection<Album> albums, Action closeWindow) : base(songs, allSongs, artists, albums,
            closeWindow)
        {
            mOriginalDisplayYear = !songs.Select(x => x.Year).Distinct().Skip(1).Any()
                ? songs.Select(x => x.Year).First().ToString()
                : null;
            Year = !songs.Select(x => x.Year).Distinct().Skip(1).Any() ? songs.Select(x => x.Year).First() : null;
            NumberOfYears = mOriginalNumberOfYears = songs.Select(x => x.Year).Distinct().Count();

            if (!songs.Select(x => x.AlbumTitle).Distinct().Skip(1).Any())
            {
                AlbumTitle = mOriginalAlbum = songs.Select(x => x.AlbumTitle).First();
                NumberOfAlbums = mOriginalNumberOfAlbums = songs.Select(x => x.AlbumTitle).Distinct().Count();
                mOriginalDisplayDisc = !songs.Select(x => x.DiscNumber).Distinct().Skip(1).Any()
                    ? songs.Select(x => x.DiscNumber).First().ToString()
                    : null;
                DiscNumber = !songs.Select(x => x.DiscNumber).Distinct().Skip(1).Any()
                    ? songs.Select(x => x.DiscNumber).First()
                    : null;
                NumberOfDiscs = mOriginalNumberOfDiscs = songs.Select(x => x.DiscNumber).Distinct().Count();
            }
            else
            {
                AlbumTitle = mOriginalAlbum = null;
                NumberOfAlbums = mOriginalNumberOfAlbums = songs.Select(x => x.AlbumTitle).Distinct().Count();
                mOriginalDisplayDisc = !songs.Select(x => x.DiscNumber).Distinct().Skip(1).Any()
                    ? songs.Select(x => x.DiscNumber).First().ToString()
                    : null;
                DiscNumber = !songs.Select(x => x.DiscNumber).Distinct().Skip(1).Any()
                    ? songs.Select(x => x.DiscNumber).First()
                    : null;
                NumberOfDiscs = mOriginalNumberOfDiscs = songs.Select(x => x.DiscNumber).Distinct().Count();
            }

            ArtistName = mOriginalArtist = !songs.Select(x => x.ArtistName).Distinct().Skip(1).Any()
                ? songs.Select(x => x.ArtistName).First()
                : null;
            NumberOfArtists = mOriginalNumberOfArtists = songs.Select(x => x.ArtistName).Distinct().Count();

            Genre = mOriginalGenre = !songs.Select(x => x.Genre).Distinct().Skip(1).Any()
                ? songs.Select(x => x.Genre).First() ?? string.Empty
                : null;
            if (!songs.Select(x => x.Genre).All(string.IsNullOrEmpty))
            {
                NumberOfGenres = mOriginalNumberOfGenres = songs.Select(x => x.Genre).Distinct().Count();
            }
            else
            {
                NumberOfGenres = mOriginalNumberOfGenres = songs.Select(x => x.Genre)
                    .Where(x => !string.IsNullOrEmpty(x)).Distinct().Count();
            }

            AlbumArtist = mOriginalAlbumArtist = !songs.Select(x => x.AlbumArtist).Distinct().Skip(1).Any()
                ? songs.Select(x => x.AlbumArtist).First() ?? string.Empty
                : null;
            if (!songs.Select(x => x.AlbumArtist).All(string.IsNullOrEmpty))
            {
                NumberOfAlbumArtists = mOriginalNumberOfAlbumArtists = songs.Select(x => x.AlbumArtist).Distinct().Count();
            }
            else
            {
                NumberOfAlbumArtists = mOriginalNumberOfAlbumArtists = songs.Select(x => x.AlbumArtist)
                    .Where(x => !string.IsNullOrEmpty(x)).Distinct().Count();
            }
            
            mTrackNumber = null;
            mTitle = null;

            EditedArtistCommand = new RelayCommand(EditedArtist, true);
            EditedAlbumCommand = new RelayCommand(EditedAlbum, true);
            EditedAlbumArtistCommand = new RelayCommand(EditedAlbumArtist, true);
            EditedGenreCommand = new RelayCommand(EditedGenre, true);
            EditedYearCommand = new RelayCommand(EditedYear, true);
            EditedDiscsCommand = new RelayCommand(EditedDiscs, true);
            OkButton = new CommandHandler(Ok, () => true);
            CancelButton = new CommandHandler(Cancel, () => true);
        }

        public bool IsMultipleGenres => mNumberOfGenres > 1;
        public bool IsMultipleAlbumArtists => mNumberOfAlbumArtists > 1;
        public bool IsMultipleYears => mNumberOfYears > 1;
        public bool IsMultipleDiscs => mNumberOfDiscs > 1;
        public bool IsMultipleArtists => mNumberOfArtists > 1;
        public bool IsMultipleAlbums => mNumberOfAlbums > 1;

        private bool mIsArtistTickVisible;
        private bool mIsAlbumTickVisible;
        private bool mIsYearTickVisible;
        private bool mIsDiscTickVisible;
        private bool mIsAlbumArtistTickVisible;
        private bool mIsGenreTickVisible;

        private readonly string mOriginalArtist;
        private readonly string mOriginalAlbum;
        private readonly string mOriginalDisplayYear;
        private readonly string mOriginalAlbumArtist;
        private readonly string mOriginalDisplayDisc;
        private readonly string mOriginalGenre;

        public bool IsGenreTickVisible
        {
            get => mIsGenreTickVisible;
            set
            {
                mIsGenreTickVisible = value;
                OnPropertyChanged();
            }
        }

        public bool IsAlbumArtistTickVisible
        {
            get => mIsAlbumArtistTickVisible;
            set
            {
                mIsAlbumArtistTickVisible = value;
                OnPropertyChanged();
            }
        }

        public bool IsDiscTickVisible
        {
            get => mIsDiscTickVisible;
            set
            {
                mIsDiscTickVisible = value;
                OnPropertyChanged();
            }
        }

        public bool IsYearTickVisible
        {
            get => mIsYearTickVisible;
            set
            {
                mIsYearTickVisible = value;
                OnPropertyChanged();
            }
        }

        public bool IsAlbumTickVisible
        {
            get => mIsAlbumTickVisible;
            set
            {
                mIsAlbumTickVisible = value;
                OnPropertyChanged();
            }
        }

        public bool IsArtistTickVisible
        {
            get => mIsArtistTickVisible;
            set
            {
                mIsArtistTickVisible = value;
                OnPropertyChanged();
            }
        }

        private int NumberOfArtists
        {
            get => mNumberOfArtists;
            set
            {
                mNumberOfArtists = value;
                OnPropertyChanged(nameof(NumberOfArtists));
                OnPropertyChanged(nameof(IsMultipleArtists));
            }
        }

        private int NumberOfAlbums
        {
            get => mNumberOfAlbums;
            set
            {
                mNumberOfAlbums = value;
                OnPropertyChanged(nameof(NumberOfAlbums));
                OnPropertyChanged(nameof(IsMultipleAlbums));
            }
        }

        private int NumberOfAlbumArtists
        {
            get => mNumberOfAlbumArtists;
            set
            {
                mNumberOfAlbumArtists = value;
                OnPropertyChanged(nameof(NumberOfAlbumArtists));
                OnPropertyChanged(nameof(IsMultipleAlbumArtists));
            }
        }

        private int NumberOfGenres
        {
            get => mNumberOfGenres;
            set
            {
                mNumberOfGenres = value;
                OnPropertyChanged(nameof(NumberOfGenres));
                OnPropertyChanged(nameof(IsMultipleGenres));
            }
        }

        private int NumberOfYears
        {
            get => mNumberOfYears;
            set
            {
                mNumberOfYears = value;
                OnPropertyChanged(nameof(NumberOfYears));
                OnPropertyChanged(nameof(IsMultipleYears));
            }
        }

        private int NumberOfDiscs
        {
            get => mNumberOfDiscs;
            set
            {
                mNumberOfDiscs = value;
                OnPropertyChanged(nameof(NumberOfDiscs));
                OnPropertyChanged(nameof(IsMultipleDiscs));
            }
        }

        private int? Year
        {
            set
            {
                mYear = value;
                OnPropertyChanged(nameof(DisplayYear));
            }
        }

        private int? DiscNumber
        {
            set
            {
                mDiscNumber = value;
                OnPropertyChanged(nameof(DisplayDiscNumber));
            }
        }

        public string AlbumArtist
        {
            get => mAlbumArtist;
            set
            {
                mAlbumArtist = value;
                if (mAlbumArtist != mOriginalAlbumArtist)
                {
                    NumberOfAlbumArtists = 1;
                }

                if (NumberOfAlbumArtists == 1 && mOriginalAlbumArtist == null)
                {
                    IsAlbumArtistTickVisible = true;
                }
                else if (NumberOfAlbumArtists == 1 && mAlbumArtist != mOriginalAlbumArtist)
                {
                    IsAlbumArtistTickVisible = true;
                }
                else if (NumberOfAlbumArtists == 1 && AlbumArtist == mOriginalAlbumArtist)
                {
                    IsAlbumArtistTickVisible = false;
                }

                OnPropertyChanged(nameof(AlbumArtist));
            }
        }

        public string Genre
        {
            get => mGenre;
            set
            {
                mGenre = value;
                if (mGenre != mOriginalGenre)
                {
                    NumberOfGenres = 1;
                }

                if (NumberOfGenres == 1 && mOriginalGenre == null)
                {
                    IsGenreTickVisible = true;
                }
                else if (NumberOfGenres == 1 && mGenre != mOriginalGenre)
                {
                    IsGenreTickVisible = true;
                }
                else if (NumberOfGenres == 1 && mGenre == mOriginalGenre)
                {
                    IsGenreTickVisible = false;
                }

                OnPropertyChanged(nameof(Genre));
            }
        }

        public string AlbumTitle
        {
            get => mAlbumTitle;
            set
            {
                mAlbumTitle = value;
                if (mAlbumTitle != mOriginalAlbum)
                {
                    NumberOfAlbums = 1;
                }

                if (NumberOfAlbums == 1 && mOriginalAlbum == null)
                {
                    IsAlbumTickVisible = true;
                }
                else if (NumberOfAlbums == 1 && mAlbumTitle != mOriginalAlbum)
                {
                    IsAlbumTickVisible = true;
                }
                else if (NumberOfAlbums == 1 && mAlbumTitle == mOriginalAlbum)
                {
                    IsAlbumTickVisible = false;
                }

                OnPropertyChanged(nameof(AlbumTitle));
            }
        }

        public string ArtistName
        {
            get => mArtistName;
            set
            {
                mArtistName = value;
                if (mArtistName != mOriginalArtist)
                {
                    NumberOfArtists = 1;
                }

                if (NumberOfArtists == 1 && mOriginalArtist == null)
                {
                    IsArtistTickVisible = true;
                }
                else if (NumberOfArtists == 1 && mArtistName != mOriginalArtist)
                {
                    IsArtistTickVisible = true;
                }
                else if (NumberOfArtists == 1 && mArtistName == mOriginalArtist)
                {
                    IsArtistTickVisible = false;
                }

                OnPropertyChanged(nameof(ArtistName));
            }
        }

        public string DisplayDiscNumber
        {
            get => mDiscNumber.ToString();
            set
            {
                DiscNumber = value.ToVisualNullableInt(mDiscNumber);

                if (DisplayDiscNumber != mOriginalDisplayDisc)
                {
                    NumberOfDiscs = 1;
                }

                if (mDiscNumber == 0)
                {
                    if (mOriginalDisplayDisc == null)
                    {
                        IsDiscTickVisible = true;
                        return;
                    }

                    IsDiscTickVisible = false;
                    return;
                }

                if (NumberOfDiscs == 1 && mOriginalDisplayDisc == null)
                {
                    IsDiscTickVisible = true;
                }
                else if (NumberOfDiscs == 1 && DisplayDiscNumber != mOriginalDisplayDisc)
                {
                    IsDiscTickVisible = true;
                }
                else if (NumberOfDiscs == 1 && DisplayDiscNumber == mOriginalDisplayDisc)
                {
                    IsDiscTickVisible = false;
                }
            }
        }

        public string DisplayYear
        {
            get => mYear.ToString();
            set
            {
                Year = value.ToVisualNullableInt(mYear);

                if (DisplayYear != mOriginalDisplayYear)
                {
                    NumberOfYears = 1;
                }

                if (mYear == 0)
                {
                    if (mOriginalDisplayYear == null)
                    {
                        IsYearTickVisible = true;
                        return;
                    }

                    IsYearTickVisible = false;
                    return;
                }

                if (NumberOfYears == 1 && mOriginalDisplayYear == null)
                {
                    IsYearTickVisible = true;
                }
                else if (NumberOfYears == 1 && DisplayYear != mOriginalDisplayYear)
                {
                    IsYearTickVisible = true;
                }
                else if (NumberOfYears == 1 && DisplayYear == mOriginalDisplayYear)
                {
                    IsYearTickVisible = false;
                }
            }
        }

        private void EditedArtist(object parameter)
        {
            if (NumberOfArtists == 1)
            {
                return;
            }

            KeyEventArgs eventArgs = (KeyEventArgs) parameter;

            if (eventArgs.Key is Key.Back or Key.BrowserBack or Key.Space)
            {
                NumberOfArtists = 1;
                IsArtistTickVisible = true;
            }
        }

        private void EditedAlbum(object parameter)
        {
            if (NumberOfAlbums == 1)
            {
                return;
            }

            KeyEventArgs eventArgs = (KeyEventArgs) parameter;

            if (eventArgs.Key is Key.Back or Key.BrowserBack or Key.Space)
            {
                NumberOfAlbums = 1;
                IsAlbumTickVisible = true;
            }
        }

        private void EditedDiscs(object parameter)
        {
            if (NumberOfDiscs == 1)
            {
                return;
            }

            KeyEventArgs eventArgs = (KeyEventArgs) parameter;

            if (eventArgs.Key is Key.Back or Key.BrowserBack)
            {
                NumberOfDiscs = 1;
                IsDiscTickVisible = true;
            }
        }

        private void EditedYear(object parameter)
        {
            if (NumberOfYears == 1)
            {
                return;
            }

            KeyEventArgs eventArgs = (KeyEventArgs) parameter;

            if (eventArgs.Key is Key.Back or Key.BrowserBack)
            {
                NumberOfYears = 1;
                IsYearTickVisible = true;
            }
        }

        private void EditedGenre(object parameter)
        {
            if (NumberOfGenres == 1)
            {
                return;
            }

            KeyEventArgs eventArgs = (KeyEventArgs) parameter;

            if (eventArgs.Key is Key.Back or Key.BrowserBack or Key.Space)
            {
                NumberOfGenres = 1;
                IsGenreTickVisible = true;
            }
        }

        private void EditedAlbumArtist(object parameter)
        {
            if (NumberOfAlbumArtists == 1)
            {
                return;
            }

            KeyEventArgs eventArgs = (KeyEventArgs) parameter;

            if (eventArgs.Key is Key.Back or Key.BrowserBack or Key.Space)
            {
                NumberOfAlbumArtists = 1;
                IsAlbumArtistTickVisible = true;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}