using System;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.InteropServices;
using System.ComponentModel.Design;
using Microsoft.Win32;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using BoydYang.SharpBuildPkg.Options;
using System.Windows.Forms;
using EnvDTE;
using System.ComponentModel;
using System.IO.Pipes;
using System.IO;
using BoydYang.SharpBuildPkg.BuildRunner;
using BoydYang.SharpBuildPkg.ServiceProviders;
using BoydYang.SharpBuildPkg.Util;
using System.Collections.Generic;

namespace BoydYang.SharpBuildPkg
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    ///
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the 
    /// IVsPackage interface and uses the registration attributes defined in the framework to 
    /// register itself and its components with the shell.
    /// </summary>
    // This attribute tells the PkgDef creation utility (CreatePkgDef.exe) that this class is
    // a package.
    [PackageRegistration(UseManagedResourcesOnly = true)]
    // This attribute is used to register the informations needed to show the this package
    // in the Help/About dialog of Visual Studio.
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)]
    // This attribute is needed to let the shell know that this package exposes some menus.
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(GuidList.guidSharpBuildPkgPkgString)]
    [ProvideOptionPage(typeof(OptionPageGrid), "Sharp Build", "General", 0, 0, true)]
    [ProvideService(typeof(MSBuildTaskService))]
    [ProvideService(typeof(SharpBuildService))]
    [ProvideService(typeof(SharpBuildDeployService))]
    public sealed class SharpOnlyPkgPackage : Package, IOleCommandTarget, Microsoft.VisualStudio.OLE.Interop.IServiceProvider
    {
        /// <summary>
        /// Default constructor of the package.
        /// Inside this method you can place any initialization code that does not require 
        /// any Visual Studio service because at this point the package object is created but 
        /// not sited yet inside Visual Studio environment. The place to do all the other 
        /// initialization is the Initialize method.
        /// </summary>
        public SharpOnlyPkgPackage()
        {
            Trace.WriteLine(string.Format(CultureInfo.CurrentCulture, "Entering constructor for: {0}", this.ToString()));

            // Init service...
            IServiceContainer serviceContainer = this as IServiceContainer;
            ServiceCreatorCallback callback =
               new ServiceCreatorCallback(CreateService);

            serviceContainer.AddService(typeof(MSBuildTaskService), callback, true);
            serviceContainer.AddService(typeof(SharpBuildService), callback, true);
            serviceContainer.AddService(typeof(SharpBuildDeployService), callback, true);
        }

        /////////////////////////////////////////////////////////////////////////////
        // Overriden Package Implementation
        #region Package Members

        private object CreateService(IServiceContainer container, Type serviceType)
        {
            Debug.WriteLine(@"CreateService");

            if (typeof(MSBuildTaskService) == serviceType)
                return new MSBuildTaskService(this);
            else if (typeof(SharpBuildService) == serviceType)
                return new SharpBuildService(this);
            else if (typeof(SharpBuildDeployService) == serviceType)
                return new SharpBuildDeployService(this);

            return null;
        }


        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initilaization code that rely on services provided by VisualStudio.
        /// </summary>
        protected override void Initialize()
        {
            Trace.WriteLine (string.Format(CultureInfo.CurrentCulture, "Entering Initialize() of: {0}", this.ToString()));
            base.Initialize();
        }

        int IOleCommandTarget.Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (pguidCmdGroup == GuidList.guidSharpBuildPkgCmdSet)
            {
                switch (nCmdID)
                {
                    case PkgCmdIDList.cmdidQuickBuild:
                        MenuItemCallback_Build(this, null);
                        break;

                    case PkgCmdIDList.cmdidQuickDeploy:
                        MenuItemCallback_Deploy(this, null);
                        break;

                    case PkgCmdIDList.cmdidQuickStopBuild:
                        MenuItemCallback_Stop(this, null);
                        break;
                    default:
                        break;
                }
            }

            return VSConstants.S_OK;
        }

        int IOleCommandTarget.QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            return VSConstants.S_OK;
        }

        #endregion

        private Solution GetSolution()
        {
            DTE dte = (DTE)this.GetService(typeof(SDTE));
            return dte.Solution;
        }

        private Project GetActiveProject()
        {
            DTE dte = (DTE)this.GetService(typeof(SDTE));
            Project activeProject = null;

            Array activeSolutionProjects = dte.ActiveSolutionProjects as Array;
            if (activeSolutionProjects != null && activeSolutionProjects.Length > 0)
            {
                activeProject = activeSolutionProjects.GetValue(0) as Project;
            }

            return activeProject;
        }

        private string GetMSBuildPath()
        {
            OptionPageGrid page =
                (OptionPageGrid)GetDialogPage(typeof(OptionPageGrid));
            return page.MSBuildPath;
        }

        private bool GetAutoDeploy()
        {
            OptionPageGrid page =
                (OptionPageGrid)GetDialogPage(typeof(OptionPageGrid));
            return page.AutoDeploy;
        }

        private bool GetDisableCA()
        {
            OptionPageGrid page =
                (OptionPageGrid)GetDialogPage(typeof(OptionPageGrid));
            return page.DisableCA;
        }

        private bool GetDisableOptimize()
        {
            OptionPageGrid page =
                (OptionPageGrid)GetDialogPage(typeof(OptionPageGrid));
            return page.DisableOptimize;
        }

        private List<string> GetFolders()
        {
            List<string> folders = new List<string>();

            OptionPageGrid page =
                (OptionPageGrid)GetDialogPage(typeof(OptionPageGrid));

            foreach (var item in page.DeployFolders)
            {
                DeployFolder folder = item as DeployFolder;
                if (folder != null && folder.Enabled)
                {
                    folders.Add(folder.Path);
                }
            }

            return folders;
        }

        private void MenuItemCallback_Build(object sender, EventArgs e)
        {
            var buildwnd = GetOutputPane(VSConstants.BuildOutput, "Build");

            ISharpBuildService buildService = this.GetService(typeof(SharpBuildService)) as ISharpBuildService;

            if (buildService.IsRunning)
            {
                buildwnd.OutputString(@"Build is busy....");
                return;
            }

            // Save all...
            DTE dte = (DTE)this.GetService(typeof(SDTE));
            dte.ExecuteCommand("File.SaveAll", String.Empty);

            // Update settings...
            SharpBuildDeployService deployService = this.GetService(typeof(SharpBuildDeployService)) as SharpBuildDeployService;
            deployService.UpdateSettings(GetFolders());

            Project activeProj = GetActiveProject();
            if (activeProj != null)
            {
                try
                {
                    // build
                    buildService.StartBuildProject(activeProj, GetMSBuildPath(), GetAutoDeploy(), GetDisableCA(), GetDisableOptimize());
                }
                catch (Exception ee)
                {
                    buildwnd.OutputString(string.Format(@"Build failed due to exception: {0}", ee.Message));
                }
            }
            else
            {
                // Solution build....
                Solution sln = GetSolution();
                if (sln.Projects.Count < 1)
                {
                    buildwnd.OutputString(@"Empty solution, bypass!!");
                    return;
                }
                try
                {
                    buildService.StartBuildSolution(sln, GetMSBuildPath(), GetAutoDeploy(), GetDisableCA(), GetDisableOptimize());
                }
                catch (Exception ee)
                {
                    buildwnd.OutputString(string.Format(@"Build failed due to exception: {0}", ee.Message));
                }
            }
        }

        private void MenuItemCallback_Stop(object sender, EventArgs e)
        {

        }

        private void MenuItemCallback_Deploy(object sender, EventArgs e)
        {
            // Update settings...
            SharpBuildDeployService srv = this.GetService(typeof(SharpBuildDeployService)) as SharpBuildDeployService;
            srv.UpdateSettings(GetFolders());
            Project actProj = GetActiveProject();
            if (actProj == null)
                srv.DeploySolution(GetSolution());
            else
                srv.DeployProject(actProj);
        }
    }
}
