using System;
using System.Collections.Immutable;
using System.ComponentModel;
using Cajonic.Services;
using Cajonic.Model;
using System.Windows.Input;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Runtime.CompilerServices;
using System.Windows.Controls;

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
                foreach (Album album in artist.ArtistAlbums)
                {
                    SongList.AddUniqueRange(album.AlbumSongCollection);
                }
            }
        }

        public string QueueFilePath { get; set; }
        public ObservableCollection<Song> SongList => mCurrentQueue.SongList;
        public ObservableCollection<Artist> Artists { get; set; } = new ObservableCollection<Artist>();

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
            get => PlayingSong != null ? mElapsedTime : "00:00 / " + "00:00";
            set
            {
                mElapsedTime = value;
                OnPropertyChanged(nameof(ElapsedTime));
            }
        }


        
        private void AddToQueueAction()
        {
            OpenFileDialog fileDialog = new OpenFileDialog
            {
                ValidateNames = false,
                CheckFileExists = false,
                CheckPathExists = true,
            };

            if (fileDialog.ShowDialog() == DialogResult.OK)
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
                PlayingSong = SelectedSong;
                mPlayingIndex = SelectedIndex;
                if (PlayingSong.Year != null)
                {
                    ArtistAlbumInfo = PlayingSong.ArtistName + " - " + PlayingSong.AlbumTitle + " [" +
                                      PlayingSong.Year.Value + "]";
                }
                else
                {
                    ArtistAlbumInfo = PlayingSong.ArtistName + " - " + PlayingSong.AlbumTitle;
                }

                TrackTitleInfo = PlayingSong.TrackNumber + ". " + PlayingSong.Title;
                mMusicPlayer.Play(new Uri(PlayingSong.FilePath));
                OnPropertyChanged(nameof(ElapsedTime));
            }
            else
            {
                mPlayer.Play();
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
            StopTimers();
            PlayingProgress = 0;
            mSeconds = 0;
            PlayingSong = null;
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
                if (filepaths.Length > 1)
                {
                    SongList.AddUniqueRange(SongLoader.LoadMultiple(filepaths, Artists.ToList()));
                }

                FileAttributes fileAttributes = File.GetAttributes(filepaths[0]);
                switch (filepaths.Length)
                {
                    case 1 when fileAttributes.HasFlag(FileAttributes.Directory):
                        SongList.AddUniqueRange(SongLoader.LoadMultiple(filepaths, Artists.ToList()));
                        break;
                    case 1 when !fileAttributes.HasFlag(FileAttributes.Directory):
                        SongList.AddUnique(SongLoader.LoadIndividualSong(filepaths[0], Artists.ToList()));
                        break;
                }
                
                OnPropertyChanged(nameof(SongList));
            }
            catch (Exception e)
            {
                DialogResult _ = MessageBox.Show(e.Message, "Error adding song", MessageBoxButtons.OK, MessageBoxIcon.Information);
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