using System.Collections.Generic;
using ATL;
using ProtoBuf;

namespace Cajonic.Services.Wrappers
{
    [ProtoContract]
    public class SerializableLyricsInfo
    {
        [ProtoMember(1)]
        public LyricsInfo.LyricsType ContentType { get; set; }
        [ProtoMember(2)]
        public string Description { get; set; }
        [ProtoMember(3)]
        public string LanguageCode { get; set; }
        [ProtoMember(4)]
        public string UnsynchronizedLyrics { get; set; }
        [ProtoMember(5)]
        public IList<SerializableLyricsPhrase> SynchronizedLyrics { get; set; }

        public SerializableLyricsInfo()
        {
            Description = "";
            LanguageCode = "";
            UnsynchronizedLyrics = "";
            ContentType = LyricsInfo.LyricsType.LYRICS;
            SynchronizedLyrics = new List<SerializableLyricsPhrase>();
        }

        public SerializableLyricsInfo(LyricsInfo info)
        {
            Description = info.Description;
            LanguageCode = info.LanguageCode;
            UnsynchronizedLyrics = info.UnsynchronizedLyrics;
            ContentType = info.ContentType;
            SynchronizedLyrics = SerializableLyricsPhrase.LyricsPhraseListCasting(info.SynchronizedLyrics);
        }
    }
}
