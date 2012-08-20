using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BoydYang.SharpBuildLogger.Constant;

namespace BoydYang.SharpBuildLogger.Events
{
    public class SharpBuildInternalErrorEvent : SharpBuildEventBase
    {
        public string Message { get; set; }

        public SharpBuildInternalErrorEvent(string msg)
            : base(SharpBuildEventType.InternalError)
        {
            Message = msg;
        }
    }
}
