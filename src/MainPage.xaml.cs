/*
 * Git: https://github.com/ClaudiaCoord/OneTouchAudioMonitor
 * Copyright (c) 2022 СС
 * License MIT.
 */

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using OneTouchMonitor.Data;
using OneTouchMonitor.Event;
using Windows.Foundation;
using Windows.Media;
using Windows.System;
using Windows.System.Threading;
using Windows.UI.Core;
using Windows.UI.ViewManagement;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Navigation;

namespace OneTouchMonitor
{
    public sealed partial class MainPage : Page, INotifyPropertyChanged
    {
        public static Size Size = new Size(340, 410);
        public static string Title => Config.GetString("TITLE1");

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        private void OnPropertyChanged(params string[] names) {
            foreach (var name in names)
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
        }

        private bool isBTOn = false,
                     isAudioOn = false;
        private string logString = string.Empty;
        private ThreadPoolTimer LazyTimer = default;
        private SystemMediaTransportControls mediaCtrl = default;

        public MainPage() {
            this.InitializeComponent();
            DataContext = this;
        }

        public bool IsBTOn {
            get => isBTOn;
            set {
                isBTOn = value;
                if (value) {
                    Config.Instance.BtSelector = AudioSelectorType.AutoPlay | AudioSelectorType.BtDevice;
                    Config.BtDevices.Start();
                    LazyStartScan(AudioSelectorType.BtDevice);
                } else {
                    Config.Instance.AudioSelector = AudioSelectorType.None;
                    Config.AudioCapture.Stop();
                    Config.BtDevices.Stop();
                }
                AllPropertyChanged(nameof(IsBTOn));
            }
        }
        public bool IsBTAutoEnable =>
            !IsAudioOn && !Config.Instance.IsPlay;

        public bool IsAudioOn {
            get => isAudioOn;
            set {
                isAudioOn = value;
                if (value) {
                    Config.Instance.AudioSelector = AudioSelectorType.AutoPlay | AudioSelectorType.AudioDevice;
                    Config.AudioOutDevices.Start();
                    LazyStartScan(AudioSelectorType.AudioDevice);
                } else {
                    Config.Instance.AudioSelector = AudioSelectorType.None;
                    Config.AudioCapture.Stop();
                    Config.AudioOutDevices.Stop();
                }
                AllPropertyChanged(nameof(IsAudioOn));
            }
        }
        public bool IsAudioAutoEnable =>
            !IsBTOn && !Config.Instance.IsPlay;

        public bool IsPlay => !Config.Instance.IsPlay && !Config.Instance.IsInit && IsAudioInDevice && (IsAudioOn || IsBTOn);
        public bool IsStop => Config.Instance.IsPlay || Config.Instance.IsInit;
        public bool IsRecord => Config.Instance.IsRecord;
        public bool IsRecordEnable => IsStop && !Config.Instance.IsInit;
        public bool IsPlayStatus => Config.Instance.IsPlay;
        public bool IsInitStatus => Config.Instance.IsInit;
        public bool IsCallSettings => !IsInitStatus && !IsPlayStatus;
        public bool IsBtOutEnable => Config.BtDevices.IsEnabled && (Config.BtDevices.IsFoundDevices || Config.BtDevices.IsConnectedDevices);
        public bool IsAudioOutEnable => Config.AudioOutDevices.IsEnabled && (Config.AudioOutDevices.IsFoundDevices || Config.AudioOutDevices.IsConnectedDevices);
        public bool IsAudioInDevice => Config.Instance.AudioInSelectedDevices.Count > 0;
        public bool IsWarning => !IsAudioInDevice || ((Config.Instance.AudioOutSelectedDevices.Count == 0) && (Config.Instance.BtSelectedDevices.Count == 0));
        private bool IsOldPlayStatus { get; set; } = false;
        private bool IsOldBtStatus { get; set; } = false;
        private bool IsStoryboardStart { get; set; } = false;

