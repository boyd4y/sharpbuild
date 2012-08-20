using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using System.IO.Pipes;
using System.IO;
using System.Diagnostics;
using BoydYang.SharpBuildLogger.Events;

namespace BoydYang.SharpBuildLogger.Loggers
{
    public class MSBuildLogger : Logger
    {
        private NamedPipeClientStream client = null;
        private StreamWriter sw = null;
        private string projectFileName = string.Empty;

        public override void Initialize(Microsoft.Build.Framework.IEventSource eventSource)
        {
            //Register for the ProjectStarted, TargetStarted, and ProjectFinished events
            eventSource.ProjectStarted += new ProjectStartedEventHandler(eventSource_ProjectStarted);
            eventSource.TargetStarted += new TargetStartedEventHandler(eventSource_TargetStarted);
            eventSource.MessageRaised += new BuildMessageEventHandler(eventSource_MessageRaised);
            eventSource.ErrorRaised += new BuildErrorEventHandler(eventSource_ErrorRaised);
            eventSource.WarningRaised += new BuildWarningEventHandler(eventSource_WarningRaised);
            eventSource.ProjectFinished += new ProjectFinishedEventHandler(eventSource_ProjectFinished);
            eventSource.BuildStarted += new BuildStartedEventHandler(eventSource_BuildStarted);
            eventSource.BuildFinished += new BuildFinishedEventHandler(eventSource_BuildFinished);

            // Pipe setup....
            SetupPipe();
        }

        void eventSource_BuildFinished(object sender, BuildFinishedEventArgs e)
        {
            TraceAsJson(new SharpBuildFinishedEvent(e.Succeeded));
        }

        void eventSource_BuildStarted(object sender, BuildStartedEventArgs e)
        {
            TraceAsJson(new SharpBuildStartEvent());
        }

        private bool SetupPipe()
        {
            if (string.IsNullOrEmpty(Parameters))
                return false;

            try
            {
                client = new NamedPipeClientStream(".", Parameters, PipeDirection.Out);
                client.Connect();
                sw = new StreamWriter(client);
            }
            catch (Exception ee)
            {
                Debug.WriteLine(ee.Message);
                return false;
            }

            return true;
        }

        private void WriteAndFlush(string msg)
        {
            if (sw != null)
            {
                sw.WriteLine(msg);
                sw.Flush();
            }
            else
                Console.WriteLine(msg);
        }

        private void TraceAsJson(SharpBuildEventBase eventBase)
        {
            try
            {
                string buffer = Newtonsoft.Json.JsonConvert.SerializeObject(new SharpBuildEventWrapper(eventBase));
                WriteAndFlush(buffer);
            }
            catch (Exception e)
            {
                try
                {
                    SharpBuildEventWrapper logevt = new SharpBuildEventWrapper(new SharpBuildInternalErrorEvent(e.Message));
                    string buffer = Newtonsoft.Json.JsonConvert.SerializeObject(logevt);
                    WriteAndFlush(buffer);
                }
                catch (Exception ee)
                {
                    WriteAndFlush(ee.Message);
                }
            }
        }

        private void TraceLog(string msg)
        {
            TraceAsJson(new SharpBuildLogEvent(msg));
        }

        void eventSource_WarningRaised(object sender, BuildWarningEventArgs e)
        {
            TraceAsJson(new SharpBuildWarningEvent(e));

        }

        void eventSource_ErrorRaised(object sender, BuildErrorEventArgs e)
        {
            TraceAsJson(new SharpBuildErrorEvent(e));
        }

        void eventSource_MessageRaised(object sender, BuildMessageEventArgs e)
        {
        }

        void eventSource_ProjectStarted(object sender, ProjectStartedEventArgs e)
        {
            TraceLog(string.Format(@"*-------- Project Started: {0} --------*", projectFileName));
        }

        void eventSource_ProjectFinished(object sender, ProjectFinishedEventArgs e)
        {
            TraceLog(string.Format(@"*======== Project Finished: {0} {1} ========*", projectFileName, e.Succeeded));
        }

        void eventSource_TargetStarted(object sender, TargetStartedEventArgs e)
        {
        }
    }
}
