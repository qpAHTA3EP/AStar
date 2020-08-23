using System.Collections;

namespace AStar
{
    public static class IListExtensions
    {
        public static int AddUnique<T>(this IList list, T item)
        {
            if (!list.Contains(item))
                return list.Add(item);
            return -1;
        }
    }
}