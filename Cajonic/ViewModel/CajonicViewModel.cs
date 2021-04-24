using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.ComponentModel;
using Cajonic.Services;
using Cajonic.Model;
using System.Windows.Input;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using ATL;
using Meziantou.Framework.WPF.Collections;
using Microsoft.WindowsAPICodePack.Dialogs;

namespace Cajonic.ViewModel
{
    public class CajonicViewModel : INotifyPropertyChanged, IFileDragDropTarget, IMusicPlayer
    {
        //serialize file paths and read a list of filepaths from this, then call reserialize.
        private readonly string mArtistDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), $"Cajonic\\SaveData\\Artists");
        public event PropertyChangedEventHandler PropertyChanged;
        private readonly System.Timers.Timer mIncrementPlayingProgress;
        private readonly System.Timers.Timer mFindSongEnd;
        private int mPlayingIndex;
        private readonly IMusicPlayer mMusicPlayer;
        private readonly SongCollection mCurrentQueue;
        private double mSeconds;
        private static readonly ISongLoader SongLoader = new SongLoader();
        private static SongCollection sSongCollection = new SongCollection(SongLoader);
        private string mBasicElapsedTime = "00:00 / " + "00:00";

        public ICommand AddToQueueCommand { get; private set; }
        public ICommand ClearQueueCommand { get; private set; }
        public ICommand PlaySong { get; }
        public ICommand PauseSong { get; }
        public ICommand StopSong { get; }
        public ICommand FastForwardCommand { get; }
        public ICommand RewindCommand { get; }

        public CajonicViewModel()
        {
            if (DesignerProperties.GetIsInDesignMode(
                new System.Windows.DependencyObject())) return;

            mSeconds = 0;

            mPlayingSong = new Song();
            QueueInfo = "";
            mMusicPlayer = this;
            ElapsedTime = "00:00 / 00:00";
            mIncrementPlayingProgress = new System.Timers.Timer { Interval = 1000 };
            mCurrentQueue = new SongCollection(SongLoader);
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
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (!mMusicPlayer.IsDone())
                    {
                        return;
                    }

                    Debug.WriteLine("Done");
                    if (++mPlayingIndex < mCurrentQueue.SongList.Count)
                    {
                        SelectedSong = mCurrentQueue.SongList[mPlayingIndex];
                        PlaySongAction();
                    }
                    else
                    {
                        StopSongAction();
                    }
                });
            };

            DeserializeArtists();
            PlaySong = new CommandHandler(PlaySongAction, () => true);
            PauseSong = new CommandHandler(PauseSongAction, () => true);
            StopSong = new CommandHandler(StopSongAction, () => true);
            FastForwardCommand = new CommandHandler(FastForwardAction, () => true);
            RewindCommand = new CommandHandler(RewindAction, () => true);
        }

        private void DeserializeArtists()
        {
            DirectoryInfo directory = new DirectoryInfo(mArtistDirectory);
            if (!directory.Exists)
            {
                return;
            }

            FileInfo[] files = directory.GetFiles("*.bin");
            foreach (FileInfo file in files)
            {
                Artists.Add(Artist.DeserializeArtistHelperStatic(file.FullName));
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

        public ConcurrentObservableCollection<Song> SongList => mCurrentQueue.SongList;

        public ConcurrentObservableCollection<Artist> Artists { get; set; } = new ConcurrentObservableCollection<Artist>();

        public ConcurrentObservableCollection<Album> Albums { get; set; } = new ConcurrentObservableCollection<Album>();

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

        private string mArtistAlbumInfo;
        public string ArtistAlbumInfo
        {
            get => mArtistAlbumInfo;
            set
            {
                mArtistAlbumInfo = value;
                OnPropertyChanged(nameof(ArtistAlbumInfo));
            }
        }

        private string mTrackTitleInfo;
        public string TrackTitleInfo
        {
            get => mTrackTitleInfo;
            set
            {
                mTrackTitleInfo = value;
                OnPropertyChanged(nameof(TrackTitleInfo));
            }
        }

        private Song mSelectedSong;
        public Song SelectedSong
        {
            get => mSelectedSong;
            set
            {
                if (mSelectedSong != null) {
                    mSelectedSong.ByteArtwork = null;
                }
                mSelectedSong = value;
                OnPropertyChanged(nameof(SelectedSong));
            }
        }

        private Song mPlayingSong;
        public Song PlayingSong
        {
            get => mPlayingSong;
            set
            {
                mPlayingSong = value;
                OnPropertyChanged(nameof(PlayingSong));
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
            get => PlayingSong != null ? mElapsedTime : mBasicElapsedTime;
            set
            {
                mElapsedTime = value;
                OnPropertyChanged(nameof(ElapsedTime));
            }
        }



        private void AddToQueueAction()
        {
            CommonOpenFileDialog fileDialog = new CommonOpenFileDialog
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

            if (mCurrentQueue.SongList.Count > 0)
            {
                double queueDuration = mCurrentQueue.TotalSeconds();
                string format = "hh\\:mm\\:ss";
                if (queueDuration < 3600)
                {
                    format = "mm\\:ss";
                }
                TimeSpan totalDuration = TimeSpan.FromSeconds(queueDuration);
                QueueInfo = mCurrentQueue.SongList.Count + " songs - " + totalDuration.ToString(format);
            }
        }

        private void ClearQueueAction()
        {
            mCurrentQueue.SongList.Clear();
            QueueInfo = "";
        }

        private void PlaySongAction()
        {
            if (PlayingSong?.FilePath != SelectedSong?.FilePath)
            {
                StopSongAction();
                Track track = new Track(SelectedSong.FilePath);
                SelectedSong.ByteArtwork = BitmapHelper.LoadImage(track.EmbeddedPictures[0].PictureData);
                track = null;

                PlayingSong = SelectedSong;
                mPlayingIndex = SelectedIndex;
                if (PlayingSong.Year != null)
                {
                    ArtistAlbumInfo = PlayingSong.Artist.Name + " - " + PlayingSong.Album.Title + " [" +
                                      PlayingSong.Year.Value + "]";
                }
                else
                {
                    ArtistAlbumInfo = PlayingSong.Artist.Name + " - " + PlayingSong.Album.Title;
                }

                TrackTitleInfo = PlayingSong.TrackNumber + ". " + PlayingSong.Title;
                mMusicPlayer.Play(new Uri(PlayingSong.FilePath));
                OnPropertyChanged(nameof(ElapsedTime));
                OnPropertyChanged(nameof(PlayingSong));
            }
            else
            {
                if (!mMusicPlayer.IsDone())
                {
                    PauseSongAction();
                }
                else
                {
                    mPlayer.Play();
                }
            }
            StartTimers();
        }

        private void PauseSongAction()
        {
            StopTimers();
            mPlayer.Pause();
        }

        private void StopSongAction()
        {
            mPlayer.Stop();
            if (PlayingSong != null) {
                PlayingSong.ByteArtwork = null;
                PlayingSong = null;
            }
            StopTimers();
            PlayingProgress = 0;
            mSeconds = 0;
            OnPropertyChanged(nameof(PlayingSong));
            OnPropertyChanged(nameof(ElapsedTime));
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

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void OnFileDrop(string[] filepaths)
        {
            try
            {
                Task.Run(() =>
                {
                    try
                    {
                        SongList.AddUniqueRange(SongLoader.LoadSongs(filepaths, Artists));
                    }
                    catch (Exception e)
                    {
                        MessageBoxResult _ = MessageBox.Show(e.Message, "Error adding song", MessageBoxButton.OK, MessageBoxImage.Information);
                    }
                });

            }
            catch (Exception e)
            {
                if (e.Message == "The key already existed in the dictionary.")
                {
                    MessageBoxResult _ = MessageBox.Show("This song already exists", "Duplicate song", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                else
                {
                    MessageBoxResult _ = MessageBox.Show(e.Message, "Error adding song", MessageBoxButton.OK, MessageBoxImage.Information);
                }
            }
        }

        private MediaElement mPlayer = new MediaElement { LoadedBehavior = MediaState.Manual };
        public MediaElement Player
        {
            get => mPlayer;
            set
            {
                mPlayer = value; OnPropertyChanged(nameof(Player));
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
    }
}