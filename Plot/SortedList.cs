using System;
using System.Collections.Generic;

namespace Plot
{
	public class SortedList<T> : List<T>
	{
		private IComparer<T> d_comparer;

		public SortedList(IComparer<T> comparer)
		{
			d_comparer = comparer;
		}
		
		public new int BinarySearch(T item)
		{
			return base.BinarySearch(item, d_comparer);
		}
		
		public int BinarySearch(int idx, int cnt, T item)
		{
			return base.BinarySearch(idx, cnt, item, d_comparer);
		}
		
		public T Find(T item)
		{
			int i = BinarySearch(item, d_comparer);
			
			if (i >= 0)
			{
				return this[i];
			}
			else
			{
				return default(T);
			}
		}
		
		public new void Add(T item)
		{
			int i = BinarySearch(item, d_comparer);
			
			if (i >= 0)
			{
				base.Insert(i, item);
			}
			else
			{
				base.Insert(~i, item);
			}
		}
		
		public new void Remove(T item)
		{
			int i = BinarySearch(item, d_comparer);
			
			if (i >= 0)
			{
				RemoveAt(i);
			}
		}
	}
}


