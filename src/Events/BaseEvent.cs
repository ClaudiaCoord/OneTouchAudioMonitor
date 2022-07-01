/*
 * Git: https://github.com/ClaudiaCoord/OneTouchAudioMonitor
 * Copyright (c) 2022 СС
 * License MIT.
*/
using System;
using OneTouchMonitor.Data;
using OneTouchMonitor.Event;

namespace OneTouchMonitor.Events
{
    public class BaseEventArgs<T1> : EventArgs where T1 : IDevice, new() {
        public T1 Obj;
        public DevicesEvents ActionId { get; private set; } = DevicesEvents.None;
        public BaseEventArgs(DevicesEvents id, T1 a) {
            Obj = a;
            ActionId = id;
        }
    }

    public class BaseEvent<T1> : LogEvent where T1 : IDevice, new() {
        public delegate void DelegateBaseEvent(object obj, BaseEventArgs<T1> args);
        public event DelegateBaseEvent EventCb = delegate { };
        protected void CallEvent(object obj, BaseEventArgs<T1> args) =>
            EventCb?.Invoke(obj, args);
        protected void CallEvent(object obj, DevicesEvents id, T1 a) =>
            EventCb?.Invoke(obj, new BaseEventArgs<T1>(id, a));
        protected void CallEvent(object obj, DevicesEvents id) =>
            EventCb?.Invoke(obj, new BaseEventArgs<T1>(id, default));
    }
}
