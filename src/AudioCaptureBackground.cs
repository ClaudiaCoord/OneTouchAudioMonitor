/*
 * Git: https://github.com/ClaudiaCoord/OneTouchAudioMonitor
 * Copyright (c) 2022 СС
 * License MIT.
*/
using System;
using Windows.Media.Playback;

namespace OneTouchMonitor
{
    internal class AudioCaptureBackground : IDisposable
    {
        private static AudioCaptureBackground instance;
        public static AudioCaptureBackground Instance {
            get {
                if (instance == null) instance = new();
                return instance;
            }
        }
        public MediaPlayer Player { get; private set; }
        public MediaPlaybackList PlaybackList {
            get => Player.Source as MediaPlaybackList;
            set => Player.Source = value;
        }
        public AudioCaptureBackground() {
            Player = new MediaPlayer();
            Player.AutoPlay = false;
            PlaybackList = new MediaPlaybackList();
        }
        public void Dispose() {
            MediaPlayer p = Player;
            Player = null;
            if (p != default) try { p.Dispose(); } catch { }
        }
    }
}
