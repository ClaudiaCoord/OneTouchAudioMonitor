/*
 * Git: https://github.com/ClaudiaCoord/OneTouchAudioMonitor
 * Copyright (c) 2022 СС
 * License MIT.
*/
using System;
using OneTouchMonitor.Data;
using Windows.Media.Audio;

namespace OneTouchMonitor
{
    public class AudioCaptureEqualizer : IDisposable
    {
        public EqualizerEffectDefinition eq { get; private set; }

        public double AudioEq1 {
            get => eq.Bands[0].Gain;
            set => eq.Bands[0].Gain = value;
        }
        public double AudioEq2 {
            get => eq.Bands[1].Gain;
            set => eq.Bands[1].Gain = value;
        }
        public double AudioEq3 {
            get => eq.Bands[2].Gain;
            set => eq.Bands[2].Gain = value;
        }
        public double AudioEq4 {
            get => eq.Bands[3].Gain;
            set => eq.Bands[3].Gain = value;
        }
        public bool IsEqEnable => Config.AudioCapture.IsEqEnable;

        public AudioCaptureEqualizer(AudioGraph ag) {
            eq = new(ag);

            eq.Bands[0].FrequencyCenter = 12000.0f;
            eq.Bands[0].Gain = 5.5958f;
            eq.Bands[0].Bandwidth = 2.0f;

            eq.Bands[1].FrequencyCenter = 5000.0f;
            eq.Bands[1].Gain = 2.4702f;
            eq.Bands[1].Bandwidth = 1.5f;

            eq.Bands[2].FrequencyCenter = 900.0f;
            eq.Bands[2].Gain = 1.6888f;
            eq.Bands[2].Bandwidth = 1.5f;

            eq.Bands[3].FrequencyCenter = 100.0f;
            eq.Bands[3].Gain = 4.033f;
            eq.Bands[3].Bandwidth = 1.5f;
        }
        ~AudioCaptureEqualizer() => Dispose();

        public void Dispose() {
            eq = null;
        }

        public void Init(AudioDeviceOutputNode adi) =>
            adi.EffectDefinitions.Add(eq);

        public void On(AudioDeviceOutputNode adi) =>
            adi.EnableEffectsByDefinition(eq);

        public void Off(AudioDeviceOutputNode adi) =>
            adi.DisableEffectsByDefinition(eq);

    }
}
