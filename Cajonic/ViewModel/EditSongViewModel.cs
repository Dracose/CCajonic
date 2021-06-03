using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using Cajonic.Model;
using Cajonic.Services;
using Meziantou.Framework.WPF.Collections;

namespace Cajonic.ViewModel
{
    public class EditSongViewModel : EditViewModel, INotifyPropertyChanged
    {
        public EditSongViewModel(ConcurrentObservableCollection<Song> songs,
            ConcurrentObservableCollection<Song> allSongs, ConcurrentObservableCollection<Artist> artists,
            ConcurrentObservableCollection<Album> albums, Action closeWindow) : base(songs, allSongs, artists, albums,
            closeWindow)
        {
            OkButton = new CommandHandler(Ok, () => true);
            CancelButton = new CommandHandler(Cancel, () => true);
        }

        public ICommand OkButton { get; }
        public ICommand CancelButton { get; }

        public string Title
        {
            get => mTitle;
            set
            {
                mTitle = value;
                OnPropertyChanged(nameof(Title));
            }
        }

        public int? Year
        {
            get => mYear;
            set
            {
                mYear = value;
                OnPropertyChanged(nameof(Year));
            }
        }

        public string AlbumArtist
        {
            get => mAlbumArtist;
            set
            {
                mAlbumArtist = value;
                OnPropertyChanged(nameof(AlbumArtist));
            }
        }

        public string Genre
        {
            get => mGenre;
            set
            {
                mGenre = value;
                OnPropertyChanged(nameof(Genre));
            }
        }

        public string AlbumTitle
        {
            get => mAlbumTitle;
            set
            {
                mAlbumTitle = value;
                OnPropertyChanged(nameof(AlbumTitle));
            }
        }

        public string ArtistName
        {
            get => mArtistName;
            set
            {
                mArtistName = value;
                OnPropertyChanged(nameof(ArtistName));
            }
        }

        public int? DiscNumber
        {
            get => mDiscNumber;
            set
            {
                mDiscNumber = value;
                OnPropertyChanged(nameof(DiscNumber));
            }
        }

        public int? TrackNumber
        {
            get => mTrackNumber;
            set
            {
                mTrackNumber = value;
                OnPropertyChanged(nameof(TrackNumber));
            }
        }

        public string DisplayDiscNumber
        {
            get => mDiscNumber.ToString();
            set => DiscNumber = value.ToVisualNullableInt(mDiscNumber);
        }

        public string DisplayYear
        {
            get => mYear.ToString();
            set => Year = value.ToVisualNullableInt(mYear);
        }

        public string DisplayTrackNumber
        {
            get => mTrackNumber.ToString();
            set => TrackNumber = value.ToVisualNullableInt(mTrackNumber);
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }
    }
}