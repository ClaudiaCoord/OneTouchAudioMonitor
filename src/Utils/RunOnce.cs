/*
 * Git: https://github.com/ClaudiaCoord/OneTouchAudioMonitor
 * Copyright (c) 2022 СС
 * License MIT.
*/
using System;
using System.Threading;

namespace OneTouchMonitor.Utils
{
    public class RunOnce
    {
        private Action<bool> act = (a) => { };
        private bool isrun = false;
        private long counter = 0L;

        public RunOnce() { }
        public RunOnce(Action<bool> a) => act = a;

        public bool IsUsing {
            get => Interlocked.Read(ref counter) > 0L;
            private set => Interlocked.Exchange(ref counter, value ? (counter + 1L) : ((counter > 0L) ? (counter - 1L) : 0L));
        }
        public bool IsRun {
            get => isrun || IsUsing;
            private set => isrun = value;
        }
        public long UsingCount => counter;
        public void Using() { IsUsing = true; act.Invoke(IsUsing); }
        public void UnUsing() { IsUsing = false; act.Invoke(IsUsing); }
        public bool Begin() { if (IsRun || IsUsing) { act.Invoke(false); return false; } IsUsing = IsRun = true; act.Invoke(IsRun); return IsRun; }
        public bool End() { if (IsRun) { IsUsing = IsRun = false; } act.Invoke(IsRun || IsUsing); return IsRun; }
        public void Invoke(bool b) => act.Invoke(b);
    }
}
