/*
 * Git: https://github.com/ClaudiaCoord/OneTouchAudioMonitor
 * Copyright (c) 2022 СС
 * License MIT.
*/
using System;
using System.ComponentModel;
using System.Linq;
using System.Xml.Serialization;
using Windows.Devices.Enumeration;

namespace OneTouchMonitor.Data
{
    [Serializable()]
    [DesignerCategory("code")]
    [XmlType(AnonymousType = true)]
    [XmlRoot(Namespace = "", IsNullable = false)]
    public class AudioDevice : IDevice {
        public string Name { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public bool IsEnable { get; set; } = false;
        public bool IsPlay { get; set; } = false;
        [XmlIgnore]
        public bool IsRemove => !IsEnable;
        [XmlIgnore]
        public bool IsEmpty => (Device == default) || !Device.IsEnabled || string.IsNullOrWhiteSpace(Name);
        [XmlIgnore]
        public DeviceInformation Device { get; set; } = default;

        public AudioDevice() { }
        public AudioDevice(string id, string name, bool b, DeviceInformation di)
        {
            Id = id ?? string.Empty;
            Name = name ?? string.Empty;
            IsEnable = b;
            Device = di;
        }
        public bool CompareAudioName(string s) =>
            Name.Equals(s, StringComparison.InvariantCultureIgnoreCase);

        public bool CompareBTName(string s) =>
            !string.IsNullOrWhiteSpace(Name) &&
             Name.Contains($"({s}", StringComparison.InvariantCultureIgnoreCase) &&
            !Name.Contains("free", StringComparison.InvariantCultureIgnoreCase);
    }
}
