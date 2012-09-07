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
using System.Xml.XPath;
using System.Xml;
using System.IO;

namespace BoydYang.SharpBuildPkg.ServiceProviders
{
    [Guid("07DB9427-C38C-49CB-950E-6EA80BC83EF1")]
    interface ISharpBuildService
    {
        bool IsRunning { get; }

        void StartBuildSolution(Solution buildSolution, string msbuildPath, bool autoDeploy, bool disableCA, bool disableOptimize);
        void StartBuildProject(Solution buildSolution, Project buildProj, string msbuildPath, bool autoDeploy, bool disableCA, bool disableOptimize);
        void DeleteShadowProjectFile(Project project);
        bool GenerateNewProjectFile(Project project, Solution sln, out string newPath);

        void StopBuild();
    }

    [Guid("921BC3F9-ECD7-42DB-8146-91FA2BC74230")]
    [ComVisible(true)]
    public class SharpBuildService : ServiceProviderBase, ISharpBuildService
    {
        private System.Collections.Generic.Dictionary<Project, MSBuildRunner> builderTable = new System.Collections.Generic.Dictionary<Project, MSBuildRunner>();
        private MSBuildRunner __currentrunner = null;
        private const string ShadowFileSuffix = @".shadow";

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

        public void StartBuildProject(Solution buildSolution, Project buildProj, string msbuildPath, bool autoDeploy, bool disableCA, bool disableOptimize)
        {
            if (IsRunning)
            {
                OutputPane.OutputStringThreadSafe(@"Builder is busy now...\r\n");
                return;
            }

            __currentrunner = new MSBuildRunner(this.serviceProvider, msbuildPath, buildProj.Name, buildProj.FullName, OutputPane);
            __currentrunner.Configuration = buildProj.ConfigurationManager.ActiveConfiguration.ConfigurationName;
            __currentrunner.BuildSolution = buildSolution;
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

        private List<Project> GetProjectList(ProjectItems items)
        {
            List<Project> refs = new List<Project>();

            if (items != null)
            {
                foreach (ProjectItem pi in items)
                {
                    if (pi.SubProject != null)
                    {
                        if (pi.SubProject.ConfigurationManager != null)
                            refs.Add(pi.SubProject);
                        else
                            refs.AddRange(GetProjectList(pi.ProjectItems));
                    }
                }
            }

            return refs;
        }

        private List<Project> GetProjectList(Solution sln)
        {
            List<Project> refs = new List<Project>();
            foreach (Project item in sln.Projects)
            {
                if (item.ConfigurationManager != null)
                    refs.Add(item);
                else
                {
                    // Maybe solution folder...
                    foreach (ProjectItem pi in item.ProjectItems)
                    {
                        if (pi.SubProject != null)
                        {
                            if (pi.SubProject.ConfigurationManager != null)
                                refs.Add(pi.SubProject);
                            else
                                refs.AddRange(GetProjectList(pi.ProjectItems));
                        }
                    }
                }
            }

            return refs;
        }

        public void DeleteShadowProjectFile(Project project)
        {
            string newPath = string.Format(@"{0}{1}", project.FullName, ShadowFileSuffix);
            if (System.IO.File.Exists(newPath))
            {
                FileAttributes atts = File.GetAttributes(newPath);
                if ((atts & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                    File.SetAttributes(newPath, FileAttributes.Normal);
                System.IO.File.Delete(newPath);
            }
        }

        public bool GenerateNewProjectFile(Project project, Solution sln, out string newPath)
        {
            newPath = string.Format(@"{0}{1}", project.FullName, ShadowFileSuffix);
            bool result = false;

            List<Project> projects = GetProjectList(sln);
            SharpBuildDeployService buildService = this.serviceProvider.GetService(typeof(SharpBuildDeployService)) as SharpBuildDeployService;

            try
            {
                System.IO.File.Copy(project.FullName, newPath, true);

                XmlDocument document = new XmlDocument();
                document.Load(newPath);
                XPathNavigator navigator = document.CreateNavigator();

                XmlNamespaceManager manager = new XmlNamespaceManager(document.NameTable);
                manager.AddNamespace("bk", "http://schemas.microsoft.com/developer/msbuild/2003");

                Dictionary<string, string> projectRefs = new Dictionary<string, string>();
                XPathNodeIterator firstProjectReference = navigator.Select("/bk:Project/bk:ItemGroup/bk:ProjectReference", manager);
                while (firstProjectReference.MoveNext())
                {
                    // find project...
                    string name = firstProjectReference.Current.SelectSingleNode("bk:Name", manager).Value;

                    Project pro = projects.FirstOrDefault(p => p.Name == name);

                    if (pro != null)
                    {
                        string targetPath = string.Empty;
                        string targetFileName = string.Empty;
                        buildService.GetProjectTargetAssemblyPath(pro, out targetPath, out targetFileName);
                        projectRefs.Add(name, targetPath);

                        firstProjectReference.Current.DeleteSelf();
                        firstProjectReference = navigator.Select("/bk:Project/bk:ItemGroup/bk:ProjectReference", manager);
                    }
                }

                XPathNavigator reference = navigator.SelectSingleNode("/bk:Project/bk:ItemGroup/bk:Reference", manager);
                reference.MoveToParent();

                foreach (var item in projectRefs)
                {
                    string refstr = string.Format("<Reference Include=\"{0}\"><HintPath>{1}</HintPath></Reference>", item.Key, item.Value);
                    reference.AppendChild(refstr);
                }

                document.Save(newPath);
                result = true;
            }
            catch (Exception e)
            {
            }

            return result;
        }
    }
}
