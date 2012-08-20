using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using BoydYang.SharpBuildLogger.Constant;
using Microsoft.Build.Framework;

namespace BoydYang.SharpBuildLogger.Events
{
    public class SharpBuildWarningEvent : SharpBuildEventBase
    {
        public string Message { get; set; }
        public string Code { get; set; }
        public int ColumnNumber { get; set; }
        public int EndColumnNumber { get; set; }
        public int EndLineNumber { get; set; }
        public string File { get; set; }
        public int LineNumber { get; set; }
        public string ProjectFile { get; set; }

        public SharpBuildWarningEvent()
            : this(null)
        {
        }

        public SharpBuildWarningEvent(BuildWarningEventArgs arg)
            : base(SharpBuildEventType.WarningLog)
        {
            if (arg != null)
            {
                Code = arg.Code;
                ColumnNumber = arg.ColumnNumber;
                EndColumnNumber = arg.EndColumnNumber;
                EndLineNumber = arg.EndLineNumber;
                File = arg.File;
                LineNumber = arg.LineNumber;
                ProjectFile = arg.ProjectFile;
                Message = arg.Message;
            }
        }
    }
}
