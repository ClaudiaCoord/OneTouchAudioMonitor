/*
 * Git: https://github.com/ClaudiaCoord/OneTouchAudioMonitor
 * Copyright (c) 2022 СС
 * License MIT.
*/
using System;
using System.ComponentModel;
using System.Xml.Serialization;
using Windows.Devices.Enumeration;

namespace OneTouchMonitor.Data
{
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    [XmlRoot(Namespace = "", IsNullable = false)]
    public class BtDevice : IDevice
    {
        public string Name { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public bool IsEnable { get; set; } = false;
        public bool IsPlay { get; set; } = false;
        [XmlIgnore]
        public bool IsRemove => !IsEnable;
        [XmlIgnore]
        public bool IsEmpty => string.IsNullOrWhiteSpace(Id) || string.IsNullOrWhiteSpace(Name);
        [XmlIgnore]
        public DeviceInformation Device { get; set; } = default;

        public BtDevice() { }
        public BtDevice(string id, string name, bool b, DeviceInformation di)
        {
            Id = id ?? string.Empty;
            Name = name ?? string.Empty;
            IsEnable = b;
            Device = di;
        }
    }
}
