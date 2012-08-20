using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BoydYang.SharpBuildLogger.Constant;

namespace BoydYang.SharpBuildLogger.Events
{
    public class SharpBuildEventBase
    {
        public SharpBuildEventType EventType { get; set; }
        public DateTime GeneratedTime { get; set; }

        public SharpBuildEventBase(SharpBuildEventType type)
        {
            EventType = type;
            GeneratedTime = DateTime.Now;
        }
    }
}
