using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using System.IO;
using Microsoft.VisualStudio.Shell.Interop;

namespace BoydYang.SharpBuildPkg.ServiceProviders
{
    interface DeployService 
    {
        void DeployProject(Project project);
        void DeploySolution(Solution sln);
    }

    public class DeployServiceProvider : DeployService
    {
        List<string> QuickDeployFolders = new List<string>();
        public IVsOutputWindowPane BuildWindow { get; set; }

        public void UpdateSettings(List<string> folders)
        {
            QuickDeployFolders = new List<string>();
            foreach (var item in folders)
            {
                if (Directory.Exists(item) || !QuickDeployFolders.Contains(item))
                    QuickDeployFolders.Add(item);
            }
        }

        public void DeployProject(Project project)
        {
            DeployProject(project, true);
        }

        private void DeployProject(Project project, bool log)
        {
            string path;
            string file;

            try
            {
                GetProjectTargetAssemblyPath(project, out path, out file);
            }
            catch (Exception ee)
            {
                if (log)
                    BuildWindow.OutputString(string.Format("-------- Failed to locate targetdir for project {0} : Reason:{1}--------\r\n", project.Name, ee.Message));
                return;
            }

            if (log)
                BuildWindow.OutputString(string.Format("-------- Start to deploy project {0} --------\r\n", project.Name));

            foreach (var item in QuickDeployFolders)
            {
                string targetpath = Path.Combine(item, file);

                if (!File.Exists(path))
                    BuildWindow.OutputString(string.Format("********* File {0} not exist *********\r\n", file));
                else
                {
                    try
                    {
                        File.Copy(path, targetpath, true);
                        BuildWindow.OutputString(string.Format("********* Copy file {0} to {1} *********\r\n", file, item));
                    }
                    catch (Exception ee)
                    {
                        BuildWindow.OutputString(string.Format("********* Failed Copy file {0} to {1} @{2} *********\r\n", file, item, ee.Message));
                    }
                }
            }

            if (log)
                BuildWindow.OutputString(string.Format("-------- Complete to deploy project --------\r\n"));
        }

        private void DeploySolutionFolder(ProjectItems items)
        {
            if (items == null)
                return;

            // Maybe solution folder...
            foreach (ProjectItem pi in items)
            {
                if (pi.SubProject != null)
                {
                    if (pi.SubProject.ConfigurationManager != null)
                        DeployProject(pi.SubProject, false);
                    else
                        DeploySolutionFolder(pi.ProjectItems);
                }
            }
        }

        public void DeploySolution(Solution sln)
        {
            BuildWindow.OutputString(string.Format("-------- Start to deploy solution --------\r\n"));

            foreach (Project item in sln.Projects)
            {
                if (item.ConfigurationManager != null)
                    DeployProject(item, false);
                else
                {
                    // Maybe solution folder...
                    DeploySolutionFolder(item.ProjectItems);
                }
            }

            BuildWindow.OutputString(string.Format("-------- Complete to deploy solution --------\r\n"));
        }

        private void GetProjectTargetAssemblyPath(Project project, out string absoluteOutputPath, out string outputfilename)
        {
            string outpath = project.ConfigurationManager.ActiveConfiguration.Properties.Item("OutputPath").Value.ToString();
            outputfilename = project.Properties.Item("OutputFileName").Value.ToString();
            absoluteOutputPath = string.Empty;

            if (outpath.StartsWith(new string(Path.DirectorySeparatorChar, 2)))
            {
                absoluteOutputPath = outpath;
            }
            else if (outpath.Length > 2 && outpath.StartsWith(new string(Path.VolumeSeparatorChar, 1)))
            {
                absoluteOutputPath = outpath;
            }
            else
            {
                string projectFolder = Path.GetDirectoryName(project.FullName);
                absoluteOutputPath = Path.Combine(projectFolder, outpath);
            }
            absoluteOutputPath = Path.Combine(absoluteOutputPath, outputfilename);
        }
    }
}