        public string LogString {
            get => logString;
            set {
                logString = value;
                OnPropertyChanged();
                if (string.IsNullOrWhiteSpace(value) && IsStoryboardStart) {
                    Storyboard1.Stop(); IsStoryboardStart = false;
                }
                else if (!IsStoryboardStart) {
                    Storyboard1.Begin(); IsStoryboardStart = true;
                }
            }
        }
        public double Volume {
            get => Config.Instance.Volume;
            set {
                Config.Instance.Volume = value + 0.0;
                OnPropertyChanged();
            }
        }
        public double AudioEq1 {
            get => Config.Instance.AudioEq1;
            set { Config.Instance.AudioEq1 = value; OnPropertyChanged(); }
        }
        public double AudioEq2 {
            get => Config.Instance.AudioEq2;
            set { Config.Instance.AudioEq2 = value; OnPropertyChanged(); }
        }
        public double AudioEq3 {
            get => Config.Instance.AudioEq3;
            set { Config.Instance.AudioEq3 = value; OnPropertyChanged(); }
        }
        public double AudioEq4 {
            get => Config.Instance.AudioEq4;
            set { Config.Instance.AudioEq4 = value; OnPropertyChanged(); }
        }
        public bool IsEqEnable {
            get => Config.Instance.IsEqEnable;
            set {
                Config.Instance.IsEqEnable = value;
                OnPropertyChanged();
            }
        }

        #region Property Changed
        private void AllPropertyChanged(string s) {
            OnPropertyChanged(s,
                nameof(IsPlay),
                nameof(IsStop),
                nameof(IsRecord),
                nameof(IsPlayStatus),
                nameof(IsInitStatus),
                nameof(IsCallSettings),
                nameof(IsAudioInDevice),
                nameof(IsAudioOutEnable),
                nameof(IsBtOutEnable),
                nameof(IsWarning),
                nameof(IsRecordEnable));
            BtPropertyChanged();
            AudioPropertyChanged();
#           if DEBUG_PropertyChanged
            Debug.WriteLine(ToString());
#           endif
        }

        private void BtPropertyChanged() =>
            OnPropertyChanged(
                nameof(IsBTOn),
                nameof(IsBTAutoEnable),
                nameof(IsBtOutEnable));

        private void AudioPropertyChanged() =>
            OnPropertyChanged(
                nameof(IsAudioOn),
                nameof(IsAudioAutoEnable),
                nameof(IsAudioOutEnable));

        private async void AudioDevices_EventCb(object obj, Events.BaseEventArgs<AudioDevice> args) {
            switch (args.ActionId) {
                case DevicesEvents.Enable:
                case DevicesEvents.Add:
                case DevicesEvents.Reload:
                case DevicesEvents.Remove:
                case DevicesEvents.RemoveAll: {
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AudioPropertyChanged());
                        break;
                    }
            }
        }

