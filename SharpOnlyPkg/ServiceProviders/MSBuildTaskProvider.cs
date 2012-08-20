using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell.Interop;
using BoydYang.SharpBuildPkg.Tasks;

namespace BoydYang.SharpBuildPkg.ServiceProviders
{
    [Guid("0C3DB0BF-C943-47A3-B4B1-9C9453786BCC")]
    public interface IMSBuildTaskServiceProvider
    {
        void ClearError();
        void ReportError(string msg);
        void ReportError(string msg, string file, int column, int line);
        void ReportWarning(string msg, string file, int column, int line);
    }

    [Guid("FB2C253F-6D02-474F-A25A-4C0E8779E9FB")]
    [ComVisible(true)]
    public class MSBuildTaskService : ServiceProviderBase, IMSBuildTaskServiceProvider, IDisposable
    {
        private SharpBuildTaskProvider _taskProvider;

        public MSBuildTaskService(IServiceProvider sp)
            : base(sp)
        {
            _taskProvider = new SharpBuildTaskProvider(sp);
        }

        public void ClearError()
        {
            _taskProvider.Tasks.Clear();
        }

        public void ReportError(string msg)
        {
            InternalReportErrorWarning(true, msg, string.Empty, 0, 0); 
        }

        public void ReportError(string msg, string file, int column, int line)
        {
            InternalReportErrorWarning(true, msg, file, column, line); 
        }

        public void ReportWarning(string msg, string file, int column, int line)
        {
            InternalReportErrorWarning(false, msg, file, column, line);
        }

        public void InternalReportErrorWarning(bool isError, string msg, string file, int column, int line)
        {
            var errorTask = new ErrorTask();
            errorTask.CanDelete = false;

            errorTask.ErrorCategory = isError ? TaskErrorCategory.Error : TaskErrorCategory.Warning;
            errorTask.Text = msg;
            errorTask.Document = file;
            errorTask.Line = line;
            errorTask.Column = column;
            errorTask.Navigate += new EventHandler(errorTask_Navigate);
            _taskProvider.Tasks.Add(errorTask);

            _taskProvider.Show();

            var taskList = serviceProvider.GetService(typeof(SVsTaskList))
                as IVsTaskList2;
            if (taskList == null)
            {
                return;
            }
            var guidProvider = typeof(SharpBuildTaskProvider).GUID;
            taskList.SetActiveProvider(ref guidProvider);
        }

        void errorTask_Navigate(object sender, EventArgs e)
        {
        }

        public void Dispose()
        {
            if (_taskProvider != null)
            {
                _taskProvider.Dispose();
                _taskProvider = null;
            }
        }
    }
}
