using ATL;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Windows.Media.Imaging;
using Cajonic.Services;
using Meziantou.Framework.WPF.Collections;
using ProtoBuf;

namespace Cajonic.Model
{
    [ProtoContract]
    public class Song : IEquatable<Song>, IComparable<Song>
    {
        [ProtoMember(1)] private string mFilePath;

        private BitmapImage mByteArtwork;

        public BitmapImage ByteArtwork
        {
            get => mByteArtwork;
            set => mByteArtwork = value;
        }

        public Song(Track track)
        {
            FilePath = track.Path;
            Title = string.IsNullOrEmpty(track.Title) ? Path.GetFileName(FilePath) : track.Title;
            ArtistName = string.IsNullOrEmpty(track.Artist) ? string.Empty : track.Artist;
            AlbumTitle = string.IsNullOrEmpty(track.Album) ? string.Empty : track.Album;
            AlbumArtist = string.IsNullOrEmpty(track.AlbumArtist) ? string.Empty : track.AlbumArtist;
            Genre = string.IsNullOrEmpty(track.Genre) ? string.Empty : track.Genre;
            Year = track.Year == 0 ? null : track.Year;
            TrackNumber = track.TrackNumber == 0 ? null : track.TrackNumber;
            Duration = TimeSpan.FromMilliseconds(track.DurationMs);
            DiscNumber = track.DiscNumber == 0 ? null : track.DiscNumber;
            
            //This is very costly !
            //ByteArtwork = BitmapHelper.LoadImage(track.EmbeddedPictures.FirstOrDefault()?.PictureData);
        }

        public Song()
        {
        }

        [ProtoMember(3)] public string ArtistName { get; set; }
        [ProtoMember(4)] public string AlbumTitle { get; set; }
        [ProtoMember(5)] public string Title { get; set; }
        public Artist Artist { get; set; }
        [ProtoMember(7)] public string AlbumArtist { get; set; }
        public Album Album { get; set; }
        [ProtoMember(10)] public string Genre { get; set; }
        [ProtoMember(11)] public int? Year { get; set; }
        [ProtoMember(12)] public int? TrackNumber { get; set; }
        [ProtoMember(13)] public int? DiscNumber { get; set; }
        [ProtoMember(14)] public int? PlayCount { get; set; }
        [ProtoMember(17)] public TimeSpan Duration { get; }
        public bool HasBeenEdited { get; set; }

        public string DisplayDuration => Duration.Days != 0 ? Duration.ToString("dd\\:hh\\:mm\\:ss") : 
            Duration.ToString(Duration.Hours != 0 ? "hh\\:mm\\:ss" : "mm\\:ss");

        public string DisplayYear => Year.NullableIntToString();

        public string FilePath
        {
            get => string.IsNullOrEmpty(mFilePath) ? string.Empty : mFilePath;
            private set => mFilePath = value;
        }

        public void EditSong(Song toCopy, Artist newArtist, Album newAlbum, 
            ConcurrentObservableCollection<Album> albums, ConcurrentObservableCollection<Artist> artists)
        {
            bool trackIsDifferent = Title != toCopy.Title || Year != toCopy.Year || AlbumArtist != toCopy.AlbumArtist ||
                                    Genre != toCopy.Genre || DiscNumber != toCopy.DiscNumber ||
                                    !ArtistName.Equals(toCopy.ArtistName,
                                        StringComparison.InvariantCultureIgnoreCase) ||
                                    !AlbumTitle.Equals(toCopy.AlbumTitle,
                                        StringComparison.InvariantCultureIgnoreCase) ||
                                    TrackNumber != toCopy.TrackNumber;

            if (!trackIsDifferent)
            {
                return;
            }

            Title = toCopy.Title;
            Year = toCopy.Year;
            AlbumArtist = toCopy.AlbumArtist;
            Genre = toCopy.Genre;
            ArtistName = toCopy.ArtistName;
            AlbumTitle = toCopy.AlbumTitle;
            
            Artist originalArtist = Artist;

            if (TrackNumber == null)
            {
                if (DiscNumber == null)
                {
                    Album.UnlistedSongs.TryRemove(this);
                    ManipulateSong(toCopy, newAlbum);
                }
                else
                {
                    Album.CDs[DiscNumber.Value].UnlistedSongs.TryRemove(this);
                    ManipulateSong(toCopy, newAlbum);
                }
            }
            else
            {
                if (DiscNumber == null)
                {
                    Album.AlbumSongCollection.TryRemove(TrackNumber.Value, out Song _);
                    ManipulateSong(toCopy, newAlbum);
                }
                else
                {
                    Album.CDs[DiscNumber.Value].SongCollection.TryRemove(TrackNumber.Value, out Song _);
                    ManipulateSong(toCopy, newAlbum);
                }
            }

            Artist = newArtist;
            Album = newAlbum;

            foreach (Album irrelevantAlbum in Artist.ArtistAlbums.Values.Where(x => x.AllSongs.IsEmpty))
            {
                Artist.ArtistAlbums.Remove(irrelevantAlbum.Title, out Album removedAlbum);
                albums.Remove(removedAlbum);
            }

            foreach (Album irrelevantAlbum in originalArtist.ArtistAlbums.Values.Where(x => x.AllSongs.IsEmpty))
            {
                originalArtist.ArtistAlbums.Remove(irrelevantAlbum.Title, out Album _);
                if (originalArtist.ArtistAlbums.Count == 0 ||
                    originalArtist.ArtistAlbums.Values.Count(x => x.AllSongs.IsEmpty) == originalArtist.ArtistAlbums.Count)
                {
                    originalArtist.IsDestruction = true;
                }
            }

            foreach (CD irrelevantCd in Artist.ArtistAlbums.Values.SelectMany(x => x.CDs.Values).Where(x =>
                x.SongCollection.IsEmpty))
            {
                Artist.ArtistAlbums.Values
                    .FirstOrDefault(x => x.HasCDs && x.CDs.ContainsKey(irrelevantCd.DiscNumber)
                                                  && x.CDs.Values.FirstOrDefault(z => z.Title == x.Title) != null)
                    ?.CDs.TryRemove(irrelevantCd.DiscNumber, out _);
            }

            HasBeenEdited = true;
            originalArtist.IsSerialization = true;

            Artist.IsSerialization = true;
        }
        
