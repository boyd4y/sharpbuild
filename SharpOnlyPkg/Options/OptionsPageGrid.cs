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
        [Description("Disable Optimize")]
        public bool DisableOptimize { get; set; }

        [Category("General")]
        [Description(@"MSBuild.exe path, normally in c:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe")]
        [EditorAttribute(typeof(FileNameEditor), typeof(UITypeEditor))]
        public string MSBuildPath { get; set; }

        [Category("General")]
        [Description("Deploy folders")]
        [TypeConverter(typeof(DeployFolderCollectionConverter))]
        [Editor(typeof(DeployFolderCollectionEditor), typeof(System.Drawing.Design.UITypeEditor))]
        public DeployFolderCollection DeployFolders { get; set; }

        public OptionPageGrid()
        {
            MSBuildPath = @"c:\Windows\Microsoft.NET\Framework\v4.0.30319\MSBuild.exe";
            DeployFolders = new DeployFolderCollection();
            AutoDeploy = true;
            DisableCA = true;
            DisableOptimize = true;
        }
    }
}
