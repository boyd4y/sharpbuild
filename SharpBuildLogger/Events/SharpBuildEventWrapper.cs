using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BoydYang.SharpBuildLogger.Constant;
using Newtonsoft.Json;

namespace BoydYang.SharpBuildLogger.Events
{
    public class SharpBuildEventWrapper
    {
        public SharpBuildEventType EventType { get; set; }
        public string EmbeddedMessage { get; set; }

        public SharpBuildEventWrapper(string embeddedMsg, SharpBuildEventType type)
        {
            EmbeddedMessage = embeddedMsg;
            EventType = type;
        }

        public SharpBuildEventWrapper()
            : this(string.Empty, SharpBuildEventType.InternalError)
        {
        }

        public SharpBuildEventWrapper(SharpBuildEventBase baseEvent)
            : this(Newtonsoft.Json.JsonConvert.SerializeObject(baseEvent), baseEvent.EventType)
        {
        }
    }
}
