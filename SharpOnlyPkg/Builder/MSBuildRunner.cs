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
using System.ComponentModel;
using System.IO.Pipes;
using BoydYang.SharpBuildPkg.Util;
using BoydYang.SharpBuildPkg.ServiceProviders;
using BoydYang.SharpBuildLogger.Loggers;
using BoydYang.SharpBuildLogger.Events;
using BoydYang.SharpBuildLogger.Constant;

namespace BoydYang.SharpBuildPkg.BuildRunner
{
	public class MSBuildRunner : IDisposable
	{
        public delegate void BuildStartEventHandler(object sender, SharpBuildStartEvent e);
        public delegate void BuildFinishedEventHandler(object sender, SharpBuildFinishedEvent e);
        public delegate void BuildErrorEventHandler(object sender, SharpBuildErrorEvent e);
        public delegate void BuildWarningEventHandler(object sender, SharpBuildWarningEvent e);
        public delegate void BuildInternalEventHandler(object sender, SharpBuildInternalErrorEvent e);

        private IServiceProvider Host { get; set; }
		public string BuildFullFileName { get; set; }
        public IVsOutputWindowPane BuildWindow { get; set; }
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
        public bool DisableOptimize { get; set; }

        private string _pipename { get; set; }
        private string _buildProjectShadowFile;

        public event BuildErrorEventHandler OnBuildError;
        public event BuildWarningEventHandler OnBuildWarning;
        public event BuildInternalEventHandler OnBuildInternalError;
        public event BuildStartEventHandler OnBuildStart;
        public event BuildFinishedEventHandler OnBuildFinished;
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
            BuildWindow = buildWnd;

            _pipename = string.Format(@"_sharponly_{0}", buildFullFileName.GetHashCode().ToString());
		}

        public void Stop()
        {
            try
            {
                _buildProcess.Kill();
                StopPipeListen();
                _buildProcess.WaitForExit();
                _building = false;
            }
            catch (Exception e)
            {
                BuildWindow.OutputString("Stop msbuild failed due to:  " + e.Message);
            }
        }

