using Cajonic.Model;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Text;

namespace Cajonic.Services
{
    public interface ISongLoader
    {
        ImmutableList<Song> Load(string path);
    }
}
