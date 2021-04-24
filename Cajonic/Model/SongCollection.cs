using Cajonic.Services;
using System.Collections.ObjectModel;
using System.Linq;
using Meziantou.Framework.WPF.Collections;

namespace Cajonic.Model
{
    public class SongCollection
    {
        private readonly ISongLoader mLoader;

        public ConcurrentObservableCollection<Song> SongList { get; set; }

        public SongCollection(ISongLoader songLoader)
        {
            SongList = new ConcurrentObservableCollection<Song>();
            mLoader = songLoader;
        }

        //public void Load(string filepath)
        //{
        //    ImmutableList<Song> songs = mLoader.LoadIndiv(filepath, null);

        //    foreach (Song s in songs)
        //    {
        //        SongList.Add(s);
        //    }
        //}

        public double TotalSeconds()
        {
            return SongList.Sum(s => s.Duration.TotalSeconds);
        }
    }
}