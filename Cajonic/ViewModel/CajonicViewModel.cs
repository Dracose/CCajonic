using System;
using System.ComponentModel;
using Cajonic.Services;
using Cajonic.Model;
using System.Windows.Input;
using System.Diagnostics;
using System.Collections.ObjectModel;
using System.Windows.Forms;
using System.Runtime.CompilerServices;

namespace Cajonic.ViewModel
{
    class CajonicViewModel : INotifyPropertyChanged, IFileDragDropTarget
    {
        public event PropertyChangedEventHandler PropertyChanged;
        private System.Timers.Timer _incrementPlayingProgress;
        private System.Timers.Timer _findSongEnd;
        private int _playingIndex;
        private readonly IMusicPlayer _player;
        private SongCollection _currentQueue;
        private double _seconds;
        private static ISongLoader mSongLoader = new SongLoader();
        private static SongCollection songCollection = new SongCollection(mSongLoader);

        public ICommand AddToQueueCommand { get; private set; }
        public ICommand ClearQueueCommand { get; private set; }
        public ICommand PlaySong { get; private set; }
        public ICommand PauseSong { get; private set; }
        public ICommand StopSong { get; private set; }
        public ICommand FastForwardCommand { get; private set; }
        public ICommand RewindCommand { get; private set; }

        public CajonicViewModel(IMusicPlayer m)
        {
            if (DesignerProperties.GetIsInDesignMode(
                new System.Windows.DependencyObject())) return;

            _seconds = 0;

            _playingSong = new Song();
            QueueInfo = "";
            _player = m;
            ElapsedTime = "00:00 / 00:00";
            _incrementPlayingProgress = new System.Timers.Timer();
            _incrementPlayingProgress.Interval = 1000;
            _currentQueue = new SongCollection(mSongLoader);
            _incrementPlayingProgress.Elapsed += (sender, e) =>
            {
                _seconds += 1000;
                PlayingProgress += 1000;

                string displayProgress = TimeSpan.FromMilliseconds(_seconds).ToString("mm\\:ss");
                ElapsedTime = displayProgress + " / " + PlayingSong.DisplayDuration;
            };

            _findSongEnd = new System.Timers.Timer();
            _findSongEnd.Interval = 1;
            _findSongEnd.Elapsed += (sender, e) =>
            {
                System.Windows.Application.Current.Dispatcher.Invoke(() =>
                {
                    if (_player.IsDone())
                    {
                        Debug.WriteLine("Done");
                        if (++_playingIndex < _currentQueue.SongList.Count)
                        {

                            SelectedSong = _currentQueue.SongList[_playingIndex];
                            PlaySongAction();
                        }
                        else
                        {
                            StopSongAction();
                        }
                    }
                });
            };

            PlaySong = new CommandHandler(() => PlaySongAction(), () => true);
            PauseSong = new CommandHandler(() => PauseSongAction(), () => true);
            StopSong = new CommandHandler(() => StopSongAction(), () => true);
            FastForwardCommand = new CommandHandler(() => FastForwardAction(), () => true);
            RewindCommand = new CommandHandler(() => RewindAction(), () => true);
        }

        public string QueueFilePath { get; set; }
        public ObservableCollection<Song> SongList { get { return _currentQueue.SongList; } }

        private string _queueInfo;
        public string QueueInfo
        {
            get { return _queueInfo; }
            set
            {
                _queueInfo = value;
                OnPropertyChanged(nameof(QueueInfo));
            }
        }

        private string _artistAlbumInfo;
        public string ArtistAlbumInfo
        {
            get { return _artistAlbumInfo; }
            set
            {
                _artistAlbumInfo = value;
                OnPropertyChanged(nameof(ArtistAlbumInfo));
            }
        }

        private string _trackTitleInfo;
        public string TrackTitleInfo
        {
            get { return _trackTitleInfo; }
            set
            {
                _trackTitleInfo = value;
                OnPropertyChanged(nameof(TrackTitleInfo));
            }
        }

        private Song _selectedSong;
        public Song SelectedSong
        {
            get { return _selectedSong; }
            set
            {
                _selectedSong = value;
                OnPropertyChanged(nameof(SelectedSong));
            }
        }

