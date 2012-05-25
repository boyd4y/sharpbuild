using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace BoydYang.SharpBuildPkg.Util
{
	public class Singleton<T> where T : class
	{
		static object SyncRoot = new object();
		static T instance;
		public static T Instance
		{
			get
			{
				if (instance == null)
				{
					lock (SyncRoot)
					{
						if (instance == null)
						{
							ConstructorInfo[] cis = typeof(T).GetConstructors(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
							if (cis.Count() == 0)
								throw new InvalidOperationException("class must contain a default constructor");
							else
								instance = (T)(cis[0]).Invoke(null);
						}
					}
				}
				return instance;
			}
		}
	}
}
