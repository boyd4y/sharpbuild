using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Build.Utilities;
using Microsoft.Build.Framework;
using System.IO.Pipes;
using System.IO;
using System.Diagnostics;

namespace BoydYang.SharpBuildPkg.Loggers
{
    public class MSBuildLogger : Logger
    {
        private static string NAME = "sharpbuild";
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
            TraceLog(string.Format(@"*-------- Build Finished: {0} {1}--------*",e.Succeeded ? "Success" : "Falied",  e.Timestamp.ToLongTimeString()));
        }

        void eventSource_BuildStarted(object sender, BuildStartedEventArgs e)
        {
            TraceLog(string.Format(@"*-------- Build Started: {0} --------*", e.Timestamp.ToLongTimeString()));
        }

        private bool SetupPipe()
        {
            try
            {
                client = new NamedPipeClientStream(".", NAME, PipeDirection.Out);
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

        private void TraceLog(string msg)
        {
            if (sw != null)
            {
                sw.WriteLine(msg);
                sw.Flush();
            }
        }

        void eventSource_WarningRaised(object sender, BuildWarningEventArgs e)
        {
            string msg = string.Format(@"{0}({1},{2}): warning {3}: {4}", e.File, e.LineNumber, e.ColumnNumber, e.Code, e.Message);
            TraceLog(msg);
        }

        void eventSource_ErrorRaised(object sender, BuildErrorEventArgs e)
        {
            string msg = string.Format(@"{0}({1},{2}): error {3}: {4}", e.File, e.LineNumber, e.ColumnNumber, e.Code, e.Message);
            TraceLog(msg);
        }

        void eventSource_MessageRaised(object sender, BuildMessageEventArgs e)
        {
        }

        void eventSource_ProjectStarted(object sender, ProjectStartedEventArgs e)
        {
            //projectFileName = System.IO.Path.GetFileName(e.ProjectFile);
            //TraceLog(string.Format(@"*-------- Project Started: {0} --------*", projectFileName));
        }

        void eventSource_ProjectFinished(object sender, ProjectFinishedEventArgs e)
        {
            //TraceLog(string.Format(@"*======== Project Finished: {0} {1} ========*", projectFileName, e.Succeeded));
        }

        void eventSource_TargetStarted(object sender, TargetStartedEventArgs e)
        {
        }
    }
}
