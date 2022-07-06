/*
 * Git: https://github.com/ClaudiaCoord/OneTouchAudioMonitor
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using OneTouchMonitor.Events;
using Windows.Media.MediaProperties;
using Windows.UI.Xaml;
using RES = Windows.ApplicationModel.Resources;

namespace OneTouchMonitor.Data
{
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    [XmlRoot(Namespace = "", IsNullable = false)]
    public class Configuration : INotifyPropertyChanged
    {
        private double volume = 0.8;
        private uint audiorate = 48000,
                     audiosamples = 16,
                     effectloudness = 1000;
        private bool ismono = false,
                     issound = false,
                     iswarning = false,
                     isaudioecho = false,
                     isaudiolimiter = false;
        private double effectdelay = 1000.0,
                       effectfeedback = 0.2,
                       effectwetdrymix = 0.5;
        private ApplicationTheme theme = ApplicationTheme.Dark;
        private AudioSelectorType btSelector = AudioSelectorType.None;
        private AudioSelectorType audioSelector = AudioSelectorType.None;
        private AudioEncodingQuality audioQuality = AudioEncodingQuality.Medium;

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));

        #region Bt
        [XmlIgnore]
        public List<BtDevice> BtAllDevices {
            get => Config.BtDevices.BaseAllDevices;
            set {
                Config.BtDevices.BaseAllDevices.Clear();
                Config.BtDevices.BaseAllDevices.AddRange(value);
                OnPropertyChanged();
            }
        }
        [XmlIgnore]
        public List<BtDevice> BtAwailDevices {
            get => Config.BtDevices.BaseAwailDevices;
            set {
                Config.BtDevices.BaseAwailDevices.Clear();
                Config.BtDevices.BaseAwailDevices.AddRange(value);
                OnPropertyChanged();
            }
        }
        public List<BtDevice> BtSelectedDevices {
            get => Config.BtDevices.BaseSelectedDevices;
            set {
                Config.BtDevices.BaseSelectedDevices.Clear();
                Config.BtDevices.BaseSelectedDevices.AddRange(value);
                OnPropertyChanged();
            }
        }
        #endregion

        #region AudioOut
        [XmlIgnore]
        public List<AudioDevice> AudioOutAwailDevices {
            get => Config.AudioOutDevices.BaseAwailDevices;
            set {
                Config.AudioOutDevices.BaseAwailDevices.Clear();
                Config.AudioOutDevices.BaseAwailDevices.AddRange(value);
                OnPropertyChanged();
            }
        }
        [XmlIgnore]
        public List<AudioDevice> AudioOutAllDevices {
            get => Config.AudioOutDevices.BaseAllDevices;
            set {
                Config.AudioOutDevices.BaseAllDevices.Clear();
                Config.AudioOutDevices.BaseAllDevices.AddRange(value);
                OnPropertyChanged();
            }
        }
        public List<AudioDevice> AudioOutSelectedDevices {
            get => Config.AudioOutDevices.BaseSelectedDevices;
            set {
                Config.AudioOutDevices.BaseSelectedDevices.Clear();
                Config.AudioOutDevices.BaseSelectedDevices.AddRange(value);
                OnPropertyChanged();
            }
        }
        #endregion

        #region AudioIn
        [XmlIgnore]
        public List<AudioDevice> AudioInAwailDevices {
            get => Config.AudioInDevices.BaseAwailDevices;
            set {
                Config.AudioInDevices.BaseAwailDevices.Clear();
                Config.AudioInDevices.BaseAwailDevices.AddRange(value);
                OnPropertyChanged();
            }
        }
        [XmlIgnore]
        public List<AudioDevice> AudioInAllDevices {
            get => Config.AudioInDevices.BaseAllDevices;
            set {
                Config.AudioInDevices.BaseAllDevices.Clear();
                Config.AudioInDevices.BaseAllDevices.AddRange(value);
                OnPropertyChanged();
            }
        }
        public List<AudioDevice> AudioInSelectedDevices {
            get => Config.AudioInDevices.BaseSelectedDevices;
            set {
                Config.AudioInDevices.BaseSelectedDevices.Clear();
                Config.AudioInDevices.BaseSelectedDevices.AddRange(value);
                OnPropertyChanged();
            }
        }
        #endregion

        public AudioSelectorType BtSelector {
            get => btSelector;
            set { btSelector = value; OnPropertyChanged(); }
        }
        public AudioSelectorType AudioSelector
        {
            get => audioSelector;
            set { audioSelector = value; OnPropertyChanged(); }
        }
        public double Volume {
            get => volume;
            set { volume = value; OnPropertyChanged(); }
        }
        public double AudioEq1 {
            get => Config.AudioCapture.AudioEq1;
            set { Config.AudioCapture.AudioEq1 = value; OnPropertyChanged(); }
        }
        public double AudioEq2 {
            get => Config.AudioCapture.AudioEq2;
            set { Config.AudioCapture.AudioEq2 = value; OnPropertyChanged(); }
        }
        public double AudioEq3 {
            get => Config.AudioCapture.AudioEq3;
            set { Config.AudioCapture.AudioEq3 = value; OnPropertyChanged(); }
        }
        public double AudioEq4 {
            get => Config.AudioCapture.AudioEq4;
            set { Config.AudioCapture.AudioEq4 = value; OnPropertyChanged(); }
        }
        public bool IsEqEnable {
            get => Config.AudioCapture.IsEqEnable;
            set { Config.AudioCapture.IsEqEnable = value; OnPropertyChanged(); }
        }
        public bool IsMono {
            get => ismono;
            set { ismono = value; OnPropertyChanged(); }
        }
        public bool IsSound {
            get => issound;
            set { issound = value; OnPropertyChanged(); }
        }
        public bool IsEffectEcho {
            get => isaudioecho;
            set { isaudioecho = value; OnPropertyChanged(); }
        }
        public bool IsEffectLimiter {
            get => isaudiolimiter;
            set { isaudiolimiter = value; OnPropertyChanged(); }
        }
        public double EffectDelay {
            get => effectdelay;
            set { effectdelay = value; OnPropertyChanged(); }
        }
        public double EffectFeedback {
            get => effectfeedback;
            set { effectfeedback = value; OnPropertyChanged(); }
        }
        public double EffectWetDryMix {
            get => effectwetdrymix;
            set { effectwetdrymix = value; OnPropertyChanged(); }
        }
        public uint EffectLoudness {
            get => effectloudness;
            set { effectloudness = value; OnPropertyChanged(); }
        }
        public uint OutAudioRate {
            get => audiorate;
            set { audiorate = value; OnPropertyChanged(); }
        }
        public uint OutAudioSample {
            get => audiosamples;
            set { audiosamples = value; OnPropertyChanged(); }
        }
        [XmlIgnore]
        public bool IsPlay {
            get => Config.AudioCapture.IsPlay;
            set => OnPropertyChanged();
        }
        [XmlIgnore]
        public bool IsInit {
            get => Config.AudioCapture.IsInit;
            set => OnPropertyChanged();
        }
        [XmlIgnore]
        public bool IsRecord {
            get => Config.AudioCapture.IsRecord;
            set => OnPropertyChanged();
        }
        [XmlIgnore]
        public bool IsWarning {
            get => iswarning;
            set { if (iswarning != value) iswarning = value; OnPropertyChanged(); }
        }
        [XmlIgnore]
        public ElementTheme EleTheme {
            get => (theme == ApplicationTheme.Dark) ? ElementTheme.Dark : ElementTheme.Light;
        }
        public ApplicationTheme Theme {
            get => theme;
            set { theme = value; OnPropertyChanged(); }
        }
        public AudioEncodingQuality AudioQuality {
            get => audioQuality;
            set { audioQuality = value; OnPropertyChanged(); }
        }

        public void BtOutDevicesAdd(List<BtDevice> list) { BtAwailDevices.Clear();  BtAwailDevices.AddRange(list); }
        public void AudioOutDevicesAdd(List<AudioDevice> list) { AudioOutSelectedDevices.Clear(); AudioOutSelectedDevices.AddRange(list); }

        public Configuration() { }

        public void Copy(Configuration cfg, bool isfull) {
            if (cfg == null)
                return;

            IDeviceEqualityComparer<AudioDevice> acmp = new();
            IDeviceEqualityComparer<BtDevice> bcmp = new();

            BtSelectedDevices = cfg.BtSelectedDevices.Distinct(bcmp).ToList();
            AudioInSelectedDevices = cfg.AudioInSelectedDevices.Distinct(acmp).ToList();
            AudioOutSelectedDevices = cfg.AudioOutSelectedDevices.Distinct(acmp).ToList();

            if (isfull) {
                BtAllDevices = cfg.BtAllDevices.Distinct(bcmp).ToList();
                BtAwailDevices = cfg.BtAwailDevices.Distinct(bcmp).ToList();

                AudioOutAllDevices = cfg.AudioOutAllDevices.Distinct(acmp).ToList();
                AudioOutAwailDevices = cfg.AudioOutAwailDevices.Distinct(acmp).ToList();

                AudioInAllDevices = cfg.AudioInAllDevices.Distinct(acmp).ToList();
                AudioInAwailDevices = cfg.AudioInAwailDevices.Distinct(acmp).ToList();
            }

            AudioSelector = cfg.AudioSelector;
            BtSelector = cfg.BtSelector;
            Volume = cfg.Volume;
            IsMono = cfg.IsMono;
            IsSound = cfg.IsSound;
            Theme = cfg.Theme;
        }
    }
    public static class Config
    {
        const string cfgname = $"{nameof(Configuration)}.cfg";
        private static RES.ResourceLoader resLoader = default;

        public static DeviceEventBT BtDevices { get; private set; } = new ();
        public static DeviceEventAudioOut AudioOutDevices { get; private set; } = new();
        public static DeviceEventAudioIn AudioInDevices { get; private set; } = new();
        public static AudioCapture AudioCapture { get; private set; } = new();
        public static Configuration Instance { get; } = new();

        public static async void Init() {

            AudioOutDevices.LogCb += (s, a) => { Debug.WriteLine($"{a.Sender.Name.HumanizeClassName()} - {a.Message}"); };
            AudioInDevices.LogCb += (s, a) => { Debug.WriteLine($"{a.Sender.Name.HumanizeClassName()} - {a.Message}"); };
            AudioCapture.LogCb += (s, a) => { Debug.WriteLine($"{a.Sender.Name.HumanizeClassName()} - {a.Message}"); };
            BtDevices.LogCb += (s, a) => { Debug.WriteLine($"{a.Sender.Name.HumanizeClassName()} - {a.Message}"); };

            await BtDevices.AutoInit().ConfigureAwait(false);
            await AudioInDevices.AutoInit().ConfigureAwait(false);
            await AudioOutDevices.AutoInit().ConfigureAwait(false);
            await AudioCapture.AutoInit().ConfigureAwait(false);
        }

        public static async void Reset(Action act = default) {

            AudioCapture.Stop();
            AudioInDevices.Stop();

            if (BtDevices.IsEnabled)
                BtDevices.Stop();

            if (AudioOutDevices.IsEnabled)
                AudioOutDevices.Stop();

            Task.Delay(250).Wait();

            AudioCapture.Dispose();
            AudioInDevices.Dispose();
            AudioOutDevices.Dispose();
            BtDevices.Dispose();

            if (act != default)
                act.Invoke();

            Task.Delay(250).Wait();

            AudioInDevices = new();
            AudioOutDevices = new();
            AudioCapture = new();
            BtDevices = new();

            await Load().ContinueWith((a) => Init()).ConfigureAwait(false);
        }

        public static async Task Save() =>
            await Task.Run(async () => {
                try {
                    await cfgname.SerializeToFile<Configuration>(Instance).ConfigureAwait(false);
                } catch (Exception ex) { Debug.WriteLine(ex.Message); }
            });
        public static async Task Load() =>
            await Task.Run(async () => {
                try {
                    Configuration cfg = await cfgname.DeserializeFromFile<Configuration>().ConfigureAwait(false);
                    Instance.Copy(cfg, false);
                } catch (Exception ex) { Debug.WriteLine(ex.Message); }
            });

        public static string GetString(string s) {
            if (resLoader == default) {
                if (Windows.UI.Core.CoreWindow.GetForCurrentThread() != null)
                    resLoader = RES.ResourceLoader.GetForCurrentView();
                if (resLoader == default) return string.Empty;
            }
            return resLoader.GetString(s);
        }
        public static string HumanizeClassName(this string s) {

            if (string.IsNullOrWhiteSpace(s)) return string.Empty;
            if (s.IndexOf(' ') != -1) return s;
            int end = s.Length - 1;
            StringBuilder sb = new();
            for (int i = 0; i < s.Length; i++)
            {
                int n = i + 1;
                bool b = (i > 0) && char.IsUpper(s[i]) && (n < s.Length) && !char.IsUpper(s[n]) && (i != end);
                sb.Append(b ? $" {s[i]}" : s[i]);
            }
            return sb.ToString();
        }
    }
}