        private void UnlistSong(Song toGetToUnlisted)
        {
            if (toGetToUnlisted == null)
            {
                return;
            }
            
            Album.UnlistedSongs.TryAdd(toGetToUnlisted);

            Track theUnlistedTrack = new(toGetToUnlisted.FilePath)
            {
                Title = toGetToUnlisted.Title,
                Year = toGetToUnlisted.Year ?? 0,
                AlbumArtist = toGetToUnlisted.AlbumArtist,
                Genre = toGetToUnlisted.Genre,
                DiscNumber = toGetToUnlisted.DiscNumber ?? 0,
                Album = toGetToUnlisted.AlbumTitle,
                Artist = toGetToUnlisted.ArtistName,
                TrackNumber = toGetToUnlisted.TrackNumber ?? 0
            };
            theUnlistedTrack.Save();
        }

        private void ManipulateSong(Song toCopy, Album newAlbum)
        {
            SetDiscAndTrackNumber(toCopy);
            if (TrackNumber != null && DiscNumber != null)
            {
                newAlbum.CDs.TryAdd(DiscNumber.Value, new CD(this));
                bool result = newAlbum.CDs[DiscNumber.Value].SongCollection.TryAdd(TrackNumber.Value, this);
                            
                if (!result)
                {
                    Album.CDs[DiscNumber.Value].SongCollection.TryRemove(TrackNumber.Value, out Song toGetToUnlisted);
                    UnlistSong(toGetToUnlisted);
                    toGetToUnlisted.TrackNumber = null;
                    toGetToUnlisted.DiscNumber = DiscNumber;
                    
                    newAlbum.CDs.TryAdd(DiscNumber.Value, new CD(this));
                    newAlbum.CDs[DiscNumber.Value].SongCollection.TryAdd(TrackNumber.Value, this);
                }
            }

            if (TrackNumber == null && DiscNumber == null)
            {
                newAlbum.UnlistedSongs.TryAdd(this);
            }

            if (TrackNumber == null && DiscNumber.HasValue)
            {
                newAlbum.CDs.TryAdd(DiscNumber.Value, new CD(this));
                newAlbum.CDs[DiscNumber.Value].UnlistedSongs.TryAdd(this);
            }

            if (!TrackNumber.HasValue || DiscNumber != null)
            {
                return;
            }

            {
                bool result = newAlbum.AlbumSongCollection.TryAdd(TrackNumber.Value, this);

                if (result)
                {
                    return;
                }

                Album.AlbumSongCollection.TryRemove(TrackNumber.Value, out Song toGetToUnlisted);
                UnlistSong(toGetToUnlisted);

                if (toGetToUnlisted?.TrackNumber == null)
                {
                    return;
                }

                newAlbum.AlbumSongCollection.TryAdd(toGetToUnlisted.TrackNumber.Value, this);
                toGetToUnlisted.TrackNumber = null;
                toGetToUnlisted.DiscNumber = null;
            }
        }

        private void SetDiscAndTrackNumber(Song song)
        {
            TrackNumber = song.TrackNumber;
            DiscNumber = song.DiscNumber;
        }

        public int CompareTo(Song other)
        {
            return string.Compare(FilePath, other?.FilePath, StringComparison.Ordinal);
        }

        public override int GetHashCode() => FilePath?.GetHashCode() ?? base.GetHashCode();

        public bool Equals([AllowNull] Song other) => other?.FilePath == FilePath;
    }
}