using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using BoydYang.SharpBuildPkg.BuildRunner;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace BoydYang.SharpBuildPkg.ServiceProviders
{
    [Guid("07DB9427-C38C-49CB-950E-6EA80BC83EF1")]
    interface ISharpBuildService
    {
        bool IsRunning { get; }

        void StartBuildSolution(Solution buildSolution, string msbuildPath, bool autoDeploy, bool disableCA, bool disableOptimize);
        void StartBuildProject(Project buildProj, string msbuildPath, bool autoDeploy, bool disableCA, bool disableOptimize);

        void StopBuild();
    }

    [Guid("921BC3F9-ECD7-42DB-8146-91FA2BC74230")]
    [ComVisible(true)]
    public class SharpBuildService : ServiceProviderBase, ISharpBuildService
    {
        private System.Collections.Generic.Dictionary<Project, MSBuildRunner> builderTable = new System.Collections.Generic.Dictionary<Project, MSBuildRunner>();
        private MSBuildRunner __currentrunner = null;

        public bool IsRunning
        {
            get
            {
                return __currentrunner != null ? __currentrunner.IsRunning : false;
            }
        }

        public SharpBuildService(IServiceProvider sp)
            : base(sp)
        {
        }

        public void StartBuildSolution(Solution buildSolution, string msbuildPath, bool autoDeploy, bool disableCA, bool disableOptimize)
        {
            if (IsRunning)
            {
                OutputPane.OutputStringThreadSafe(@"Builder is busy now...\r\n");
                return;
            }
            __currentrunner = new MSBuildRunner(this.serviceProvider, msbuildPath, "Solution", buildSolution.FullName, OutputPane);
            __currentrunner.Configuration = buildSolution.Projects.Item(1).ConfigurationManager.ActiveConfiguration.ConfigurationName;
            __currentrunner.AutoDeploy = autoDeploy;
            __currentrunner.DisableCA = disableCA;
            __currentrunner.DisableOptimize = disableOptimize;
            __currentrunner.BuildProject = null;
            __currentrunner.BuildSolution = buildSolution;

            __currentrunner.OnBuildError += new MSBuildRunner.BuildErrorEventHandler(__currentrunner_OnBuildError);
            __currentrunner.OnBuildInternalError += new MSBuildRunner.BuildInternalEventHandler(__currentrunner_OnBuildInternalError);
            __currentrunner.OnBuildWarning += new MSBuildRunner.BuildWarningEventHandler(__currentrunner_OnBuildWarning);
            __currentrunner.OnBuildFinished += new MSBuildRunner.BuildFinishedEventHandler(__currentrunner_OnBuildFinished);
            __currentrunner.OnBuildStart += new MSBuildRunner.BuildStartEventHandler(__currentrunner_OnBuildStart);

            __currentrunner.Start();
        }

        public void StartBuildProject(Project buildProj, string msbuildPath, bool autoDeploy, bool disableCA, bool disableOptimize)
        {
            if (IsRunning)
            {
                OutputPane.OutputStringThreadSafe(@"Builder is busy now...\r\n");
                return;
            }

            __currentrunner = new MSBuildRunner(this.serviceProvider, msbuildPath, buildProj.Name, buildProj.FullName, OutputPane);
            __currentrunner.Configuration = buildProj.ConfigurationManager.ActiveConfiguration.ConfigurationName;
            __currentrunner.AutoDeploy = autoDeploy;
            __currentrunner.DisableCA = disableCA;
            __currentrunner.DisableOptimize = disableOptimize;
            __currentrunner.BuildProject = buildProj;

            __currentrunner.OnBuildError += new MSBuildRunner.BuildErrorEventHandler(__currentrunner_OnBuildError);
            __currentrunner.OnBuildInternalError += new MSBuildRunner.BuildInternalEventHandler(__currentrunner_OnBuildInternalError);
            __currentrunner.OnBuildWarning += new MSBuildRunner.BuildWarningEventHandler(__currentrunner_OnBuildWarning);
            __currentrunner.OnBuildFinished += new MSBuildRunner.BuildFinishedEventHandler(__currentrunner_OnBuildFinished);
            __currentrunner.OnBuildStart += new MSBuildRunner.BuildStartEventHandler(__currentrunner_OnBuildStart);

            __currentrunner.Start();
        }

        void __currentrunner_OnBuildStart(object sender, SharpBuildLogger.Events.SharpBuildStartEvent e)
        {
            IMSBuildTaskServiceProvider buildSrv = this.serviceProvider.GetService(typeof(MSBuildTaskService)) as IMSBuildTaskServiceProvider;
            buildSrv.ClearError();
        }

        void __currentrunner_OnBuildFinished(object sender, SharpBuildLogger.Events.SharpBuildFinishedEvent e)
        {
            if (e.Successed)
            {
                // Trigger to deploy....
                if (__currentrunner.AutoDeploy)
                {
                    SharpBuildDeployService srv = this.serviceProvider.GetService(typeof(SharpBuildDeployService)) as SharpBuildDeployService;
                    if (__currentrunner.BuildProject != null)
                        srv.DeployProject(__currentrunner.BuildProject);
                    else
                        srv.DeploySolution(__currentrunner.BuildSolution);
                }
            }
        }

        void __currentrunner_OnBuildWarning(object sender, SharpBuildLogger.Events.SharpBuildWarningEvent e)
        {
            IMSBuildTaskServiceProvider buildSrv = this.serviceProvider.GetService(typeof(MSBuildTaskService)) as IMSBuildTaskServiceProvider;
            buildSrv.ReportWarning(e.Message, e.File, e.ColumnNumber, e.LineNumber);
        }

        void __currentrunner_OnBuildInternalError(object sender, SharpBuildLogger.Events.SharpBuildInternalErrorEvent e)
        {
            IMSBuildTaskServiceProvider buildSrv = this.serviceProvider.GetService(typeof(MSBuildTaskService)) as IMSBuildTaskServiceProvider;
            buildSrv.ReportError(e.Message);
        }

        void __currentrunner_OnBuildError(object sender, SharpBuildLogger.Events.SharpBuildErrorEvent e)
        {
            IMSBuildTaskServiceProvider buildSrv = this.serviceProvider.GetService(typeof(MSBuildTaskService)) as IMSBuildTaskServiceProvider;
            buildSrv.ReportError(e.Message, e.File, e.ColumnNumber, e.LineNumber);
        }

        public void StopBuild()
        {
            if (IsRunning)
            {
                OutputPane.Activate();
                OutputPane.OutputString(string.Format(@"Force stop msbuild {0}", __currentrunner.BuildFullFileName));

                if (__currentrunner.IsRunning)
                {
                    __currentrunner.Start();
                }
            }
        }
    }
}
