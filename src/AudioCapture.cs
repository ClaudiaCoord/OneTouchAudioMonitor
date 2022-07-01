/*
 * Git: https://github.com/ClaudiaCoord/OneTouchAudioMonitor
 * Copyright (c) 2022 СС
 * License MIT.
*/
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OneTouchMonitor.Data;
using OneTouchMonitor.Event;
using OneTouchMonitor.Events;
using Windows.ApplicationModel.Background;
using Windows.Devices.Enumeration;
using Windows.Foundation;
using Windows.Media;
using Windows.Media.Audio;
using Windows.Media.Capture;
using Windows.Media.Devices;
using Windows.Media.MediaProperties;
using Windows.Media.Render;
using Windows.UI.Xaml.Controls;

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
        private AudioDevice AudioOutDev { get; set; }
        private RunOnce runOncePlay { get; set; } = new((b) => Config.Instance.IsPlay = b);
        private RunOnce runOnceInit { get; set; } = new((b) => Config.Instance.IsInit = b);
        private CancellationTokenSource token { get; set; } = default(CancellationTokenSource);

        public AudioCaptureEqualizer Equalizer { get; set; } = default;
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
            CancellationTokenSource t = token;
            token = default;
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

            AudioDeviceInputNode i = deviceIn;
            deviceIn = null;
            if (i != null)
                i.Stop();

            AudioDeviceOutputNode o = deviceOut;
            deviceOut = null;
            if (o != null)
                o.Stop();

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
        private void DisposeTokenSrc() {
            CancellationTokenSource t = token;
            token = default;
            if (t != null) {
                if (!t.IsCancellationRequested)
                    t.Cancel();
                t.Dispose();
            }
        }
        #endregion

        public async void Run(IBackgroundTaskInstance bti)
        {
            deferral = bti.GetDeferral();
            while (IsPlay) { await Task.Delay(15).ConfigureAwait(false); }
            deferral.Complete();
            deferral = default;
        }

        #region Start/Stop
        public async Task<bool> Start() =>
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

                    DisposeTokenSrc();
                    token = new();

                    AudioDevice dev = default;
                    CancellationToken t = token.Token;
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
                    return await Start(dev).ConfigureAwait(false);
                }
                catch (Exception ex) { ToLog(this, ex); }
                finally { runOnceInit.End(); }
                return false;
            });

        public async Task<bool> Start(AudioSelectorType at) =>
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

                    DisposeTokenSrc();
                    token = new();

                    AudioDevice dev = default;
                    CancellationToken t = token.Token;
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
                    return await Start(dev).ConfigureAwait(false);
                }
                catch (Exception ex) { ToLog(this, ex); }
                finally { runOnceInit.End(); }
                return false;
            });

        public async Task<bool> Start(AudioDevice dev) =>
            await Task.Run(async () => {

                if ((dev == default) || !runOncePlay.Begin()) {
                    ToLog(this, $"{Config.GetString("S4")} -> {IsPlay}/{dev.IsEmpty}/{dev.IsPlay}");
                    return false;
                }

                bool isinit = false;
                try {

                    DisposeTokenSrc();

                    AudioDevice AudioInDev = IsAudioInDeviceFound();
                    if (AudioInDev == default) {
                        ToLog(this, Config.GetString("S8"));
                        return false;
                    }

                    token = new();

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
                        MediaEncodingProfile.CreateWav(
                            Config.Instance.IsMono ? AudioEncodingQuality.Low : AudioEncodingQuality.Medium).Audio,
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

                    deviceIn.AddOutgoingConnection(deviceOut);
                    audioGraph.Start();
                    if (IsEqEnable) Equalizer.On(deviceOut);
                    else Equalizer.Off(deviceOut);

                    isinit = true;
                    return isinit;
                }
                catch (Exception ex) {
                    ToLog(this, ex);
                    isinit = false;
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

        public void Stop() {
            if (!IsPlay || !runOnceInit.Begin()) {
                ToLog(this, $"{Config.GetString("S1")} -> {IsPlay}/{IsInit}/{runOnceInit.UsingCount}");
                return;
            }
            try { Dispose(); }
            catch (Exception ex) { ToLog(this, ex); }
            finally { runOnceInit.End(); }
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
                        await Start(dev).ConfigureAwait(false);
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
                                Stop();
                            }
                        }
                        else if ((type == AudioSelectorType.BtDevice) && (args.ActionId == DevicesEvents.RemoveAll)) {
                            ToLog(this, string.Format(Config.GetString("FMT4"), DevicesEvents.RemoveAll.ToString()));
                            Stop();
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
                            await Start(type).ConfigureAwait(false);
                    }
                }
            } catch (Exception ex) { ToLog(this, ex); }
        }
        private void AudioOutDevices_EventCb(object obj, BaseEventArgs<AudioDevice> args) =>
            EventsCb(obj, args);
        private void BtDevices_EventCb(object obj, BaseEventArgs<BtDevice> args) =>
            EventsCb(obj, args);

        private void AudioInDevices_EventCb(object obj, BaseEventArgs<AudioDevice> args)
        {
            if (IsPlay)
            {
                if ((args.ActionId == DevicesEvents.Remove) && (AudioOutDev != default)) {
                    if (args.Obj is AudioDevice data) {
                        if (AudioOutDev.CompareAudioName(data.Name)) {
                            ToLog(this, string.Format(Config.GetString("FMT1"), nameof(AudioDevice), DevicesEvents.Remove.ToString(), data.Name));
                            Stop();
                        }
                    }
                    else if (args.ActionId == DevicesEvents.RemoveAll) {
                        ToLog(this, string.Format(Config.GetString("FMT4"), DevicesEvents.RemoveAll.ToString()));
                        Stop();
                    }
                }
            }
        }
        #endregion
    }
}
