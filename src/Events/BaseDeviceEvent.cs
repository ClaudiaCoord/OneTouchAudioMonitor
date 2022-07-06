/*
 * Git: https://github.com/ClaudiaCoord/OneTouchAudioMonitor
 * Copyright (c) 2022 СС
 * License MIT.
*/
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using OneTouchMonitor.Data;
using OneTouchMonitor.Event;
using OneTouchMonitor.Utils;
using Windows.Devices.Enumeration;

namespace OneTouchMonitor.Events
{
    [Flags]
    internal enum EventState : int {
        Auto = 0,
        Start,
        Stop
    }

    public abstract class BaseDeviceEvent<T1> : BaseEvent<T1>, IDisposable where T1 : IDevice, new()
    {
        private bool isEnabled = false;

        protected object __lock = new object();
        protected DeviceWatcher dwatch { get; set; }
        protected DeviceInformationKind deviceKind { get; private set; } = DeviceInformationKind.Unknown;
        protected RunOnce runOnceAllList { get; set; } = new();
        protected RunOnce runOnceInitWatch { get; set; } = new();

        public bool IsFoundDevices => BaseAllDevices.Count > 0;
        public bool IsOutDevices => BaseSelectedDevices.Count > 0;
        public bool IsConnectedDevices => BaseAwailDevices.Count > 0;
        public bool IsEnabled {
            get => isEnabled;
            set { isEnabled = value; CallEvent(this, DevicesEvents.Enable); }
        }

        public List<T1> BaseAllDevices { get; } = new();
        public List<T1> BaseAwailDevices { get; } = new();
        public List<T1> BaseSelectedDevices { get; } = new();

        public BaseDeviceEvent(DeviceInformationKind kind) =>
            deviceKind = kind;
        ~BaseDeviceEvent() => Dispose();

        public void Dispose() => WatcherInitDev(false);

        #region Watcher devices init/deinit
        protected virtual string[] GetPref() => new string[0];
        protected virtual string GetFilter() => string.Empty;
        protected virtual DeviceWatcher GetWatcher() => default;

        protected void WatcherInitDev(bool isstart) {
            try {
                if (dwatch != null) {
                    if (!isstart) {
                        if (runOnceInitWatch.IsRun) {

                            CancellationTokenSource cts = new(TimeSpan.FromSeconds(10));
                            while (runOnceInitWatch.IsRun) {
                                Task.Delay(50);
                                if (cts.IsCancellationRequested) break;
                            }
                            cts.Dispose();
                        }
                        IsEnabled = false;

                        DeviceWatcher dw = dwatch;
                        dwatch = default;
                        if (dw != null) try { dw.Stop(); } catch { }
                    }
                    return;
                }
                if (!isstart) return;
                if (isstart && (dwatch != null)) return;
            } catch { }

            if (!runOnceInitWatch.Begin()) return;

            try {
                Clear();

                dwatch = GetWatcher();
                if (dwatch == null) return;

                dwatch.Added += Dwatch_Added;
                dwatch.Updated += Dwatch_Updated;
                dwatch.Removed += Dwatch_Removed;
                dwatch.Stopped += Dwatch_Stopped;
                dwatch.EnumerationCompleted += Dwatch_EnumerationCompleted;
                dwatch.Start();
                IsEnabled = true;
            }
            catch (Exception ex) {
                ToLog(this, ex);
                runOnceInitWatch.End();
                WatcherInitDev(false);
            }
            finally { runOnceInitWatch.End(); }
        }
        #endregion

