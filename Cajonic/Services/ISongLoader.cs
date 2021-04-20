using Cajonic.Model;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Threading.Tasks;

namespace Cajonic.Services
{
    public interface ISongLoader
    {
        ImmutableList<Song> LoadMultiple(string[] path, ICollection<Artist> artists);

        Song LoadIndividualSong(string path, IEnumerable<Artist> artists);
    }
}
