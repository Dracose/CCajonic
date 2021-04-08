using Cajonic.Services;
using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace Cajonic.Model
{
    public class SongCollection
    {
        private readonly ISongLoader _loader;

        public ObservableCollection<Song> SongList { get; }

        public SongCollection(ISongLoader ql)
        {
            SongList = new ObservableCollection<Song>();
            _loader = ql;
        }

        public void Load(string filepath)
        {
            var songs = _loader.Load(filepath);

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