        #region Manage Devices List
        private void ManageDevicesList(bool isadd, string id, string name, DeviceInformation di = default) {

            try {
                if (isadd && (name != default)) {
                    T1 a = (from i in BaseAwailDevices where i.Id.Equals(id) select i).FirstOrDefault();
                    do {
                        if (a != null) {
                            if (a.IsEnable && (!string.IsNullOrWhiteSpace(a.Name) || a.Name.Equals(name)))
                                break;
                            a.Name = string.IsNullOrWhiteSpace(name) ? a.Name : name;
                            a.IsEnable = true;

                        } else {
                            a = new T1() {
                                Id = id,
                                Name = name,
                                IsEnable = true,
                                Device = di
                            };
                        }
                        if (a != null)
                            lock (__lock)
                                BaseAwailDevices.Add(a);

                    } while (false);
                    CallEvent(this, DevicesEvents.Add, a);
                }
                else if (!isadd && (BaseAwailDevices.Count > 0)) {
                    T1 a = (from i in BaseAwailDevices where i.Id.Equals(id) select i).FirstOrDefault();
                    if (a != null) {
                        lock (__lock)
                            BaseAwailDevices.Remove(a);
                        CallEvent(this, DevicesEvents.Remove, a);
                    }
                }
#               if DEBUG
                foreach (IDevice item in BaseAllDevices)
                    Debug.WriteLine($"\tall\t+/- {item.Name} - {item.Id} : {item.IsEnable}");
                foreach (IDevice item in BaseAwailDevices)
                    Debug.WriteLine($"\tava\t+/- {item.Name} - {item.Id} : {item.IsEnable}");
#               endif
            }
            catch (Exception ex) { ToLog(this, ex); }
        }
        #endregion

        #region Get all Devices List
        protected virtual Task<DeviceInformationCollection> GetDevicesCollections() => default(Task<DeviceInformationCollection>);
        protected virtual bool IsDeviceEnable(DeviceInformation di) => false;
        protected virtual T1 CreateDevice(DeviceInformation di, bool isenable) => default(T1);

        protected async Task<bool> AllDevicesList(bool isclear = true) {

            if (!runOnceAllList.Begin()) return false;

            try {
                DeviceInformationCollection devices = await GetDevicesCollections().ConfigureAwait(false);

                if ((devices == default) || (devices.Count == 0))
                    return false;

                List<T1> alldev = isclear ? new() : new(BaseAwailDevices);
                alldev.AddRange(devices.Where(x => IsDeviceEnable(x)).Select(x => CreateDevice(x, false)));
                IDeviceEqualityComparer<T1> cmp = new();

                foreach (T1 d in BaseAwailDevices) {
                    T1 dev = (from i in alldev where i.Id == d.Id select i).FirstOrDefault();
                    if ((dev != null) && !dev.IsEmpty)
                        d.IsEnable = true;
                }

                lock (__lock) {
                    BaseAllDevices.Clear();
                    BaseAllDevices.AddRange(alldev.Distinct<T1>(cmp));
                }
                CallEvent(this, DevicesEvents.Reload);

#               if DEBUG
                foreach (DeviceInformation item in devices)
                    Debug.WriteLine($"\tlist\t *  {item.Name} - {item.Id} - {item.Kind} - {item.IsDefault}/{item.IsEnabled}/{item.Pairing.CanPair}/{item.Pairing.IsPaired}");
#               endif
            }
            catch (Exception ex) { ToLog(this, ex); }
            finally { runOnceAllList.End(); }
            return BaseAllDevices.Count > 0;
        }
        #endregion

        #region Events callback
        private void Dwatch_Added(DeviceWatcher _, DeviceInformation args) {
            if (args != null) ManageDevicesList(true, args.Id, args.Name, args);
        }
        private void Dwatch_Updated(DeviceWatcher _, DeviceInformationUpdate args) {
            if (args != null) ManageDevicesList(true, args.Id, string.Empty);
        }
        private void Dwatch_Removed(DeviceWatcher _, DeviceInformationUpdate args) {
            if (args != null) ManageDevicesList(false, args.Id, string.Empty);
        }
        private async void Dwatch_EnumerationCompleted(DeviceWatcher __, object ___) {
            _ = await AllDevicesList().ConfigureAwait(false);
        }
        private void Dwatch_Stopped(DeviceWatcher _, object __) {
            IsEnabled = false;
            Clear();
        }
        #endregion

        #region Clear bases
        protected void Clear() {
            lock (__lock) {
                BaseAllDevices.Clear();
                BaseAwailDevices.Clear();
            }
            CallEvent(this, DevicesEvents.RemoveAll);
        }
        #endregion
    }
}
