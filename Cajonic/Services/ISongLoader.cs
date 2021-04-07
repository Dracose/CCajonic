using Cajonic.Model;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Cajonic.Services
{
    public interface ISongLoader
    {
        // This is where you get the metadata you want, and potentially make this function parallelized
        ImmutableList<Song> Load(string path);
    }
}
