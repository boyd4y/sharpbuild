using System;
using System.ComponentModel;
using System.Text;

namespace BoydYang.SharpBuildPkg.Options
{
	// This is a special type converter which will be associated with the Employee class.
	// It converts an Employee object to string representation for use in a property grid.
	internal class DeployFolderConverter : ExpandableObjectConverter
	{
		public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destType )
		{
			if( destType == typeof(string) && value is DeployFolder )
			{
				// Cast the value to an Employee type
                DeployFolder emp = (DeployFolder)value;

				// Return department and department role separated by comma.
				return emp.Path;
			}
			return base.ConvertTo(context,culture,value,destType);
		}
	}
}
