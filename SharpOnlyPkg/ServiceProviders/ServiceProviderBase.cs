using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio;

namespace BoydYang.SharpBuildPkg.ServiceProviders
{
    public class ServiceProviderBase
    {
        protected IServiceProvider serviceProvider;
        protected IVsOutputWindowPane __outputpane;

        protected IVsOutputWindowPane OutputPane
        {
            get
            {
                if (__outputpane == null)
                {
                    IVsOutputWindow output = this.serviceProvider.GetService(typeof(SVsOutputWindow)) as IVsOutputWindow;
                    output.GetPane(VSConstants.BuildOutput, out __outputpane);
                }

                return __outputpane;
            }
        }

        public ServiceProviderBase(IServiceProvider sp)
        {
            serviceProvider = sp;
        }
    }
}
