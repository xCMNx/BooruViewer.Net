using System.Collections.Generic;

namespace Booru.Core
{
	public class SortedItems<T> : List<T>
	{

		public bool Find(T item, out int index)
		{
			return (index = BinarySearch(item)) >= 0;
		}

		public IComparer<T> Comparer;

		public new int Add(T item)
		{
			int index = BinarySearch(item, Comparer);
			if (index < 0)
			{
				index = ~index;
				Insert(index, item);
			}
			return index;
		}

		public new void Remove(T item)
		{
			int index = BinarySearch(item, Comparer);
			if (index >= 0)
				RemoveAt(index);
		}

		public new int IndexOf(T item)
		{
			int index = BinarySearch(item, Comparer);
			return index < 0 ? -1 : ~index;
		}

		public bool Contain(T item)
		{
			return BinarySearch(item, Comparer) >= 0;
		}
	}
}
