using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using Cajonic.Services;
using Cajonic.Model;
using System.Windows.Input;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ATL;
using MahApps.Metro.Controls;
using Meziantou.Framework.WPF.Collections;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace Cajonic.ViewModel
{
    public class CajonicViewModel : INotifyPropertyChanged, IFileDragDropTarget, IMusicPlayer, IWindowShowService
    {
        //serialize file paths and read a list of filePaths from this, then call reserialize.
        public event PropertyChangedEventHandler PropertyChanged;
        private readonly System.Timers.Timer mIncrementPlayingProgress;
        private readonly System.Timers.Timer mFindSongEnd;
        private int mPlayingIndex;
        private readonly IMusicPlayer mMusicPlayer;
        private double mSeconds;
        private static readonly ISongLoader sSongLoader = new SongLoader();
        private const string BasicElapsedTime = "00:00 / " + "00:00";
        public const string MixedString = "Mixed";
        private string mLastHeaderClicked;
        private ListSortDirection mLastDirection = ListSortDirection.Ascending;

        public ICommand AddToQueueCommand { get; private set; }
        public ICommand ClearQueueCommand { get; private set; }
        public ICommand PlaySong { get; }
        public ICommand PlaySongRelay { get; }
        public ICommand PlayPauseStopLastNext { get; }
        public ICommand PauseSong { get; }
        public ICommand StopSong { get; }
        public ICommand FastForwardCommand { get; }
        public ICommand RewindCommand { get; }
        public ICommand SortGrid { get; }
        
        public string OpenFileText { get; }
        
        public static bool IsOpenFileVisible => RuntimeInformation.IsOSPlatform(OSPlatform.Windows) ||
                                                RuntimeInformation.IsOSPlatform(OSPlatform.OSX);
        
        public ICommand OpenFile { get; }
        public ICommand EditSong { get; }
        public ICommand DeleteSong { get; }

        public CajonicViewModel()
        {
            if (DesignerProperties.GetIsInDesignMode(new DependencyObject()))
            {
                return;
            }

            mSeconds = 0;

            mPlayingSong = new Song();
            QueueInfo = "";
            mMusicPlayer = this;
            ElapsedTime = "00:00 / 00:00";
            mIncrementPlayingProgress = new System.Timers.Timer
            {
                Interval = 1000
            };

            mIncrementPlayingProgress.Elapsed += (sender, e) =>
            {
                mSeconds += 1000;
                PlayingProgress += 1000;

                string displayProgress = TimeSpan.FromMilliseconds(mSeconds).ToString("mm\\:ss");
                ElapsedTime = displayProgress + " / " + PlayingSong.DisplayDuration;
            };

            mFindSongEnd = new System.Timers.Timer
            {
                Interval = 1
            };
            
            mFindSongEnd.Elapsed += (sender, e) =>
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    if (!mMusicPlayer.IsDone())
                    {
                        return;
                    }

                    Debug.WriteLine("Done");
                    if (++mPlayingIndex < SongList.Count)
                    {
                        SelectedSong = SongList[mPlayingIndex];
                        PlaySongAction();
                    }
                    else
                    {
                        StopSongAction();
                    }
                });
            };

            DeserializeArtists();

            OpenFileText = "Show file in " +
                           (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)
                               ? "Windows Explorer"
                               : RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                                   ? "Finder"
                                   : "");

            PlaySongRelay = new RelayCommand(PlaySongAction, true);
            SortGrid = new RelayCommand(SortGridAction, true);
            SelectedItemsChangedCommand = new RelayCommand(SelectedItemsChanged, true);
            PlayPauseStopLastNext = new RelayCommand(PlayPauseStopLastNextAction, true);
            OpenFile = new CommandHandler(OpenInExplorerAction, () => 
                mSelectedSongs.Count == 1 && SelectedSong != null && IsOpenFileVisible);
            DeleteSong = new CommandHandler(DeleteSongFromLibraryAction, () => true);
            EditSong = new CommandHandler(ShowEditWindowAction, () => true);  
            PlaySong = new CommandHandler(PlaySongAction, () => mSelectedSongs.Count == 1 && SelectedSong != null);
            PauseSong = new CommandHandler(PauseSongAction, () => true);
            StopSong = new CommandHandler(StopSongAction, () => true);
            FastForwardCommand = new CommandHandler(FastForwardAction, () => true);
            RewindCommand = new CommandHandler(RewindAction, () => true);

            SelectedSong = null;
            PlayingSong = null;
            PlayingProgress = 0;
        }

        private void DeserializeArtists()
        {
            DirectoryInfo knownArtistDictionary = new(Artist.ArtistDirectory);
            if (!knownArtistDictionary.Exists)
            {
                return;
            }

            string[] files = Directory.GetFiles(knownArtistDictionary.FullName, "*.bin", SearchOption.AllDirectories);

            foreach (string path in files)
            {
                Artists.Add(Artist.DeserializeArtistHelperStatic(path));
            }

            foreach (Artist artist in Artists)
            {
                foreach (Album album in artist.ArtistAlbums.Values.ToList())
                {
                    if (!album.IsCompilation)
                    {
                        Albums.Add(album);
                    }

                    SongList.AddUniqueRange(album.AllSongs);
                }
            }
        }

        public string QueueFilePath { get; set; }

        public bool IsSongPlaying => PlayingSong != null;
        
        public ConcurrentObservableCollection<Song> SongList { get; set; } = new();
        
        private ConcurrentObservableCollection<Artist> Artists { get; set; } = new();

        private ConcurrentObservableCollection<Album> Albums { get; set; } = new();

        private string mQueueInfo;

        public string QueueInfo
        {
            get => mQueueInfo;
            set
            {
                mQueueInfo = value;
                OnPropertyChanged(nameof(QueueInfo));
            }
        }

        public string ArtistAlbumInfo =>
            PlayingSong != null
                ? PlayingSong.Year != null
                    ? PlayingSong.Artist.Name + " - " + PlayingSong.Album.Title + " [" + PlayingSong.Year.Value +
                      "]"
                    : PlayingSong.Artist.Name + " - " + PlayingSong.Album.Title
                : string.Empty;

        public string TrackTitleInfo =>
            PlayingSong != null ? PlayingSong.TrackNumber == null
                    ? PlayingSong.Title
                    : PlayingSong.TrackNumber + ". " + PlayingSong.Title
                : string.Empty;

        private Song mSelectedSong;

        public Song SelectedSong
        {
            get => mSelectedSong;
            set
            {
                if (mSelectedSong != null)
                {
                    mSelectedSong.ByteArtwork = null;
                }

                mSelectedSong = value;
                OnPropertyChanged(nameof(SelectedSong));
            }
        }

        private readonly ConcurrentObservableCollection<Song> mSelectedSongs = new();

        public RelayCommand SelectedItemsChangedCommand { get; }

        private void SelectedItemsChanged(object parameter)
        {
            mSelectedSongs.Clear();
            
            if (parameter != null) 
            {
                foreach (Song item in (IList)parameter)
                {
                    mSelectedSongs.Add(item);
                }
            }
        
            OnPropertyChanged(nameof(mSelectedSongs));
        }

        private void PlaySongAction(object parameter)
        {
            MouseButtonEventArgs eventArgs = (MouseButtonEventArgs) parameter;
            if (((FrameworkElement) eventArgs.OriginalSource).DataContext is Song) 
            {
                PlaySongAction();
            }
            
        }

        private void SortGridAction(object parameter)
        {
            ListSortDirection direction;
            string headerClicked = (string) parameter;

            headerClicked = headerClicked switch
            {
                "Track" => "TrackNumber",
                "Album Artist" => "AlbumArtist",
                "Artist" => "ArtistName",
                "Album" => "AlbumTitle",
                _ => headerClicked
            };

            if (headerClicked != mLastHeaderClicked)
            {
                direction = ListSortDirection.Ascending;
            }
            else
            {
                direction = mLastDirection == ListSortDirection.Ascending
                    ? ListSortDirection.Descending
                    : ListSortDirection.Ascending;
                if ((string) parameter == "ArtistName")
                {
                    direction = ListSortDirection.Ascending;
                }
            }

            Task task = new (() =>
            {
                IEnumerable<Song> something = (direction == ListSortDirection.Ascending ? 
                    SongList.OrderBy(headerClicked) : SongList.OrderByDescending(headerClicked));

                SongList = new ConcurrentObservableCollection<Song>();
                
                SongList.AddUniqueRange(something);

                OnPropertyChanged(nameof(SongList));
            });
            
            task.Start();

            mLastHeaderClicked = headerClicked;
            mLastDirection = direction;
        }

        private Song mPlayingSong;

        public Song PlayingSong
        {
            get => mPlayingSong;
            set
            {
                mPlayingSong = value;
                OnPropertyChanged(nameof(PlayingSong));
                OnPropertyChanged(nameof(IsSongPlaying));
            }
        }

        private int mSelectedIndex;

        public int SelectedIndex
        {
            get => mSelectedIndex;
            set
            {
                mSelectedIndex = value;
                OnPropertyChanged(nameof(SelectedIndex));
            }
        }

        private double mPlayingProgress;

        public double PlayingProgress
        {
            get => mPlayingProgress;
            set
            {
                mPlayingProgress = value;
                OnPropertyChanged(nameof(PlayingProgress));
            }
        }

        private string mElapsedTime;

        public string ElapsedTime
        {
            get => PlayingSong != null ? mElapsedTime : BasicElapsedTime;
            set
            {
                mElapsedTime = value;
                OnPropertyChanged(nameof(ElapsedTime));
            }
        }

        private void AddToQueueAction()
        {
            CommonOpenFileDialog fileDialog = new()
            {
                EnsureValidNames = false,
                EnsureFileExists = false,
                EnsurePathExists = true
            };

            if (fileDialog.ShowDialog() == CommonFileDialogResult.Ok)
            {
                QueueFilePath = fileDialog.FileName;
            }
            else
            {
                return;
            }

            //TODO : this is garbage
            //mCurrentQueue.Load(QueueFilePath);

            if (SongList.Count <= 0)
            {
                return;
            }

            double queueDuration = TotalSeconds();
            string format = "hh\\:mm\\:ss";
            if (queueDuration < 3600)
            {
                format = "mm\\:ss";
            }

            TimeSpan totalDuration = TimeSpan.FromSeconds(queueDuration);
            QueueInfo = SongList.Count + " songs - " + totalDuration.ToString(format);
        }

        private void ClearQueueAction()
        {
            SongList.Clear();
            QueueInfo = "";
        }

        private void DeleteSongFromLibraryAction()
        {
            List<Artist> artistsToRemove = new();
            foreach (Song song in mSelectedSongs)
            {
                if (song == PlayingSong)
                {
                    StopSongAction();
                }
                
                artistsToRemove.Add(song.Artist);
                SongList.Remove(song);
                if (song.DiscNumber.HasValue)
                {
                    if (song.TrackNumber.HasValue)
                    {
                        song.Album.CDs[song.DiscNumber.Value].SongCollection
                            .TryRemove(song.TrackNumber.Value, out Song _);
                    }
                    else
                    {
                        song.Album.CDs[song.DiscNumber.Value].UnlistedSongs
                            .TryRemove(song);
                    }
                }

                else
                {
                    if (song.TrackNumber.HasValue)
                    {
                        song.Album.AlbumSongCollection.TryRemove(song.TrackNumber.Value, out Song _);
                    }
                    else
                    {
                        song.Album.UnlistedSongs.TryRemove(song);
                    }
                }
                
                foreach (Album irrelevantAlbum in song.Artist.ArtistAlbums.Values.Where(x => x.AllSongs.IsEmpty))
                {
                    song.Artist.ArtistAlbums.Remove(irrelevantAlbum.Title, out Album removedAlbum);
                    Albums.Remove(removedAlbum);
                }

                foreach (CD irrelevantCd in song.Artist.ArtistAlbums.Values.SelectMany(x => x.CDs.Values).Where(x =>
                    x.SongCollection.IsEmpty))
                {
                    song.Artist.ArtistAlbums.Values
                        .FirstOrDefault(x => x.HasCDs && x.CDs.ContainsKey(irrelevantCd.DiscNumber)
                                                      && x.CDs.Values.FirstOrDefault(z => z.Title == x.Title) != null)
                        ?.CDs.TryRemove(irrelevantCd.DiscNumber, out _);
                }
            }

            foreach (Artist artist in artistsToRemove)
            {
                if (artist.ArtistAlbums.Count == 0 || 
                    artist.ArtistAlbums.Values.Count(x => x.AllSongs.IsEmpty) == artist.ArtistAlbums.Count)
                {
                    Artists.Remove(artist);
                    artist.DestroySerializedArtist();
                }
                else
                {
                    artist.SerializeArtistAsync();
                }
            }
        }
        
             
        private void OpenInExplorerAction()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                OpenInExplorer.OpenInOsxFileExplorer(SelectedSong.FilePath);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) 
            {
                OpenInExplorer.OpenInWindowsFileExplorer(SelectedSong.FilePath);
            }
        }

        //If selected song isn't null and it's like playing right then i can make the play song action true and just restart the song
        //this is for another play song button function thingy
        private void PlaySongAction()
        {
            if (PlayingSong?.FilePath != SelectedSong?.FilePath)
            {
                StopSongAction();
                Track track = new(SelectedSong.FilePath);
                SelectedSong.ByteArtwork =
                    BitmapHelper.ResizeImage(BitmapHelper.LoadPicture(track.EmbeddedPictures), 100, 100);

                PlayingSong = SelectedSong;
                mPlayingIndex = SelectedIndex;
                
                mMusicPlayer.Play(new Uri(PlayingSong.FilePath));
                StartTimers();
            }
            else
            {
                mPlayer.Play();
                StartTimers();
            }
            OnPropertyChanged(nameof(TrackTitleInfo));
            OnPropertyChanged(nameof(ArtistAlbumInfo));
            OnPropertyChanged(nameof(ElapsedTime));
            OnPropertyChanged(nameof(PlayingSong));
            OnPropertyChanged(nameof(IsSongPlaying));
        }

        private void PauseSongAction()
        {
            StopTimers();
            mPlayer.Pause();
        }

        private void StopSongAction()
        {
            mPlayer.Stop();
            if (PlayingSong != null)
            {
                PlayingSong.ByteArtwork = null;
                PlayingSong = null;
            }

            StopTimers();
            ElapsedTime = BasicElapsedTime;
            PlayingProgress = 0;
            mSeconds = 0;
            
            OnPropertyChanged(nameof(TrackTitleInfo));
            OnPropertyChanged(nameof(ArtistAlbumInfo));
            OnPropertyChanged(nameof(PlayingSong));
            OnPropertyChanged(nameof(ElapsedTime));
            OnPropertyChanged(nameof(IsSongPlaying));
        }

        private void FastForwardAction()
        {
            mMusicPlayer.FastForward(10000);

            mSeconds += 10000;
            PlayingProgress += 10000;

            string displayProgress = TimeSpan.FromMilliseconds(mSeconds).ToString("mm\\:ss");
            ElapsedTime = displayProgress + " / " + PlayingSong.DisplayDuration;
        }

        private void RewindAction()
        {
            mMusicPlayer.Rewind(10000);
            mSeconds -= 10000;
            PlayingProgress -= 10000;
            if (mSeconds < 0)
            {
                mSeconds = 0;
            }

            if (PlayingProgress < 0)
            {
                PlayingProgress = 0;
            }

            string displayProgress = TimeSpan.FromMilliseconds(mSeconds).ToString("mm\\:ss");
            ElapsedTime = displayProgress + " / " + PlayingSong.DisplayDuration;
        }

        private void StopTimers()
        {
            mIncrementPlayingProgress.Stop();
            mFindSongEnd.Stop();
        }

        private void StartTimers()
        {
            mIncrementPlayingProgress.Start();
            mFindSongEnd.Start();
        }

        private void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void OnFileDrop(string[] filePaths)
        {
            try
            {
                Task.Run(() =>
                {
                    try
                    {
                        SongList.AddUniqueRange(sSongLoader.LoadSongs(filePaths, Artists, SongList));
                    }
                    catch (Exception e)
                    {
                        MessageBoxResult _ = MessageBox.Show(e.Message, "Error adding song", MessageBoxButton.OK,
                            MessageBoxImage.Information);
                    }
                });
            }
            catch (Exception e)
            {
                if (e.Message == "The key already existed in the dictionary.")
                {
                    MessageBoxResult _ = MessageBox.Show("This song already exists", "Duplicate song",
                        MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBoxResult _ = MessageBox.Show(e.Message, "Error adding song", MessageBoxButton.OK,
                        MessageBoxImage.Information);
                }
            }
        }
        
        private void PlayPauseStopLastNextAction(object parameter)
        {
            KeyEventArgs keyEventArgs = (KeyEventArgs) parameter;
            if (keyEventArgs.Key == Key.MediaPlayPause)
            {
                if (PlayingSong != null && mIncrementPlayingProgress.Enabled)
                {
                    PauseSongAction();
                    return;
                }

                if (PlayingSong != null && !mIncrementPlayingProgress.Enabled)
                {
                    PlaySongAction();
                    return;
                }
            }

            if (keyEventArgs.Key == Key.MediaStop)
            {
                StopSongAction();
            }

            if (keyEventArgs.Key == Key.MediaNextTrack)
            {
                if (PlayingSong != null)
                {
                    int index = SongList.IndexOf(PlayingSong);
                    if (SongList.Count > index +1)
                    {
                        StopSongAction();
                        SelectedSong = SongList[index +1];
                        PlaySongAction();
                        return;
                    }

                    if (SongList.Count == index + 1)
                    {
                        StopSongAction();
                    }
                }
            }

            if (keyEventArgs.Key == Key.MediaPreviousTrack)
            {
                if (PlayingSong != null)
                {
                    int index = SongList.IndexOf(PlayingSong);
                    
                    if (index == 0)
                    {
                        StopSongAction();
                        return;
                    }
                    
                    if (SongList.Count >= index +1)
                    {
                        StopSongAction();
                        SelectedSong = SongList[index -1];
                        PlaySongAction();
                        return;
                    }
                }
            }

            if (keyEventArgs.Key == Key.VolumeMute)
            {
                mPlayer.IsMuted = !mPlayer.IsMuted;
            }
        }

        private MediaElement mPlayer = new()
        {
            LoadedBehavior = MediaState.Manual
        };

        public MediaElement Player
        {
            get => mPlayer;
            set
            {
                mPlayer = value;
                OnPropertyChanged(nameof(Player));
            }
        }

        public void Play(Uri filePath)
        {
            Player.Source = filePath;
            Player.Play();
        }

        public void Play()
        {
            Player.Play();
        }

        public void Pause()
        {
            Player.Pause();
        }

        public void Stop()
        {
            Player.Stop();
        }

        public void FastForward(double milliseconds)
        {
            Player.Position += TimeSpan.FromMilliseconds(milliseconds);
        }

        public void Rewind(double milliseconds)
        {
            Player.Position -= TimeSpan.FromMilliseconds(milliseconds);
        }

        public bool IsDone()
        {
            return Player.Position >= Player.NaturalDuration;
        }
        
        private double TotalSeconds()
        {
            return SongList.Sum(s => s.Duration.TotalSeconds);
        }

        public void ShowEditWindowAction()
        {
            MetroWindow win = new()
            {
                Width = 430,
                IsMinButtonEnabled = false,
                IsMaxRestoreButtonEnabled = false,
                ResizeMode = ResizeMode.NoResize
            };

            EditViewModel viewModel;
            
            if (mSelectedSongs.Count > 1)
            {
                viewModel = new EditSongsViewModel(mSelectedSongs, SongList, Artists, Albums, () => win.Close());
                win.Height = 370;
                win.Content = viewModel;
                win.Title = "Edit Multiple Songs";
            }
            else if (mSelectedSongs.Count == 1 && SelectedSong != null)
            {
                viewModel = new EditSongViewModel(mSelectedSongs, SongList, Artists, Albums, () => win.Close());

                win.Content = viewModel;
                win.Height = 400;
                win.Title = string.IsNullOrEmpty(SelectedSong.Title) ?
                    "Edit Song" : "Edit \"" + SelectedSong.Title + "\"";
            }

            win.ShowDialog();

            SortGridAction("ArtistName");
            OnPropertyChanged(nameof(TrackTitleInfo));
            OnPropertyChanged(nameof(ArtistAlbumInfo));
        }
    }
}