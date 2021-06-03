using System;

namespace Cajonic.Services
{
    public interface IMusicPlayer
    {
        void Play(Uri filePath);
        void Play();
        void Pause();
        void Stop();
        void FastForward(double milliseconds);
        void Rewind(double milliseconds);
        bool IsDone();
    }
}