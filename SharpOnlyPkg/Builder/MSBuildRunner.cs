using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.IO;
using System.Threading;
using EnvDTE;
using Process = System.Diagnostics.Process;
using Thread = System.Threading.Thread;
using BoydYang.SharpBuildPkg.Options;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell.Interop;
using BoydYang.SharpBuildPkg.Loggers;
using System.ComponentModel;
using System.IO.Pipes;
using BoydYang.SharpBuildPkg.Util;
using BoydYang.SharpBuildPkg.ServiceProviders;

namespace BoydYang.SharpBuildPkg.BuildRunner
{
	public class MSBuildRunner
	{
        private IServiceProvider Host { get; set; }
		public string BuildFullFileName { get; set; }
        public IVsOutputWindowPane BuildWindow { get; set; }
		public string LogFile { get; set; }
		private Process _buildProcess;
		private volatile bool _building;
        private string MSBUILD = string.Empty;
        private string ProjectName = string.Empty;
        private NamedPipeServerStream serverPipe = null;
        private bool _pipeStopper = false;

        public Project BuildProject { get; set; }
        public Solution BuildSolution { get; set; }
        public bool AutoDeploy { get; set; }
        public bool DisableCA { get; set; }

        private string PIPENAME
        {
            get
            {
                return "sharpbuild";
            }
        }

        public bool IsRunning
        {
            get
            {
                return _building;
            }
        }

        public MSBuildRunner(IServiceProvider host, string msbuild, string projectName, string buildFullFileName, IVsOutputWindowPane buildWnd)
        {
            Host = host;
			BuildFullFileName = buildFullFileName;
            MSBUILD = msbuild;
            ProjectName = projectName;
            BuildWindow = buildWnd; // host.GetOutputPane(VSConstants.BuildOutput, "Build");
		}

		public void Start()
		{
			_building = true;
			//BuildWindow.OutputString(LogFile + "(1,1): msbuild " + BuildArguments() + "\r\n");
			Trace.WriteLine("Starting build...");
            ThreadPool.QueueUserWorkItem(StartPipeListen);
            ThreadPool.QueueUserWorkItem(StartBuildProcess);
		}

        private void StopPipeListen()
        {
            _pipeStopper = true;
            System.Threading.Thread.Sleep(200);
        }

        private void StartPipeListen(object state)
        {
            try
            {
                serverPipe = new NamedPipeServerStream(PIPENAME, PipeDirection.In);
                Trace.WriteLine("Waiting for connection...");
                serverPipe.WaitForConnection();
                Trace.WriteLine("Connection established...");

                using (StreamReader sr = new StreamReader(serverPipe))
                {
                    while (!_pipeStopper)
                    {
                        string content = sr.ReadLine();
                        if (!string.IsNullOrEmpty(content))
                            BuildWindow.OutputStringThreadSafe(content + "\r\n");
                        System.Threading.Thread.Sleep(200);
                    }
                }

                Debug.WriteLine("close pipe....");
                serverPipe.Close();
                _pipeStopper = false;
            }
            finally
            {
            }
        }

		private void StartBuildProcess(object state)
		{
            try
            {
                // We are on a separate thread - read and marshal back to the UI!
                // this will monitor, capture and re-print errors and warnings for your project
                ProcessStartInfo processStartInfo = GetProcessStartInfo();

                _buildProcess = new Process();
                _buildProcess.StartInfo = processStartInfo;
                _buildProcess.Exited += new EventHandler(_buildProcess_Exited);
                _buildProcess.EnableRaisingEvents = true;
                _buildProcess.Start();
                Thread.Sleep(100); // give it a chance to start
            }
            catch (Exception ee)
            {
                this.BuildWindow.OutputString(string.Format(@"Build failed due to exception: {0}", ee.Message));
                _building = false;
            }
			finally
			{
			}
		}

        void _buildProcess_Exited(object sender, EventArgs e)
        {
            _building = false;

            StopPipeListen();

            // Trigger to deploy....
            if (AutoDeploy)
            {
                if (BuildProject != null)
                    Singleton<DeployServiceProvider>.Instance.DeployProject(BuildProject);
                else
                    Singleton<DeployServiceProvider>.Instance.DeploySolution(BuildSolution);
            }
        }

		private ProcessStartInfo GetProcessStartInfo()
		{
            string args = BuildArguments();
            Debug.WriteLine(args);

            var processStartInfo = new ProcessStartInfo(MSBUILD)
									{
                                        Arguments = args,
										WorkingDirectory = BuildFileLocation,
										CreateNoWindow = true,
										UseShellExecute = false,
									};
			return processStartInfo;
		}

		private string BuildArguments()
		{
			var builder = new StringBuilder();
			builder.Append(BuildFileName);
			builder.Append(" ");
			builder.Append(" /v:q "); // verbosity: minimal
            builder.Append(@"/p:BuildProjectReferences=false "); // skip project reference...
            if (this.DisableCA)
                builder.Append(@" /p:RunCodeAnalysis=false "); // skip project reference...
            if (!string.IsNullOrEmpty(Configuration))
			    builder.AppendFormat(" /property:Configuration=\"{0}\" ", Configuration); // verbosity: minimal
			var assembly = typeof (MSBuildRunner).Assembly;
            Uri assemblyUri = new Uri(assembly.GetName().CodeBase);
            var path = assemblyUri.LocalPath;
            builder.Append(" /flp:Verbosity=minimal ");
            builder.AppendFormat(" /logger:{0},{1} ", typeof(MSBuildLogger).FullName, path);
            return builder.ToString();
		}

		private string BuildFileLocation
		{
			get { return Path.GetDirectoryName(BuildFullFileName); }
		}
		private string BuildFileName
		{
			get { return Path.GetFileName(BuildFullFileName); }
		}

		public string Configuration { get; set; }
		public bool Building { get { return _building; } }
		
		internal void KillBuild()
		{
			try
			{
				Trace.WriteLine("Killing the build ...");
				if (_buildProcess != null && !_buildProcess.HasExited)
				{
					BuildWindow.OutputString("\r\n\r\nCancel build ...\r\n\r\n");
					_buildProcess.Kill();
				}
			}
			catch (Exception ex)
			{
				BuildWindow.OutputString(ex.Message);
			}
		}
	}
}
