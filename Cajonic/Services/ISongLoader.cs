using Cajonic.Model;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Cajonic.Services
{
    public interface ISongLoader
    {
        ImmutableList<Song> LoadSongs(string[] path, ICollection<Artist> artists);
    }
}
