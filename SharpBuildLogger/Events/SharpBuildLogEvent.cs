using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BoydYang.SharpBuildLogger.Constant;

namespace BoydYang.SharpBuildLogger.Events
{
    public class SharpBuildLogEvent : SharpBuildEventBase
    {
        public string Message { get; set; }
        public SharpBuildLogEvent(string msg)
            : base(SharpBuildEventType.Log)
        {
            Message = msg;
        }
    }
}
