/*
 * Git: https://github.com/ClaudiaCoord/OneTouchAudioMonitor
 * Copyright (c) 2022 СС
 * License MIT.
*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using OneTouchMonitor.Data;
using Windows.Devices.Bluetooth;
using Windows.Devices.Enumeration;
using Windows.Devices.Radios;

namespace OneTouchMonitor.Events
{
    public class DeviceEventBT : BaseDeviceEvent<BtDevice>, IAutoInit
    {
        protected override string[] GetPref() => new string[] {
            "System.Devices.Aep.DeviceAddress",
            "System.Devices.Aep.IsConnected",
            "System.Devices.Aep.IsPresent",
            "System.Devices.Aep.IsPaired",
            "System.Devices.Aep.CanPair",
            "System.Devices.Aep.ContainerId",
            "System.Devices.Aep.Manufacturer",
            "System.Devices.Aep.ModelId",
            "System.Devices.Aep.ModelName",
            "System.Devices.Aep.ProtocolId",
            "System.Devices.Aep.SignalStrength",
            "System.Devices.Aep.Bluetooth.Le.IsConnectable"
        };
        protected override string GetFilter() =>
            @"System.Devices.Aep.ProtocolId:=""{e0cbf06c-cd8b-4647-bb8a-263b43f0f974}"" AND System.Devices.Aep.IsPaired:=System.StructuredQueryType.Boolean#True AND System.Devices.Aep.IsPresent:=System.StructuredQueryType.Boolean#True AND System.Devices.Aep.IsConnected:=System.StructuredQueryType.Boolean#True";

        protected override async Task<DeviceInformationCollection> GetDevicesCollections() =>
            await DeviceInformation.FindAllAsync(BluetoothDevice.GetDeviceSelectorFromPairingState(true), GetPref());

        protected override bool IsDeviceEnable(DeviceInformation di) =>
            di.Pairing.CanPair || di.Pairing.IsPaired;

        protected override BtDevice CreateDevice(DeviceInformation di, bool isenable) =>
            new BtDevice(di.Id, di.Name, isenable, di);

        protected override DeviceWatcher GetWatcher() =>
            DeviceInformation.CreateWatcher(GetFilter(), GetPref(), deviceKind);

        public DeviceEventBT() : base(DeviceInformationKind.AssociationEndpoint) { }
        ~DeviceEventBT() => Dispose();

        public async Task AutoInit() =>
            await Task.Run(async () => {
                IsEnabled = await GetBluetoothState().ConfigureAwait(false);
                if (IsEnabled) {
                    Config.Instance.BtSelector = AudioSelectorType.AutoPlay | AudioSelectorType.BtDevice;
                    base.Clear();
                    base.WatcherInitDev(true);
                }
            });

        public async Task<bool> ReloadDevicesList() =>
            await AllDevicesList().ConfigureAwait(false);

        public async void Start() =>
            _ = await ToogleBluetoothStateAsync(EventState.Start).ConfigureAwait(false);

        public async void Stop() =>
            _ = await ToogleBluetoothStateAsync(EventState.Stop).ConfigureAwait(false);

        #region Toogle Bluetooth State
        private async Task<bool> ToogleBluetoothStateAsync(EventState st) {
            try {
                IsEnabled = false;
                RadioState state = RadioState.Unknown;
                RadioAccessStatus access = await Radio.RequestAccessAsync();
                if (access != RadioAccessStatus.Allowed) {
                    ToLog(this, $"{Config.GetString("S7")}: '{access}'");
                    return false;
                }
                IReadOnlyList<Radio> radios = await Radio.GetRadiosAsync();
                Radio BluetoothRadio = radios.FirstOrDefault(radio => radio.Kind == RadioKind.Bluetooth);
                if (BluetoothRadio == default)
                    return false;

                access = RadioAccessStatus.Unspecified;
                foreach (Radio module in radios) {
                    if (module.Kind == RadioKind.Bluetooth) {
                        if (module.State == RadioState.Off) {
                            if ((st == EventState.Start) || (st == EventState.Auto)) {
                                base.Clear();
                                module.StateChanged += Module_StateChanged;
                                access = await module.SetStateAsync(RadioState.On);
                            } else
                                access = RadioAccessStatus.Allowed;
                        }
                        else if (module.State == RadioState.On) {
                            if ((st == EventState.Stop) || (st == EventState.Auto)) {
                                access = await module.SetStateAsync(RadioState.Off);
                                module.StateChanged -= Module_StateChanged;
                            } else
                                access = RadioAccessStatus.Allowed;
                        }
                        state = module.State;
                        break;
                    }
                }
                IsEnabled = state == RadioState.On;
                bool ret = st switch {
                    EventState.Start => (access == RadioAccessStatus.Allowed) && (state == RadioState.On),
                    EventState.Stop => (access == RadioAccessStatus.Allowed) && (state == RadioState.Off),
                    EventState.Auto => (access == RadioAccessStatus.Allowed) && (state != RadioState.Unknown),
                    _ => false
                };
                if (ret && (st == EventState.Start)) {
                    base.WatcherInitDev(true);
                }
                else if (ret && (st == EventState.Stop)) {
                    base.WatcherInitDev(false);
                }
                return ret;
            } catch (Exception ex) { ToLog(this, ex); IsEnabled = false; }
            return false;
        }
        #endregion

        #region Check Bluetooth State
        private async Task<bool> GetBluetoothState() {
            try {
                RadioAccessStatus access = await Radio.RequestAccessAsync();
                if (access != RadioAccessStatus.Allowed) {
                    ToLog(this, $"{Config.GetString("S7")}: '{access}'");
                    return false;
                }
                IReadOnlyList<Radio> radios = await Radio.GetRadiosAsync();
                Radio BluetoothRadio = radios.FirstOrDefault(radio => radio.Kind == RadioKind.Bluetooth);
                if (BluetoothRadio == null)
                    return false;

                access = RadioAccessStatus.Unspecified;
                foreach (Radio module in radios) {
                    if (module.Kind == RadioKind.Bluetooth)
                        return module.State == RadioState.On;
                }
            } catch (Exception ex) { ToLog(this, ex); }
            return false;
        }
        #endregion

        #region Events callback
        private void Module_StateChanged(Radio sender, object args)
        {
            if (sender != null)
                base.IsEnabled = sender.State == RadioState.On;
        }
        #endregion
    }
}
