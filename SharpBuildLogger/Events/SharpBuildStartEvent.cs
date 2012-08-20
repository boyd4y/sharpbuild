using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BoydYang.SharpBuildLogger.Constant;

namespace BoydYang.SharpBuildLogger.Events
{
    public class SharpBuildStartEvent : SharpBuildEventBase
    {
        public SharpBuildStartEvent()
            : base(SharpBuildEventType.Start)
        {
        }
    }
}
