using Cajonic.Model;
using Cajonic.Services;
using Cajonic.ViewModel;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Cajonic
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class CajonicView : Window, IMusicPlayer
    {
        private CajonicViewModel vm;
        public CajonicView()
        {
            InitializeComponent();
            Player.LoadedBehavior = MediaState.Manual;
            vm = new CajonicViewModel(this);
            DataContext = vm;
        }
        public void ListViewItem_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            vm.PlaySong.Execute(null);
        }

        #region IMusicPlayer
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
        #endregion
    }
}
