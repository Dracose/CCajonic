using System.Collections.Immutable;
using Cajonic.Services;
using System.Collections.ObjectModel;
using System.Linq;

namespace Cajonic.Model
{
    public class SongCollection
    {
        private readonly ISongLoader mLoader;

        public ObservableCollection<Song> SongList { get; }

        public SongCollection(ISongLoader songLoader)
        {
            SongList = new ObservableCollection<Song>();
            mLoader = songLoader;
        }

        public void Load(string filepath)
        {
            ImmutableList<Song> songs = mLoader.Load(filepath);

            foreach (Song s in songs)
            {
                SongList.Add(s);
            }
        }

        public double TotalSeconds()
        {
            return SongList.Sum(s => s.Duration.TotalSeconds);
        }
    }
}