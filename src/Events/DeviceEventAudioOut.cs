/*
 * Git: https://github.com/ClaudiaCoord/OneTouchAudioMonitor
 * Copyright (c) 2022 СС
 * License MIT.
*/

using System;
using System.Threading.Tasks;
using OneTouchMonitor.Data;
using OneTouchMonitor.Event;
using Windows.Devices.Enumeration;
using Windows.Media.Devices;

namespace OneTouchMonitor.Events
{
    public class DeviceEventAudioOut : BaseDeviceEvent<AudioDevice>, IAutoInit
    {
        protected override async Task<DeviceInformationCollection> GetDevicesCollections() =>
            await DeviceInformation.FindAllAsync(MediaDevice.GetAudioRenderSelector());

        protected override bool IsDeviceEnable(DeviceInformation di) => di.IsEnabled;

        protected override AudioDevice CreateDevice(DeviceInformation di, bool isenable) =>
            new AudioDevice(di.Id, di.Name, isenable, di);

        protected override DeviceWatcher GetWatcher() =>
            DeviceInformation.CreateWatcher(MediaDevice.GetAudioRenderSelector());

        public DeviceEventAudioOut() : base(DeviceInformationKind.Unknown) { }
        ~DeviceEventAudioOut() => Dispose();

        public new void Dispose() {
            AudioState(EventState.Stop);
            base.Dispose();
        }

        public async Task<bool> ReloadDevicesList() =>
            await AllDevicesList().ConfigureAwait(false);

        public async Task AutoInit() =>
            await Task.Run(() => {
                Config.BtDevices.EventCb += BtDevices_EventCb;
                Start();
            });
        public void Start() => AudioState(EventState.Start);
        public void Stop() => AudioState(EventState.Stop);

        #region Audio State
        private void AudioState(EventState st) {
            try {
                switch (st) {
                    case EventState.Start: base.WatcherInitDev(true); break;
                    case EventState.Stop: base.WatcherInitDev(false); break;
                }
            } catch (Exception ex) { ToLog(this, ex); IsEnabled = false; }
        }
        #endregion

        #region event
        private async void BtDevices_EventCb(object obj, Events.BaseEventArgs<BtDevice> args) {
            if (args.ActionId == DevicesEvents.Reload)
                _ = await ReloadDevicesList().ConfigureAwait(false);
        }
        #endregion

    }
}
