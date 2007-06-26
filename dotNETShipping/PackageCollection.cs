using System;
using System.Collections;

namespace dotNETShipping
{
	/// <summary>
	/// Summary description for PackageCollection.
	/// </summary>
	public class PackageCollection : IEnumerable
	{
		private ArrayList _packages;

		public PackageCollection()
		{
			this._packages = new ArrayList();
		}

		public int Count
		{
			get
			{
				return this._packages.Count;
			}
		}

		public System.Collections.IEnumerator GetEnumerator()
		{
			return this._packages.GetEnumerator();
		}

		public int Add(Package package)
		{
			return this._packages.Add(package);
		}

		public void Clear()
		{
			this._packages.Clear();
		}

		public Package this[int index]
		{
			get
			{
				return (Package)_packages[index];
			}
		}
	}
}
