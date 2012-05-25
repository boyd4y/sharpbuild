using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;
using System.ComponentModel;
using System.Windows.Forms.Design;
using System.Drawing.Design;

namespace BoydYang.SharpBuildPkg.Options
{
    [ClassInterface(ClassInterfaceType.AutoDual)]
    public class OptionPageGrid : DialogPage
    {
        [Category("General")]
        [Description("Auto deploy after sharp build")]
        public bool AutoDeploy { get; set; }

        [Category("General")]
        [Description("Disable CA")]
        public bool DisableCA { get; set; }

        [Category("General")]
        [Description(@"MSBuild.exe path, normally in c:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe")]
        [EditorAttribute(typeof(FileNameEditor), typeof(UITypeEditor))]
        public string MSBuildPath { get; set; }

        [Category("General")]
        [Description("Deploy folder 1")]
        [EditorAttribute(typeof(FolderNameEditor), typeof(UITypeEditor))]
        public string DeployTarget1 { get; set; }

        [Category("General")]
        [Description("Deploy folder 2")]
        [EditorAttribute(typeof(FolderNameEditor), typeof(UITypeEditor))]
        public string DeployTarget2 { get; set; }

        [Category("General")]
        [Description("Deploy folder 3")]
        [EditorAttribute(typeof(FolderNameEditor), typeof(UITypeEditor))]
        public string DeployTarget3 { get; set; }

        public OptionPageGrid()
        {
            MSBuildPath = @"c:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe";
        }
    }
}
