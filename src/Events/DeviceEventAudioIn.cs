/*
 * Git: https://github.com/ClaudiaCoord/OneTouchAudioMonitor
 * Copyright (c) 2022 СС
 * License MIT.
*/
using System;
using System.Threading.Tasks;
using OneTouchMonitor.Data;
using Windows.Devices.Enumeration;
using Windows.Media.Devices;

namespace OneTouchMonitor.Events
{
    public class DeviceEventAudioIn : BaseDeviceEvent<AudioDevice>, IAutoInit
    {
        protected override async Task<DeviceInformationCollection> GetDevicesCollections() =>
            await DeviceInformation.FindAllAsync(MediaDevice.GetAudioCaptureSelector());

        protected override bool IsDeviceEnable(DeviceInformation di) => di.IsEnabled;

        protected override AudioDevice CreateDevice(DeviceInformation di, bool isenable) =>
            new AudioDevice(di.Id, di.Name, isenable, di);

        protected override DeviceWatcher GetWatcher() =>
            DeviceInformation.CreateWatcher(MediaDevice.GetAudioCaptureSelector());

        public DeviceEventAudioIn() : base(DeviceInformationKind.Unknown) { }
        ~DeviceEventAudioIn() => Dispose();

        public new void Dispose()
        {
            AudioState(EventState.Stop);
            base.Dispose();
        }

        public async Task<bool> ReloadDevicesList() =>
            await AllDevicesList().ConfigureAwait(false);

        public async Task AutoInit() =>
            await Task.Run(() => {
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
    }
}
