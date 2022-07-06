/*
 * Git: https://github.com/ClaudiaCoord/OneTouchAudioMonitor
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OneTouchMonitor.Data;
using OneTouchMonitor.Event;
using OneTouchMonitor.Events;
using OneTouchMonitor.Utils;
using Windows.ApplicationModel.Background;
using Windows.Media.Audio;
using Windows.Media.Capture;
using Windows.Media.MediaProperties;
using Windows.Media.Playback;
using Windows.Media.Render;
using Windows.Storage;

namespace OneTouchMonitor
{
    [Flags]
    public enum AudioSelectorType : int {
        None = 0,
        AutoPlay,
        BtDevice,
        AudioDevice
    }

    public sealed class AudioCapture : LogEvent, IBackgroundTask, IDisposable, IAutoInit {

        public AudioCapture() {}
        ~AudioCapture() => Dispose();

        public Task AutoInit() {
            Config.BtDevices.EventCb += BtDevices_EventCb;
            Config.AudioInDevices.EventCb += AudioInDevices_EventCb; ;
            Config.AudioOutDevices.EventCb += AudioOutDevices_EventCb;
            Config.Instance.PropertyChanged += Config_PropertyChanged;
            return Task.CompletedTask;
        }

        private double eq1val = 3.7,
                       eq2val = 3.1,
                       eq3val = 0.3,
                       eq4val = 2.2,
                       volval = 0.8;
        private bool eqenable = true;

        private BackgroundTaskDeferral deferral;
        private AudioGraph audioGraph { get; set; }
        private AudioDeviceOutputNode deviceOut { get; set; }
        private AudioDeviceInputNode deviceIn { get; set; }
        private AudioFileOutputNode fileOut { get; set; } = default;
        private AudioSubmixNode audioMix { get; set; } = default;
        private AudioDevice AudioOutDev { get; set; }
        private LimiterEffectDefinition effectLimit { get; set; } = default;
        private EchoEffectDefinition effectEcho { get; set; } = default;
        private RunOnce runOncePlay { get; set; } = new((b) => Config.Instance.IsPlay = b);
        private RunOnce runOnceInit { get; set; } = new((b) => Config.Instance.IsInit = b);
        private CancellationTokenSource tokenPlay { get; set; } = default(CancellationTokenSource);
        private MediaPlayer Player { get; set; } = default;

        public AudioCaptureEqualizer Equalizer { get; set; } = default;
        public bool IsRecord => IsPlay && (fileOut != default);
        public bool IsPlay => runOncePlay.IsRun || (AudioOutDev != default);
        public bool IsInit => runOnceInit.IsRun;
        private double Volume {
            get => volval;
            set { volval = value; if(deviceOut != null) deviceOut.OutgoingGain = volval; }
        }
        public double AudioEq1 {
            get => eq1val;
            set { eq1val = value; if (Equalizer != null) Equalizer.AudioEq1 = eq1val; }
        }
        public double AudioEq2 {
            get => eq2val;
            set { eq2val = value; if (Equalizer != null) Equalizer.AudioEq2 = eq2val; }
        }
        public double AudioEq3 {
            get => eq3val;
            set { eq3val = value; if (Equalizer != null) Equalizer.AudioEq3 = eq3val; }
        }
        public double AudioEq4 {
            get => eq4val;
            set { eq4val = value; if (Equalizer != null) Equalizer.AudioEq4 = eq4val; }
        }
        public bool IsEqEnable {
            get => eqenable;
            set {
                if (eqenable == value) return;
                eqenable = value;
                if ((Equalizer != null) && (deviceOut != null)) {
                    if (eqenable) Equalizer.On(deviceOut);
                    else Equalizer.Off(deviceOut);
                }
            }
        }

        #region Dispose
        public void Dispose()
        {
            CancellationTokenSource t = tokenPlay;
            tokenPlay = default;
            if ((t != null) && !t.IsCancellationRequested)
                t.Cancel();

            AudioGraph a = audioGraph;
            audioGraph = null;
            if (a != null)
                a.Stop();

            AudioCaptureEqualizer acq = Equalizer;
            Equalizer = default;
            if (acq != null)
                acq.Dispose();

            DisposeFileRecord();

            AudioSubmixNode m = audioMix;
            audioMix = default;
            if (m != null)
                m.Stop();

            AudioDeviceInputNode i = deviceIn;
            deviceIn = null;
            if (i != null)
                i.Stop();

            AudioDeviceOutputNode o = deviceOut;
            deviceOut = null;
            if (o != null)
                o.Stop();

            if (m != null)
                m.Dispose();

            if (i != null)
                i.Dispose();

            if (o != null)
                o.Dispose();

            if (a != null)
                a.Dispose();

            if (t != null)
                t.Dispose();

            if (AudioOutDev != default) {
                AudioOutDev.IsPlay = false;
                AudioOutDev = default;
            }
            runOncePlay.Invoke(false);
        }
        private void DisposeTokenPlay() {
            CancellationTokenSource t = tokenPlay;
            tokenPlay = default;
            if (t != null) {
                if (!t.IsCancellationRequested)
                    t.Cancel();
                t.Dispose();
            }
        }
        private void DisposeFileRecord() {
            try {
                AudioFileOutputNode f = fileOut;
                fileOut = default;
                if (f != null) {
                    f.Stop();
                    if (deviceIn != default)
                        deviceIn.RemoveOutgoingConnection(f);
                    ToLog(this, $"{Config.GetString("S10")}: '{f.File.Name}'");
                    f.Dispose();
                }
            } catch { }
        }
        #endregion

        public async void Run(IBackgroundTaskInstance bti) {
            deferral = bti.GetDeferral();
            while (IsPlay) { await Task.Delay(15).ConfigureAwait(false); }
            deferral.Complete();
            deferral = default;
        }

        #region Start/Stop Play
        public async void Start() => await StartAsync().ConfigureAwait(false);
        public async void Start(AudioSelectorType at) => await StartAsync(at).ConfigureAwait(false);
        public async void Start(AudioDevice dev) => await StartAsync(dev).ConfigureAwait(false);

        public async Task<bool> StartAsync() =>
            await Task.Run(async () => {

                if (IsPlay || !runOnceInit.Begin()) {
                    ToLog(this, $"{Config.GetString("S4")}: -> {IsPlay}/{IsInit}/{runOnceInit.UsingCount}");
                    return false;
                }

                try {
                    if (Config.Instance.AudioOutAllDevices.Count == 0) {
                        ToLog(this, Config.GetString("S2"));
                        return false;
                    }

                    DisposeTokenPlay();
                    tokenPlay = new();
                    Config.Instance.IsWarning = false;

                    if (Player == null)
                        Player = AudioCaptureBackground.Instance.Player;

                    AudioDevice dev = default;
                    CancellationToken t = tokenPlay.Token;
                    CancellationTokenSource cts = new(TimeSpan.FromSeconds(12));
                    while (!cts.IsCancellationRequested) {
                        bool[] b = new bool[] {
                            Config.Instance.BtSelector.HasFlag(AudioSelectorType.AutoPlay) &&
                                Config.Instance.BtSelector.HasFlag(AudioSelectorType.BtDevice),
                            Config.Instance.AudioSelector.HasFlag(AudioSelectorType.AutoPlay) &&
                                Config.Instance.AudioSelector.HasFlag(AudioSelectorType.AudioDevice)
                        };
                        Task.Delay(50).Wait();
                        dev = b[0] ? IsBtDeviceFound() : default;
                        if (dev != default) break;
                        dev = b[1] ? IsAudioOutDeviceFound() : default;
                        if (dev != default) break;
                        if (t.IsCancellationRequested) break;
                    }
                    cts.Dispose();
                    if ((dev == default) || dev.IsEmpty) {
                        ToLog(this, Config.GetString("S3"));
                        return false;
                    }
                    if (t.IsCancellationRequested) return false;
                    return await StartAsync(dev).ConfigureAwait(false);
                }
                catch (Exception ex) { ToLog(this, ex); Config.Instance.IsWarning = true; }
                finally { runOnceInit.End(); }
                return false;
            });

        public async Task<bool> StartAsync(AudioSelectorType at) =>
            await Task.Run(async () => {

                if (IsPlay || !runOnceInit.Begin()) {
                    ToLog(this, $"{Config.GetString("S4")}: '{at}' -> {IsPlay}/{IsInit}/{runOnceInit.UsingCount}");
                    return false;
                }

                try {
                    if (Config.Instance.AudioOutAllDevices.Count == 0) {
                        ToLog(this, Config.GetString("S2"));
                        return false;
                    }

                    DisposeTokenPlay();
                    tokenPlay = new();
                    Config.Instance.IsWarning = false;

                    if (Player == null)
                        Player = AudioCaptureBackground.Instance.Player;

                    AudioDevice dev = default;
                    CancellationToken t = tokenPlay.Token;
                    CancellationTokenSource cts = new(TimeSpan.FromSeconds(8));
                    while (!cts.IsCancellationRequested) {
                        switch (at) {
                            case AudioSelectorType.BtDevice: {
                                if (Config.Instance.BtSelector.HasFlag(AudioSelectorType.AutoPlay) &&
                                    Config.Instance.BtSelector.HasFlag(AudioSelectorType.BtDevice))
                                        dev = IsBtDeviceFound();
                                    break;
                                }
                            case AudioSelectorType.AudioDevice: {
                                    if (Config.Instance.AudioSelector.HasFlag(AudioSelectorType.AutoPlay) &&
                                        Config.Instance.AudioSelector.HasFlag(AudioSelectorType.AudioDevice))
                                        dev = IsAudioOutDeviceFound();
                                    break;
                                }
                        }
                        if (dev != default) break;
                        if (t.IsCancellationRequested) break;
                        Task.Delay(50).Wait();
                    }
                    cts.Dispose();
                    if ((dev == default) || dev.IsEmpty) {
                        ToLog(this, Config.GetString("S3"));
                        return false;
                    }
                    if (t.IsCancellationRequested) return false;
                    return await StartAsync(dev).ConfigureAwait(false);
                }
                catch (Exception ex) { ToLog(this, ex); Config.Instance.IsWarning = true; }
                finally { runOnceInit.End(); }
                return false;
            });

        public async Task<bool> StartAsync(AudioDevice dev) =>
            await Task.Run(async () => {

                if ((dev == default) || !runOncePlay.Begin()) {
                    if (dev == default)
                        ToLog(this, $"{Config.GetString("S4")} -> {IsPlay}/null");
                    else
                        ToLog(this, $"{Config.GetString("S4")} -> {IsPlay}/{dev.IsEmpty}/{dev.IsPlay}");
                    return false;
                }

                bool isinit = false;
                try {

                    DisposeTokenPlay();
                    Config.Instance.IsWarning = false;

                    AudioDevice AudioInDev = IsAudioInDeviceFound();
                    if (AudioInDev == default) {
                        ToLog(this, Config.GetString("S8"));
                        return false;
                    }

                    tokenPlay = new();

                    if (Player == null)
                        Player = AudioCaptureBackground.Instance.Player;

                    AudioOutDev = dev;
                    AudioOutDev.IsPlay = true;

                    var result = await AudioGraph.CreateAsync(
                        new AudioGraphSettings(AudioRenderCategory.GameChat) {
                            PrimaryRenderDevice = AudioOutDev.Device
                    });
                    if (result.Status != AudioGraphCreationStatus.Success)
                        return false;

                    audioGraph = result.Graph;
                    Equalizer = new AudioCaptureEqualizer(audioGraph);
                    // audioGraph.QuantumProcessed += AudioGraph_QuantumProcessed;

#                   if FIXED_MIC_INPUT
                    AudioInDev.Device = await DeviceInformation.CreateFromIdAsync(
                        MediaDevice.GetDefaultAudioCaptureId(AudioDeviceRole.Default));
#                   endif

                    var inputResult = await audioGraph.CreateDeviceInputNodeAsync(
                        MediaCategory.GameChat,
                        AudioEncodingProperties.CreatePcm(Config.Instance.OutAudioRate, Config.Instance.IsMono ? 1U : 2U, Config.Instance.OutAudioSample),
                        AudioInDev.Device);
                    if (inputResult.Status != AudioDeviceNodeCreationStatus.Success)
                        return false;

                    deviceIn = inputResult.DeviceInputNode;

                    var outputResult = await audioGraph.CreateDeviceOutputNodeAsync();
                    if (outputResult.Status != AudioDeviceNodeCreationStatus.Success)
                        return false;

                    deviceOut = outputResult.DeviceOutputNode;
                    deviceOut.OutgoingGain = volval;

                    Equalizer.Init(deviceOut);
                    Equalizer.AudioEq1 = eq1val;
                    Equalizer.AudioEq2 = eq2val;
                    Equalizer.AudioEq3 = eq3val;
                    Equalizer.AudioEq4 = eq4val;

                    audioMix = audioGraph.CreateSubmixNode();
                    deviceIn.AddOutgoingConnection(audioMix);
                    audioMix.AddOutgoingConnection(deviceOut);

                    AddEffectLimiter();
                    AddEffectEcho();

                    audioGraph.Start();
                    if (IsEqEnable) Equalizer.On(deviceOut);
                    else Equalizer.Off(deviceOut);

                    isinit = true;
                    return isinit;
                }
                catch (Exception ex) {
                    ToLog(this, ex);
                    isinit = false;
                    Config.Instance.IsWarning = true;
                }
                finally {
                    
                    if (!isinit) {
                        if (AudioOutDev != null)
                            ToLog(this, $"{Config.GetString("S5")}: '{AudioOutDev.Name}'");
                        Dispose();
                    } else {
                        if (AudioOutDev != null)
                            ToLog(this, $"{Config.GetString("S6")}: -> '{AudioOutDev.Name}'");
                    }
                    runOncePlay.End();
                }
                return false;
            });

        public async void Stop() => await StopAsync().ConfigureAwait(false);
        public async Task<bool> StopAsync() =>
            await Task<bool>.Run(async () => {
                if (!IsPlay) return false;

                try {
                    if ((tokenPlay != null) && !tokenPlay.IsCancellationRequested)
                        tokenPlay.Cancel();
                    CancellationTokenSource token = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                    while (runOnceInit.IsRun) {
                        if (token.IsCancellationRequested) break;
                        await Task.Delay(100).ConfigureAwait(false);
                    }
                    if (runOnceInit.IsRun) {
                        ToLog(this, $"{Config.GetString("S1")} -> {IsPlay}/{IsInit}/{runOnceInit.UsingCount}");
                        Config.Instance.IsWarning = true;
                        return false;
                    }
                    Dispose();
                    Config.Instance.IsWarning = false;
                    return true;
                }
                catch (Exception ex) { ToLog(this, ex); }
                finally { runOnceInit.End(); }
                return false;
            });
        #endregion

        #region Start/Stop Record
        public async Task<bool> StartRecord() =>
            await Task.Run(async () => {
                try {
                    if (IsRecord || (deviceIn == default)) return false;
                    DisposeFileRecord();

                    DateTime now = DateTime.Now;
                    StorageFolder folder = KnownFolders.MusicLibrary;
                    if (folder == default) return false;

                    StorageFile file = await folder.CreateFileAsync(
                        $"{deviceIn.Device.Name}-{now.Year}-{now.Month}-{now.Day}-{now.Hour}-{now.Minute}-{now.Second}.mp3",
                        CreationCollisionOption.ReplaceExisting);
                    if (file == default) return false;

                    var fileResult = await audioGraph.CreateFileOutputNodeAsync(
                        file, MediaEncodingProfile.CreateMp3(Config.Instance.AudioQuality));
                    if (fileResult.Status != AudioFileNodeCreationStatus.Success) return false;

                    fileOut = fileResult.FileOutputNode;
                    fileOut.OutgoingGain = volval;
                    if (deviceIn == default) DisposeFileRecord();
                    else {
                        deviceIn.AddOutgoingConnection(fileOut);
                        ToLog(this, $"{Config.GetString("S9")}: '{fileOut.File.Name}'");
                        System.Diagnostics.Debug.WriteLine(fileOut.File.Path);
                        return true;
                    }
                }
                catch (Exception ex) { ToLog(this, ex); Config.Instance.IsWarning = true; }
                return false;
            });

        public bool StopRecord() {
            try {
                if (!IsRecord || (fileOut == default)) return false;
                DisposeFileRecord();
                Config.Instance.IsWarning = false;
                return true;
            }
            catch (Exception ex) { ToLog(this, ex); Config.Instance.IsWarning = true; }
            return false;
        }

        public void AddEffectLimiter() {
            if ((audioMix == default) || (audioGraph == default)) return;
            try {
                if (Config.Instance.IsEffectLimiter) {
                    if (effectLimit == default)
                        effectLimit = new LimiterEffectDefinition(audioGraph);
                    effectLimit.Loudness = Config.Instance.EffectLoudness;
                    audioMix.EffectDefinitions.Add(effectLimit);
                } else if (effectLimit != default) {
                    audioMix.EffectDefinitions.Remove(effectLimit);
                }
            }
            catch (Exception ex) {
                ToLog(this, ex);
                Config.Instance.IsWarning = true;
            }
        }
        public void AddEffectEcho() {
            if ((audioMix == default) || (audioGraph == default)) return;
            try {
                if (Config.Instance.IsEffectEcho) {
                    if (effectEcho == default)
                        effectEcho = new EchoEffectDefinition(audioGraph);
                    effectEcho.Delay = Config.Instance.EffectDelay;
                    effectEcho.Feedback = Config.Instance.EffectFeedback;
                    effectEcho.WetDryMix = Config.Instance.EffectWetDryMix;
                    audioMix.EffectDefinitions.Add(effectEcho);
                } else if (effectEcho != default) {
                    audioMix.EffectDefinitions.Remove(effectEcho);
                }
            } catch (Exception ex) {
                ToLog(this, ex);
                Config.Instance.IsWarning = true;
            }
        }
        #endregion

        #region private utils
        public async Task FindDevice<T>(T d) where T : IDevice =>
            await Task.Run(async () => {
                try {
                    if (IsPlay) return;
                    runOnceInit.Using();

                    AudioDevice dev = default;
                    CancellationTokenSource cts = new(TimeSpan.FromSeconds(8));
                    while (!cts.IsCancellationRequested) {
                        Task.Delay(50).Wait();
                        if ((dev = GetAudioBtDevice(d.Name)) != default) break;
                    }
                    cts.Dispose();
                    if ((dev != default) && !dev.IsEmpty && !IsPlay) {
                        ToLog(this, string.Format(Config.GetString("FMT2"), typeof(T).Name, d.Name));
                        await StartAsync(dev).ConfigureAwait(false);
                    }
                } catch { }
                finally { runOnceInit.UnUsing(); }
            });


        private AudioDevice IsBtDeviceFound() {
            foreach (var s in Config.Instance.BtSelectedDevices) {
                var dev = GetAudioBtDevice(s.Name);
                if (dev != null)
                    return dev;
            }
            return default;
        }
        private AudioDevice IsAudioOutDeviceFound() {
            foreach (var s in Config.Instance.AudioOutSelectedDevices) {
                var dev = GetAudioOutLocalDevice(s.Name);
                if (dev != null)
                    return dev;
            }
            return default;
        }
        private AudioDevice IsAudioInDeviceFound() {
            foreach (var s in Config.Instance.AudioInSelectedDevices) {
                var dev = GetAudioInLocalDevice(s.Name);
                if (dev != null)
                    return dev;
            }
            return default;
        }

        private AudioDevice GetAudioInLocalDevice(string s) {
            return (from i in Config.Instance.AudioInAwailDevices
                    where i.CompareAudioName(s)
                    select i).FirstOrDefault();
        }
        private AudioDevice GetAudioOutLocalDevice(string s) {
            return (from i in Config.Instance.AudioOutAwailDevices
                    where i.CompareAudioName(s)
                    select i).FirstOrDefault();
        }
        private AudioDevice GetAudioBtDevice(string s) {
            return (from i in Config.Instance.AudioOutAwailDevices
                    where i.CompareBTName(s)
                    select i).FirstOrDefault();
        }
        #endregion

        #region events
        private bool GetConfigAudioSelector(AudioSelectorType sel) =>
            Config.Instance.AudioSelector.HasFlag(sel);

        private bool GetConfigBtSelector(AudioSelectorType sel) =>
            Config.Instance.BtSelector.HasFlag(sel);

        private void Config_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            switch (e.PropertyName) {
                case nameof(Configuration.Volume): Volume = Config.Instance.Volume; break;
                case nameof(Configuration.EffectLoudness): {
                        if (effectLimit != default)
                            effectLimit.Loudness = Config.Instance.EffectLoudness;
                        break;
                    }
                case nameof(Configuration.EffectDelay): {
                        if (effectEcho != default)
                            effectEcho.Delay = Config.Instance.EffectDelay;
                        break;
                    }
                case nameof(Configuration.EffectFeedback): {
                        if (effectEcho != default)
                            effectEcho.Feedback = Config.Instance.EffectFeedback;
                        break;
                    }
                case nameof(Configuration.EffectWetDryMix): {
                        if (effectEcho != default)
                            effectEcho.WetDryMix = Config.Instance.EffectWetDryMix;
                        break;
                    }
            }
        }
        private async void EventsCb<T1>(object obj, BaseEventArgs<T1> args) where T1 : IDevice, new()
        {
            try {
                AudioSelectorType type = (typeof(T1) == typeof(AudioDevice)) ? AudioSelectorType.AudioDevice :
                                            ((typeof(T1) == typeof(BtDevice)) ? AudioSelectorType.BtDevice : AudioSelectorType.None);
                if (type == AudioSelectorType.None)
                    return;
                if (IsPlay) {
                    if ((args.ActionId == DevicesEvents.Remove) && (AudioOutDev != default)) {
                        if (args.Obj is T1 data) {
                            if (((type == AudioSelectorType.AudioDevice) && AudioOutDev.CompareAudioName(data.Name)) ||
                                ((type == AudioSelectorType.BtDevice) && AudioOutDev.CompareBTName(data.Name))) {
                                ToLog(this, string.Format(Config.GetString("FMT1"), type.ToString(), DevicesEvents.Remove.ToString(), data.Name));
                                await StopAsync().ConfigureAwait(false);
                            }
                        }
                        else if ((type == AudioSelectorType.BtDevice) && (args.ActionId == DevicesEvents.RemoveAll)) {
                            ToLog(this, string.Format(Config.GetString("FMT4"), DevicesEvents.RemoveAll.ToString()));
                            await StopAsync().ConfigureAwait(false);
                        }
                    }
                } else {
                    if (((type == AudioSelectorType.BtDevice) && GetConfigBtSelector(AudioSelectorType.AutoPlay) && GetConfigBtSelector(AudioSelectorType.AudioDevice)) ||
                        ((type == AudioSelectorType.AudioDevice) && GetConfigAudioSelector(AudioSelectorType.AutoPlay) && GetConfigAudioSelector(AudioSelectorType.AudioDevice))) {
                        if (args.ActionId == DevicesEvents.Add) {
                            if (args.Obj is T1 data)
                                await FindDevice(data).ConfigureAwait(false);
                        }
                        else if ((args.ActionId == DevicesEvents.Enable) || (args.ActionId == DevicesEvents.Reload))
                            await StartAsync(type).ConfigureAwait(false);
                    }
                }
            } catch (Exception ex) { ToLog(this, ex); }
        }
        private void AudioOutDevices_EventCb(object obj, BaseEventArgs<AudioDevice> args) =>
            EventsCb(obj, args);
        private void BtDevices_EventCb(object obj, BaseEventArgs<BtDevice> args) =>
            EventsCb(obj, args);

        private async void AudioInDevices_EventCb(object obj, BaseEventArgs<AudioDevice> args) {

            if (IsPlay) {
                if ((args.ActionId == DevicesEvents.Remove) && (AudioOutDev != default)) {
                    if (args.Obj is AudioDevice data) {
                        if (AudioOutDev.CompareAudioName(data.Name)) {
                            ToLog(this, string.Format(Config.GetString("FMT1"), nameof(AudioDevice), DevicesEvents.Remove.ToString(), data.Name));
                            await StopAsync().ConfigureAwait(false);
                        }
                    }
                    else if (args.ActionId == DevicesEvents.RemoveAll) {
                        ToLog(this, string.Format(Config.GetString("FMT4"), DevicesEvents.RemoveAll.ToString()));
                        await StopAsync().ConfigureAwait(false);
                    }
                }
            }
        }
        #endregion


        /*
        private void AudioGraph_QuantumProcessed(AudioGraph sender, object args) {
            using (AudioBuffer buffer = deviceOut.GetFrame().LockBuffer(AudioBufferAccessMode.Write))
            using (IMemoryBufferReference reference = buffer.CreateReference())
            {
                byte* dataInBytes;
                uint capacityInBytes;
                float* dataInFloat;

                // Get the buffer from the AudioFrame
                ((IMemoryBufferByteAccess)reference).GetBuffer(out dataInBytes, out capacityInBytes);

                dataInFloat = (float*)dataInBytes;


            }
            dataInFloat = (float*)dataInBytes;
            float max = 0;
            for (int i = 0; i < sender.SamplesPerQuantum; i++)
            {
                max = Math.Max(Math.Abs(dataInFloat[i]), max);

            }

            finalLevel = max;
            Debug.WriteLine(max);
        }
        */


    }
}
