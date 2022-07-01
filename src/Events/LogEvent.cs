/*
 * Git: https://github.com/ClaudiaCoord/OneTouchAudioMonitor
 * Copyright (c) 2022 СС
 * License MIT.
*/
using System;

namespace OneTouchMonitor.Events
{
    public class LogEventArgs : EventArgs {
        public Type Sender { get; private set; } = Type.Missing.GetType();
        public string Message { get; private set; } = string.Empty;
        public LogEventArgs(Type t, string s) {
            Sender = t;
            Message = s;
        }
        public LogEventArgs(Type t, Exception ex) {
            Sender = t;
            Message = ex.Message;
        }
    }

    public class LogEvent {
        public delegate void DelegateLogEvent(object obj, LogEventArgs args);
        public event DelegateLogEvent LogCb = delegate { };
        protected void ToLog(object obj, LogEventArgs args) =>
            LogCb?.Invoke(obj, args);
        protected void ToLog(object obj, string s) =>
            LogCb?.Invoke(obj, new LogEventArgs(obj.GetType(), s));
        protected void ToLog(object obj, Exception ex) =>
            LogCb?.Invoke(obj, new LogEventArgs(obj.GetType(), ex));
    }
}
