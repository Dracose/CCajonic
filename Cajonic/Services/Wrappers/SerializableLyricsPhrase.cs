using System;
using System.Collections.Generic;
using System.Linq;
using ATL;
using Commons;

namespace Cajonic.Services.Wrappers
{
    [Serializable]
    public class SerializableLyricsPhrase
    {
        public int TimestampMs { get; set; }

        public string Text { get; set; }

        public SerializableLyricsPhrase(int timestampMs, string text)
        {
            TimestampMs = timestampMs;
            Text = text;
        }

        public SerializableLyricsPhrase(string timestamp, string text)
        {
            TimestampMs = Utils.DecodeTimecodeToMs(timestamp);
            Text = text;
        }

        public SerializableLyricsPhrase(LyricsInfo.LyricsPhrase phrase)
        {
            TimestampMs = phrase.TimestampMs;
            Text = phrase?.Text;
        }

        public static IList<SerializableLyricsPhrase> LyricsPhraseListCasting(IEnumerable<LyricsInfo.LyricsPhrase> list)
        {
            return list.Select(element => new SerializableLyricsPhrase(element)).ToList();
        }
    }
}
