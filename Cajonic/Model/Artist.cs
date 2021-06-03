using System;
using System.Collections.Concurrent;
using System.Collections.Immutable;
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
    [ProtoContract]
    public class Artist : IEquatable<Artist>
    {
        public string BinaryFilePath { get; set; }

        public const string UnknownArtist = "Unknown Artist";
        public static readonly string ArtistDirectory = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), $"Cajonic\\SaveData\\Artists");

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
            BinaryFilePath = $"{ArtistDirectory}\\{Name}.bin";
        }

        public Artist(Song song)
        {
            Name = song.ArtistName;
            ProfileImage = null;
            BinaryFilePath = $"{ArtistDirectory}\\{Name}.bin";
        }

        public Artist(Track track)
        {
            Name = string.IsNullOrEmpty(track.Artist) ? UnknownArtist : track.Artist;
            string copyName = ReplaceInvalidChars(Name);
            BinaryFilePath = Name == UnknownArtist ? Path.Combine(ArtistDirectory, $"{Name}.bin") :
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), $"Cajonic\\SaveData\\Artists\\{copyName}.bin");
            Album newAlbum = new(track, Name);
            
            ArtistAlbums.TryAdd(newAlbum.Title, newAlbum);
            IsToModify = true;
        }


        public async void SerializeArtistAsync()
        {
            IsSerialization = false;
            IsToModify = false;
            IsDestruction = false;
            await SerializationHelper.WriteToBinaryFile(BinaryFilePath, this);
        }

        public void DestroySerializedArtist()
        {
            SerializationHelper.DestroyBinaryFile(BinaryFilePath);
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

            ImmutableList<Song> allAlbumSongs = taskArtist.Result.ArtistAlbums.
                SelectMany(x => x.Value.AlbumSongCollection.Values).ToImmutableList();

            ImmutableList<Song> allCdSongs = taskArtist.Result.ArtistAlbums.
                SelectMany(x => x.Value.CDs.Values).
                SelectMany(x => x.SongCollection.Values).ToImmutableList();

            foreach (Song song in allAlbumSongs)
            {
                song.Artist = this;
                song.Album = taskArtist.Result.ArtistAlbums.FirstOrDefault(x => x.Value.AlbumSongCollection.Values.Contains(song)).Value;
            }

            foreach (Song song in allCdSongs)
            {
                song.Artist = this;
                song.Album = taskArtist.Result.ArtistAlbums
                    .FirstOrDefault(x => x.Value.CDs
                        .SelectMany(x => x.Value.SongCollection)
                        .Select(x => x.Value.FilePath)
                        .Contains(song.FilePath)).Value;
            }

            foreach (Song song in taskArtist.Result.ArtistAlbums.Values.SelectMany(x => x.UnlistedSongs))
            {
                song.Artist = this;
                song.Album = taskArtist.Result.ArtistAlbums.Values.FirstOrDefault(x =>
                    x.UnlistedSongs.Select(x => x.FilePath).Contains(song.FilePath));
            }

            ArtistAlbums = taskArtist.Result.ArtistAlbums;

            return this;
        }

        private static string ReplaceInvalidChars(string illegal)
        {
            string invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());

            return invalid.Aggregate(illegal, (current, c) => current.Replace(c.ToString(), "_"));
        }

        public static Artist DeserializeArtistHelperStatic(string filePath)
        {
            Artist artist = new();
            return artist.DeserializeArtistHelper(filePath);
        }

        [ProtoMember(2)]
        public string Name { get; set; }
        [ProtoMember(3)]
        public ConcurrentDictionary<string, Album> ArtistAlbums { get; set; } = new(StringComparer.InvariantCultureIgnoreCase);

        public bool IsSerialization { get; set; }
        public bool IsDestruction { get; set; }
        public bool IsToModify { get; private set; }

        public bool Equals([AllowNull] Artist other) => other?.Name == Name;

        public override int GetHashCode()
        {
            return $"{Name}".GetHashCode();
        }
    }
}
