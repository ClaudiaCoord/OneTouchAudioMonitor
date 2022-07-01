/*
 * Git: https://github.com/ClaudiaCoord/OneTouchAudioMonitor
 * Copyright (c) 2022 СС
 * License MIT.
*/
using Windows.Devices.Enumeration;

namespace OneTouchMonitor.Data
{
    public interface IDevice {
        string Name { get; set; }
        string Id { get; set; }
        bool IsPlay { get; set; }
        bool IsEmpty { get; }
        bool IsRemove { get; }
        bool IsEnable { get; set; }
        DeviceInformation Device { get; set; }
    }
}
