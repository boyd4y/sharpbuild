using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel.Design;

namespace BoydYang.SharpBuildPkg.Options
{
    public class DeployFolderCollectionEditor : CollectionEditor
    {
        public DeployFolderCollectionEditor(Type type)
            : base(type)
        {
        }

        protected override string GetDisplayText(object value)
        {
            DeployFolder item = null;

            if (value is DeployFolder)
            {
                item = (DeployFolder)value;
                return string.Format("<{0}>", item.Path);
            }

            return "Deploy Folder";
        }
    }
}
