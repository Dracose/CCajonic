using System;
using System.Collections.Generic;
using ATL;

namespace Cajonic.Services.Wrappers
{
    [Serializable]
    public class SerializableLyricsInfo
    {
        public LyricsInfo.LyricsType ContentType { get; set; }

        public string Description { get; set; }

        public string LanguageCode { get; set; }

        public string UnsynchronizedLyrics { get; set; }

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