		public void Start()
		{
			_building = true;
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
                serverPipe = new NamedPipeServerStream(_pipename, PipeDirection.In);
                Trace.WriteLine("Waiting for connection...");
                serverPipe.WaitForConnection();
                Trace.WriteLine("Connection established...");

                using (StreamReader sr = new StreamReader(serverPipe))
                {
                    while (!_pipeStopper)
                    {
                        string content = sr.ReadLine();
                        if (!string.IsNullOrEmpty(content))
                        {
                            // Json des...
                            try
                            {
                                SharpBuildEventWrapper eventWrapper = Newtonsoft.Json.JsonConvert.DeserializeObject<SharpBuildEventWrapper>(content);

                                switch (eventWrapper.EventType)
                                {
                                    case SharpBuildEventType.Start:
                                        {
                                            SharpBuildStartEvent log = Newtonsoft.Json.JsonConvert.DeserializeObject<SharpBuildStartEvent>(eventWrapper.EmbeddedMessage);
                                            string msg = string.Format("*-------- Build Started: {0} --------*\r\n", log.GeneratedTime.ToLongTimeString());
                                            
                                            BuildWindow.OutputStringThreadSafe(msg);

                                            // report error...
                                            if (OnBuildStart != null)
                                                OnBuildStart(this, log);
                                        }
                                        break;
                                    case SharpBuildEventType.ErrorLog:
                                        {
                                            SharpBuildErrorEvent log = Newtonsoft.Json.JsonConvert.DeserializeObject<SharpBuildErrorEvent>(eventWrapper.EmbeddedMessage);
                                            string msg = string.Format("{0}({1},{2}): error {3}: {4}\r\n", log.File, log.LineNumber, log.ColumnNumber, log.Code, log.Message);
                                            BuildWindow.OutputStringThreadSafe(msg);

                                            // report error...
                                            if (OnBuildError != null)
                                                OnBuildError(this, log);
                                        }
                                        break;
                                    case SharpBuildEventType.WarningLog:
                                        {
                                            SharpBuildWarningEvent log = Newtonsoft.Json.JsonConvert.DeserializeObject<SharpBuildWarningEvent>(eventWrapper.EmbeddedMessage);
                                            string msg = string.Format("{0}({1},{2}): warning {3}: {4}", log.File, log.LineNumber, log.ColumnNumber, log.Code, log.Message);
                                            BuildWindow.OutputStringThreadSafe(msg + "\r\n");

                                            // report error...
                                            if (OnBuildWarning != null)
                                                OnBuildWarning(this, log);
                                        }
                                        break;
                                    case SharpBuildEventType.Log:
                                        {
                                            SharpBuildLogEvent log = Newtonsoft.Json.JsonConvert.DeserializeObject<SharpBuildLogEvent>(eventWrapper.EmbeddedMessage);
                                            BuildWindow.OutputStringThreadSafe(log.Message + "\r\n");
                                        }
                                        break;

                                    case SharpBuildEventType.InternalError:
                                        {
                                            SharpBuildInternalErrorEvent error = Newtonsoft.Json.JsonConvert.DeserializeObject<SharpBuildInternalErrorEvent>(eventWrapper.EmbeddedMessage);
                                            BuildWindow.OutputStringThreadSafe(error.Message + "\r\n");
                                            if (OnBuildInternalError != null)
                                                OnBuildInternalError(this, error);
                                        }
                                        break;

                                    case SharpBuildEventType.Finished:
                                        {
                                            SharpBuildFinishedEvent finishedevnt = Newtonsoft.Json.JsonConvert.DeserializeObject<SharpBuildFinishedEvent>(eventWrapper.EmbeddedMessage);
                                            string msg = string.Format("*-------- Build Finished: {0} {1}--------*\r\n", finishedevnt.Successed ? "Success" : "Falied", finishedevnt.GeneratedTime.ToLongTimeString());
                                            BuildWindow.OutputStringThreadSafe(msg);

                                            if (OnBuildFinished != null)
                                                OnBuildFinished(this, finishedevnt);
                                        }
                                        break;

                                    default:
                                        break;
                                }
                                
                            }
                            catch (Exception ee)
                            {
                                BuildWindow.OutputStringThreadSafe(string.Format("Failed to get pipe events, general issues {0} \r\n ", ee.Message));
                            }
                        }
                        System.Threading.Thread.Sleep(200);
                    }
                }

                Debug.WriteLine("close pipe....");
                serverPipe.Close();
                _pipeStopper = false;
            }
            catch (Exception e)
            {
                BuildWindow.OutputString(@"Failed to setup msbuild due to:  " + e.Message);
            }
            finally
            {
            }
        }

		private void StartBuildProcess(object state)
		{
            try
            {
                // if build project....
                if (BuildProject != null)
                {
                    // clone, and make a shadow copy of original project file....
                    try
                    {
                        string fullname;
                        ISharpBuildService buildService = this.Host.GetService(typeof(SharpBuildService)) as ISharpBuildService;
                        buildService.GenerateNewProjectFile(BuildProject, BuildSolution, out fullname);
                        _buildProjectShadowFile = System.IO.Path.GetFileName(fullname);
                    }
                    catch (Exception ee)
                    {
                        BuildWindow.OutputString(ee.Message);
                    }
                }

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
            StopPipeListen();
            _building = false;

            try
            {
                if (BuildProject != null)
                {
                    ISharpBuildService buildService = this.Host.GetService(typeof(SharpBuildService)) as ISharpBuildService;
                    buildService.DeleteShadowProjectFile(BuildProject);
                }
            }
            catch (Exception ee)
            {
                this.BuildWindow.OutputString(string.Format(@"Faield to delete shadow project file: {0}", ee.Message));
            }
        }

		private ProcessStartInfo GetProcessStartInfo()
		{
            string args = BuildArguments();
            BuildWindow.OutputString(args + "\r\n");

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
            builder.Append(string.IsNullOrEmpty(_buildProjectShadowFile) ? BuildFileName : _buildProjectShadowFile);
			builder.Append(@" /v:q "); // verbosity: minimal

            // Only apply below property while building project.
            if (BuildProject != null)
            {
                builder.Append(@"/p:BuildProjectReferences=false "); // skip project reference...
                if (this.DisableOptimize)
                    builder.Append(@" /p:Optimize=false "); // skip project reference...
                if (this.DisableCA)
                    builder.Append(@" /p:RunCodeAnalysis=false "); // skip project reference...
            }
            else
            {
                // Build project only... refine output project targets file if needed...
                // Change msbuild project file in fly to fix some criticl bugs...
            }

            if (!string.IsNullOrEmpty(Configuration))
			    builder.AppendFormat(" /property:Configuration=\"{0}\" ", Configuration); // verbosity: minimal
            var assembly = typeof(MSBuildLogger).Assembly;
            Uri assemblyUri = new Uri(assembly.GetName().CodeBase);
            var path = assemblyUri.LocalPath;
            builder.Append(" /flp:Verbosity=minimal ");
            builder.AppendFormat(" /logger:{0},\"{1}\";{2} ", typeof(MSBuildLogger).FullName, path, _pipename);
            return builder.ToString();
		}

        private string ShadowBuildFullFileName
        {
            get
            {
                return string.Format(@"{0}_{1}", Guid.NewGuid().ToString(), BuildFullFileName);
            }
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

        public void Dispose()
        {
            if (serverPipe != null)
            {
                serverPipe.Close();
                serverPipe = null;
            }
        }
    }
}