        private async void BtDevices_EventCb(object obj, Events.BaseEventArgs<BtDevice> args) {
            switch (args.ActionId) {
                case DevicesEvents.Enable:
                case DevicesEvents.Add:
                case DevicesEvents.Reload:
                case DevicesEvents.Remove:
                case DevicesEvents.RemoveAll: {
                        await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => BtPropertyChanged());
                        break;
                    }
            }
        }

        private async void InPropertyChanged(object sender, PropertyChangedEventArgs e) =>
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => AllPropertyChanged(e.PropertyName));

        #endregion

        #region LazyStartScan
        private void LazyStartScan(AudioSelectorType at) {
            if (IsStop || (LazyTimer != default)) return;
            LazyTimer = ThreadPoolTimer.CreateTimer(
                async (a) => {
                    await Dispatcher.RunAsync(
                        CoreDispatcherPriority.High,
                        async () => {
                            if (!IsStop)
                                await Config.AudioCapture.Start(at).ConfigureAwait(false);
                            LazyTimer = default;
                        });
                }, TimeSpan.FromSeconds(5));
        }
        #endregion

        #region OnNavigatedTo / OnNavigatedFrom
        protected override void OnNavigatedTo(NavigationEventArgs e) {
            RequestedTheme = Config.Instance.EleTheme;
            mediaCtrl = SystemMediaTransportControls.GetForCurrentView();

            Config.Instance.PropertyChanged += InPropertyChanged;
            Config.BtDevices.EventCb += BtDevices_EventCb;
            Config.AudioOutDevices.EventCb += AudioDevices_EventCb;
            Config.BtDevices.LogCb += LogCb;
            Config.AudioOutDevices.LogCb += LogCb;
            Config.AudioCapture.LogCb += LogCb;

            OnPropertyChanged(nameof(Volume));
            if (IsOldBtStatus) IsBTOn = true;
            else if (IsOldPlayStatus) IsAudioOn = true;

            Window.Current.CoreWindow.KeyDown += CoreWindow_KeyDown;

            if (mediaCtrl != default) {
                mediaCtrl.IsNextEnabled = true;
                mediaCtrl.IsPlayEnabled = true;
                mediaCtrl.IsStopEnabled = true;
                mediaCtrl.IsPauseEnabled = true;
                mediaCtrl.IsPreviousEnabled = true;
                mediaCtrl.ButtonPressed += TransportCtrl_ButtonPressed;
            }
            if (Config.Instance.IsSound) {
                ElementSoundPlayer.State = ElementSoundPlayerState.On;
                ElementSoundPlayer.SpatialAudioMode = ElementSpatialAudioMode.On;
            } else {
                ElementSoundPlayer.State = ElementSoundPlayerState.Off;
            }
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e) {
            IsOldPlayStatus = IsPlayStatus;
            IsOldBtStatus = IsBtOutEnable;
            Window.Current.CoreWindow.KeyDown -= CoreWindow_KeyDown;
            if (mediaCtrl != default)
                mediaCtrl.ButtonPressed -= TransportCtrl_ButtonPressed;
            Config.Instance.PropertyChanged -= InPropertyChanged;
            Config.BtDevices.EventCb -= BtDevices_EventCb;
            Config.AudioOutDevices.EventCb -= AudioDevices_EventCb;
            Config.BtDevices.LogCb -= LogCb;
            Config.AudioOutDevices.LogCb -= LogCb;
            Config.AudioCapture.LogCb -= LogCb;
            mediaCtrl = default;
        }
        #endregion

        private async void LogCb(object obj, Events.LogEventArgs args) =>
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => LogString = args.Message);

        private void TransportCtrl_ButtonPressed(SystemMediaTransportControls s, SystemMediaTransportControlsButtonPressedEventArgs a) {
            if (a.Button.HasFlag(SystemMediaTransportControlsButton.Play)) {
                if (IsPlay) ClickPlay();
                else if (IsStop) ClickStop();
            }
            else if ((a.Button.HasFlag(SystemMediaTransportControlsButton.Pause) ||
                 a.Button.HasFlag(SystemMediaTransportControlsButton.Stop)) && IsStop)
                ClickStop();
            else if (a.Button.HasFlag(SystemMediaTransportControlsButton.Next) ||
                     a.Button.HasFlag(SystemMediaTransportControlsButton.Previous))
                ClickRecord();
        }

        private void CoreWindow_KeyDown(CoreWindow s, KeyEventArgs a) {
            if (((a.VirtualKey == VirtualKey.Stop) || (a.VirtualKey == VirtualKey.Pause)) && IsStop)
                ClickStop();
            else if ((int)a.VirtualKey == 179) {
                if (IsStop) ClickStop();
                else if (IsPlay) ClickPlay();
            }
            else if (a.VirtualKey == VirtualKey.F2) {
                if (IsBTOn) IsBTOn = false;
                else if (!IsAudioOn) IsBTOn = true;
            }
            else if (a.VirtualKey == VirtualKey.F3) {
                if (IsAudioOn) IsAudioOn = false;
                else if (!IsBTOn) IsAudioOn = true;
            }
            else if ((a.VirtualKey == VirtualKey.F4) && IsCallSettings)
                ClickSetup();
            else if ((a.VirtualKey == VirtualKey.F5) && IsPlay)
                ClickPlay();
            else if ((a.VirtualKey == VirtualKey.F6) && IsRecordEnable)
                ClickRecord();
            else if ((a.VirtualKey == VirtualKey.F8) && IsStop)
                ClickStop();
        }

        private async void Button_ClickMinimize(object sender, RoutedEventArgs e) {
            IList<AppDiagnosticInfo> infos = await AppDiagnosticInfo.RequestInfoForAppAsync();
            IList<AppResourceGroupInfo> resourceInfos = infos[0].GetResourceGroups();
            await resourceInfos[0].StartSuspendAsync();
        }

        private async void Button_ClickExit(object __, RoutedEventArgs _) =>
            await ApplicationView.GetForCurrentView().TryConsolidateAsync();

        private async void Button_ClickPlay(object __, RoutedEventArgs _) =>
            await Config.AudioCapture.Start().ConfigureAwait(false);

        private async void Button_ClickStop(object __, RoutedEventArgs _) {
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => {
                LogString = string.Empty;
                Config.AudioCapture.Stop();
            });
        }

        private void ClickPlay() => Button_ClickPlay(null, null);
        private void ClickStop() => Button_ClickStop(null, null);
        private void ClickSetup() => Button_ClickSetup(null, null);
        private void ClickRecord() => Button_ClickRecord(null, null);

        private async void Button_ClickReset(object sender, RoutedEventArgs __) =>
            _ = await ClickReset().ConfigureAwait(false);

        private async Task<bool> ClickReset() =>
            await Task.Run(async () => {
                await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => IsAudioOn = IsBTOn = false);
                try { Config.Reset(); } catch (Exception ex) { Debug.WriteLine(ex); }
                return true;
            });

        private void Button_ClickSetup(object __, RoutedEventArgs e) =>
            ((App)App.Current).OnNavigatedTo(AppPageType.MainSetup);

        private async void Button_ClickRecord(object __, RoutedEventArgs _) {
            if (IsRecord) Config.AudioCapture.StopRecord();
            else await Config.AudioCapture.StartRecord().ConfigureAwait(false);
            await Dispatcher.RunAsync(CoreDispatcherPriority.Normal, () =>
                    OnPropertyChanged(nameof(IsRecord), nameof(IsRecordEnable)));
        }

        #region ToString