        private Song _playingSong;
        public Song PlayingSong
        {
            get { return _playingSong; }
            set
            {
                _playingSong = value;
                OnPropertyChanged(nameof(PlayingSong));
            }
        }

        private int _selectedIndex;
        public int SelectedIndex
        {
            get { return _selectedIndex; }
            set
            {
                _selectedIndex = value;
                OnPropertyChanged(nameof(SelectedIndex));
            }
        }

        private double _playingProgress;
        public double PlayingProgress
        {
            get => _playingProgress;
            set
            {
                _playingProgress = value;
                OnPropertyChanged(nameof(PlayingProgress));
            }
        }

        private string _elapsedTime;
        public string ElapsedTime
        {
            get => PlayingSong != null ? _elapsedTime : "00:00 / " + "00:00";
            set
            {
                _elapsedTime = value;
                OnPropertyChanged(nameof(ElapsedTime));
            }
        }


        #region CommandActions
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

            _currentQueue.Load(QueueFilePath);

            if (_currentQueue.SongList.Count > 0)
            {
                double queueDuration = _currentQueue.TotalSeconds();
                string format = "hh\\:mm\\:ss";
                if (queueDuration < 3600)
                {
                    format = "mm\\:ss";
                }
                TimeSpan totalDuration = TimeSpan.FromSeconds(queueDuration);
                QueueInfo = _currentQueue.SongList.Count + " songs - " + totalDuration.ToString(format);
            }
        }

        private void ClearQueueAction()
        {
            _currentQueue.SongList.Clear();
            QueueInfo = "";
        }

        private void PlaySongAction()
        {
            if (PlayingSong.FilePath != SelectedSong.FilePath)
            {
                StopSongAction();
                PlayingSong = SelectedSong;
                _playingIndex = SelectedIndex;
                ArtistAlbumInfo = PlayingSong.Artist + " - " + PlayingSong.Album + " [" + PlayingSong.Year + "]";
                TrackTitleInfo = PlayingSong.TrackNumber + ". " + PlayingSong.Title;
                _player.Play(new Uri(PlayingSong.FilePath));
                OnPropertyChanged(nameof(ElapsedTime));
            }
            else
            {
                _player.Play();
            }
            StartTimers();
        }

        private void PauseSongAction()
        {
            StopTimers();
            _player.Pause();
        }

        private void StopSongAction()
        {
            _player.Stop();
            StopTimers();
            PlayingProgress = 0;
            _seconds = 0;
            PlayingSong = null;
            OnPropertyChanged(nameof(ElapsedTime));
        }

        private void FastForwardAction()
        {
            _player.FastForward(10000);

            _seconds += 10000;
            PlayingProgress += 10000;

            string displayProgress = TimeSpan.FromMilliseconds(_seconds).ToString("mm\\:ss");
            ElapsedTime = displayProgress + " / " + PlayingSong.DisplayDuration;
        }

        private void RewindAction()
        {
            _player.Rewind(10000);
            _seconds -= 10000;
            PlayingProgress -= 10000;
            if (_seconds < 0)
            {
                _seconds = 0;
            }

            if (PlayingProgress < 0)
            {
                PlayingProgress = 0;
            }

            string displayProgress = TimeSpan.FromMilliseconds(_seconds).ToString("mm\\:ss");
            ElapsedTime = displayProgress + " / " + PlayingSong.DisplayDuration;
        }

        #endregion

        #region TimerControls
        private void StopTimers()
        {
            _incrementPlayingProgress.Stop();
            _findSongEnd.Stop();
        }

        private void StartTimers()
        {
            _incrementPlayingProgress.Start();
            _findSongEnd.Start();
        }
        #endregion

        protected void OnPropertyChanged([CallerMemberName] string name = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        public void OnFileDrop(string[] filepaths)
        {
            try
            {
                foreach (string filepath in filepaths)
                {
                    SongList.AddUniqueRange(mSongLoader.Load(filepath));
                }
                OnPropertyChanged(nameof(SongList));
            }
            catch
            {
                DialogResult _ = MessageBox.Show("This format is not supported by Cajonic", "Format not supported", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
        }
    }
}