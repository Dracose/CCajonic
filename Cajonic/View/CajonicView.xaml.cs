using System;
using System.Windows;
using System.Windows.Input;
using Cajonic.Services;
using Cajonic.ViewModel;

namespace Cajonic.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class CajonicView : Window
    {
        public CajonicView()
        {
            InitializeComponent();
        }
        public void ListViewItem_MouseDoubleClick(object sender, System.Windows.Input.MouseEventArgs e)
        {
            (DataContext as CajonicViewModel)?.PlaySong.Execute(null);
        }
    }
}
