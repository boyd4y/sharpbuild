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

	// This is a special type converter which will be associated with the EmployeeCollection class.
	// It converts an EmployeeCollection object to a string representation for use in a property grid.
	internal class DeployFolderCollectionConverter : ExpandableObjectConverter
	{
		public override object ConvertTo(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value, Type destType )
		{
			if( destType == typeof(string) && value is DeployFolderCollection )
			{
                StringBuilder sb = new StringBuilder();
                DeployFolderCollection col = value as DeployFolderCollection;

                foreach (DeployFolder item in col)
                {
                    sb.AppendFormat("{0}{1}|", item.Path, item.Enabled ? "*" : string.Empty);
                }
				return sb.ToString();
			}
			return base.ConvertTo(context,culture,value,destType);
		}

        public override object ConvertFrom(ITypeDescriptorContext context, System.Globalization.CultureInfo culture, object value)
        {
            if (value is string)
            {
                string s = value.ToString();
                string[] allfolders = s.Split(new string[] { "|" }, StringSplitOptions.RemoveEmptyEntries);

                DeployFolderCollection collection = new DeployFolderCollection();

                foreach (var item in allfolders)
	            {
                    collection.Add(new DeployFolder(item.Trim(new char[] {'|', '*'}), item.EndsWith("*")));
	            }

                return collection;
            }

            return base.ConvertFrom(context, culture, value);
        }

        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType)
        {
            if (sourceType == typeof(string))
                return true;

            return base.CanConvertFrom(context, sourceType);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType)
        {
            return base.CanConvertTo(context, destinationType);
        }
	}

}