#       if DEBUG
        public override string ToString()
        {
            StringBuilder sb = new();
            sb.AppendLine($"\t - {nameof(IsPlay)} = {IsPlay}");
            sb.AppendLine($"\t - {nameof(IsStop)} = {IsStop}");
            sb.AppendLine($"\t - {nameof(IsPlayStatus)} = {IsPlayStatus}");
            sb.AppendLine($"\t - {nameof(IsInitStatus)} = {IsInitStatus}");
            sb.AppendLine($"\t - {nameof(IsCallSettings)} = {IsCallSettings}");
            sb.AppendLine($"\t - {nameof(IsBtOutEnable)} = {IsBtOutEnable}");
            sb.AppendLine($"\t - {nameof(IsAudioOutEnable)} = {IsAudioOutEnable}");
            sb.AppendLine($"\t - {nameof(IsAudioInDevice)} = {IsAudioInDevice}");
            sb.AppendLine($"\t - {nameof(IsBTOn)} = {IsBTOn}");
            sb.AppendLine($"\t - {nameof(IsBTAutoEnable)} = {IsBTAutoEnable}");
            sb.AppendLine($"\t - {nameof(IsAudioOn)} = {IsAudioOn}");
            sb.AppendLine($"\t - {nameof(IsAudioAutoEnable)} = {IsAudioAutoEnable}");

            sb.AppendLine($"\t + Config.Instance.IsPlay = {Config.Instance.IsPlay}");
            sb.AppendLine($"\t + Config.Instance.IsInit = {Config.Instance.IsInit}");
            sb.AppendLine($"\t + Config.Instance.IsMono = {Config.Instance.IsMono}");
            sb.AppendLine($"\t + Config.Instance.Volume = {Config.Instance.Volume}");

            sb.AppendLine($"\t + Config.Instance.BtSelector = {Config.Instance.BtSelector}");
            sb.AppendLine($"\t + Config.BtDevices.IsEnabled = {Config.BtDevices.IsEnabled}");
            sb.AppendLine($"\t + Config.BtDevices.IsOutDevices = {Config.BtDevices.IsOutDevices}");
            sb.AppendLine($"\t + Config.BtDevices.IsFoundDevices = {Config.BtDevices.IsFoundDevices}");
            sb.AppendLine($"\t + Config.BtDevices.IsConnectedDevices = {Config.BtDevices.IsConnectedDevices}");

            sb.AppendLine($"\t + Config.Instance.AudioSelector = {Config.Instance.AudioSelector}");
            sb.AppendLine($"\t + Config.AudioOutSelectedDevices.IsEnabled = {Config.AudioOutDevices.IsEnabled}");
            sb.AppendLine($"\t + Config.AudioOutSelectedDevices.IsOutDevices = {Config.AudioOutDevices.IsOutDevices}");
            sb.AppendLine($"\t + Config.AudioOutSelectedDevices.IsFoundDevices = {Config.AudioOutDevices.IsFoundDevices}");
            sb.AppendLine($"\t + Config.AudioOutSelectedDevices.IsConnectedDevices = {Config.AudioOutDevices.IsConnectedDevices}");
            return sb.ToString();
        }
#       endif
        #endregion
    }
}
