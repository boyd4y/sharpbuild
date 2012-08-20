using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace BoydYang.SharpBuildPkg.Tasks
{
    [Guid("44BA4AF1-29DD-4002-8AA0-CF6EB6DA1A62")]
    public class SharpBuildTaskProvider : ErrorListProvider
    {
        public SharpBuildTaskProvider(IServiceProvider sp)
            : base(sp)
        {
            this.ProviderName = @"Sharp Build";
        }
    }
}
