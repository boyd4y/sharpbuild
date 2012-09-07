using System;
using System.Text;
using System.ComponentModel;
using System.Drawing.Design;
using System.Windows.Forms.Design;

namespace BoydYang.SharpBuildPkg.Options
{
    /// <summary>
    /// Person is the test class defining two properties: first name and last name .
    /// By deriving from GlobalizedObject the displaying of property names are language aware.
    /// GlobalizedObject implements the interface ICustomTypeDescriptor. 
    /// </summary>
    [Description("Deploy Folder")]
    [TypeConverter(typeof(DeployFolderConverter))]
    public class DeployFolder
    {
        private string _path = string.Empty;
        private bool _enabled = false;

        public DeployFolder() 
            : this(string.Empty, false)
        {
        }

        public DeployFolder(string folder, bool enabled)
        {
            this._path = folder;
            this._enabled = enabled;
        }

        [Category("Required")]
        [EditorAttribute(typeof(FolderNameEditor), typeof(UITypeEditor))]
        public string Path
        {
            get { return _path; }
            set { _path = value; }
        }

        // Uncomment the next line to see the attribute in action: 
        [Category("Required")]
        public bool Enabled
        {
            get { return _enabled; }
            set { _enabled = value; }
        }
    }
}
