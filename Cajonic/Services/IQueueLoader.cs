using Cajonic.Model;
using System;
using System.Collections.Generic;
using System.Text;

namespace Cajonic.Services
{
    public interface IQueueLoader
    {
        // This is where you get the metadata you want, and potentially make this function parallelized
        List<Song> Load(string path);
    }
}
