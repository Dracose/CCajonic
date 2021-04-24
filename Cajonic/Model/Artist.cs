using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
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

        public Artist(Track track)
        {
            Name = track.Artist;
            string copyName = ReplaceInvalidChars(Name);
            BinaryFilePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), $"Cajonic\\SaveData\\Artists\\{copyName}.bin");
            Album newAlbum = new Album(track);
            if (track.DiscTotal != 0 && track.DiscTotal != 1)
            {
                Album newCd = new Album(track)
                {
                    CDs = null
                };
                newAlbum.CDs.TryAdd(track.DiscNumber, newCd);
            }
            ArtistAlbums.TryAdd(track.Album, newAlbum);
            IsToModify = true;
        }


        public async void SerializeArtistAsync()
        {
            IsSerialization = false;
            IsToModify = false;
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

            ImmutableList<Song> allAlbumSongs = taskArtist.Result.ArtistAlbums.
                SelectMany(x => x.Value.AlbumSongCollection.Values).ToImmutableList();

            ImmutableList<Song> allCdSongs = taskArtist.Result.ArtistAlbums.
                SelectMany(x => x.Value.CDs.Values).
                SelectMany(x => x.AlbumSongCollection.Values).ToImmutableList();

            foreach (Song song in allAlbumSongs)
            {
                song.Artist = this;
                song.Album = taskArtist.Result.ArtistAlbums.FirstOrDefault(x => x.Value.AlbumSongCollection.Values.Contains(song)).Value;
            }

            foreach (Song song in allCdSongs)
            {
                song.Artist = this;
                song.Album = taskArtist.Result.ArtistAlbums.SelectMany(x => x.Value.CDs)
                    .FirstOrDefault(x => x.Key == song.DiscNumber).Value;
                song.Album.CDs = null;
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
            Artist artist = new Artist();
            return artist.DeserializeArtistHelper(filePath);
        }

        [ProtoMember(2)]
        public string Name { get; set; }
        [ProtoMember(3)]
        public ConcurrentDictionary<string, Album> ArtistAlbums { get; set; } = new ConcurrentDictionary<string, Album>();

        public bool IsSerialization { get; set; }
        public bool IsToModify { get; set; }

        public bool Equals([AllowNull] Artist other) => other?.Name == Name;

        public override int GetHashCode()
        {
            return $"{Name}".GetHashCode();
        }
    }
}
