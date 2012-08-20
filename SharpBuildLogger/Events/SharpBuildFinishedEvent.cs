using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BoydYang.SharpBuildLogger.Constant;

namespace BoydYang.SharpBuildLogger.Events
{
    public class SharpBuildFinishedEvent : SharpBuildEventBase
    {
        public bool Successed {get;set;}

        public SharpBuildFinishedEvent(bool success)
            : base(SharpBuildEventType.Finished)
        {
            Successed = success;
        }
    }
}
