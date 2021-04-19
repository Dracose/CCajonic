using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows.Media.Imaging;
using Cajonic.Services;
using ProtoBuf;

namespace Cajonic.Model
{
    [ProtoContract]
    public class Album : IEquatable<Album>
    {
        public BitmapImage Artwork => mByteArtwork != null ? BitmapHelper.LoadImage(mByteArtwork) : null;

        private byte[] mByteArtwork;

        public Album()
        {
            Title = string.Empty;
            ArtistName = string.Empty;
            mByteArtwork = null;
            mByteArtwork = null;
        }

        public Album(Song song)
        {
            Title = song.AlbumTitle;
            ArtistName = song.ArtistName;
            mByteArtwork = song.ByteArtwork;
            AlbumSongCollection.Add(song);
        }

        [ProtoMember(2)]
        public string Title { get; set; }
        [ProtoMember(3)]
        public string ArtistName { get; set; }
        [ProtoMember(4)]
        public ObservableCollection<Song> AlbumSongCollection { get; set; } = new ObservableCollection<Song>();

        public bool Equals(Album other) => other?.Title == Title && other?.ArtistName == ArtistName;
    }
}
