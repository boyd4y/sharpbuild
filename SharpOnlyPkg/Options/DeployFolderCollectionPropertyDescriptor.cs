using System;
using System.Text;
using System.ComponentModel;

namespace BoydYang.SharpBuildPkg.Options
{
	/// <summary>
	/// Summary description for CollectionPropertyDescriptor.
	/// </summary>
	public class DeployFolderCollectionPropertyDescriptor : PropertyDescriptor
	{
		private DeployFolderCollection collection = null;
		private int index = -1;

		public DeployFolderCollectionPropertyDescriptor(DeployFolderCollection coll, int idx) : 
			base( "#"+idx.ToString(), null )
		{
			this.collection = coll;
			this.index = idx;
		} 

		public override AttributeCollection Attributes
		{
			get 
			{ 
				return new AttributeCollection(null);
			}
		}

		public override bool CanResetValue(object component)
		{
			return true;
		}

		public override Type ComponentType
		{
			get 
			{ 
				return this.collection.GetType();
			}
		}

		public override string DisplayName
		{
			get 
			{
                return Resources.DeployFolder;
			}
		}

		public override string Description
		{
			get
			{
                return Resources.DeployFolder;
			}
		}

		public override object GetValue(object component)
		{
			return this.collection[index].Path;
		}

		public override bool IsReadOnly
		{
			get { return false;  }
		}

		public override string Name
		{
			get { return "#"+index.ToString(); }
		}

		public override Type PropertyType
		{
			get { return this.collection[index].GetType(); }
		}

		public override void ResetValue(object component)
		{
		}

		public override bool ShouldSerializeValue(object component)
		{
			return true;
		}

		public override void SetValue(object component, object value)
		{
		}
	}
}
