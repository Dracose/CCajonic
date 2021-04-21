using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Media.Imaging;
using ATL;
using Cajonic.Services;
using ProtoBuf;

namespace Cajonic.Model
{
    //TODO : DON'T FORGET TO GET THE ALBUM ARTWORK BACK
    [ProtoContract]
    public class Artist : IEquatable<Artist>
    {
        public string BinaryFilePath { get; set; }

        private BitmapImage mProfileImage;
        public BitmapImage ProfileImage
        {
            get => mProfileImage;
            set => mProfileImage = value;
        }

        [ProtoMember(1)]
        private byte[] mByteProfileImage;

        public Artist()
        {
            Name = string.Empty;
            ProfileImage = null;
            BinaryFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), $"Cajonic\\SaveData\\Artists\\{Name}.bin");
        }

        public Artist(Song song)
        {
            Name = song.ArtistName;
            BinaryFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), $"Cajonic\\SaveData\\Artists\\{Name}.bin");
            ArtistAlbums.TryAdd(ArtistAlbums.Count, song.Album);
        }

        public Artist(Track track)
        {
            Name = track.Artist;
            BinaryFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), $"Cajonic\\SaveData\\Artists\\{Name}.bin");
        }


        public async void SerializeArtistAsync()
        {
            IsSerialization = false;
            await SerializationHelper.WriteToBinaryFile(BinaryFilePath, this);
        }

        private static async Task<Artist> DeserializeArtistAsync(string filePath)
        {
            return await SerializationHelper.ReadFromBinaryFile<Artist>(filePath);
        }

        private Artist DeserializeArtistHelper(string filePath)
        {
            Task<Artist> taskArtist = DeserializeArtistAsync(filePath);
            BinaryFilePath = filePath;
            Name = taskArtist.Result.Name;
            ProfileImage = taskArtist.Result.ProfileImage;
            if (ProfileImage != null)
            {
                mByteProfileImage = BitmapHelper.ConvertToBytes(ProfileImage);
            }
            ArtistAlbums = taskArtist.Result.ArtistAlbums;
            return this;
        }

        public static Artist DeserializeArtistHelperStatic(string filePath)
        {
            Artist artist = new Artist();
            return artist.DeserializeArtistHelper(filePath);
        }

        [ProtoMember(2)]
        public string Name { get; set; }
        [ProtoMember(3)]
        public ConcurrentDictionary<int, Album> ArtistAlbums { get; set; } = new ConcurrentDictionary<int, Album>();

        public bool IsSerialization { get; set; }

        public bool Equals([AllowNull] Artist other) => other?.BinaryFilePath == BinaryFilePath;

        public override int GetHashCode()
        {
            return $"{BinaryFilePath}".GetHashCode();
        }
    }
}
