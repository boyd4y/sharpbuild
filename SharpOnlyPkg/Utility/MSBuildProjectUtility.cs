using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using EnvDTE;
using System.Xml;
using System.Xml.XPath;
using BoydYang.SharpBuildPkg.ServiceProviders;

namespace BoydYang.SharpBuildPkg.Utility
{
    public class MSBuildProjectUtility
    {
        private const string ShadowFileSuffix = @".shadow";

        protected MSBuildProjectUtility()
        {
        }

        private List<Project> GetProjectList(ProjectItems items)
        {
            List<Project> refs = new List<Project>();
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
                System.IO.File.Delete(newPath);
            }
        }

        public bool GenerateNewProjectFile(IServiceProvider sp, Project project, Solution sln, out string newPath)
        {
            newPath = string.Format(@"{0}{1}", project.FullName, ShadowFileSuffix);
            bool result = false;

            List<Project> projects = GetProjectList(sln);
            SharpBuildDeployService buildService = sp.GetService(typeof(SharpBuildDeployService)) as SharpBuildDeployService;

            try
            {
                System.IO.File.Copy(project.FullName, newPath, true);

                XmlDocument document = new XmlDocument();
                document.Load(newPath);
                XPathNavigator navigator = document.CreateNavigator();

                XmlNamespaceManager manager = new XmlNamespaceManager(document.NameTable);
                manager.AddNamespace("bk", "http://schemas.microsoft.com/developer/msbuild/2003");

                Dictionary<string, string> projectRefs = new Dictionary<string,string>();
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
