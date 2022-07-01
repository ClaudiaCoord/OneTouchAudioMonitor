/*
 * Git: https://github.com/ClaudiaCoord/OneTouchAudioMonitor
 * Copyright (c) 2022 СС
 * License MIT.
*/
using System.Collections.Generic;

namespace OneTouchMonitor.Data
{
    public class IDeviceEqualityComparer<T1> : IEqualityComparer<T1> where T1 : IDevice {

        public bool Equals(T1 id1, T1 id2) {
            if (id2 == null && id1 == null)
                return true;
            else if (id1 == null || id2 == null)
                return false;
            else if (!string.IsNullOrWhiteSpace(id1.Id) && id1.Id.Equals(id2.Id))
                return true;
            else
                return false;
        }
        public int GetHashCode(T1 id) =>
            id.Id.GetHashCode();
    }
}
