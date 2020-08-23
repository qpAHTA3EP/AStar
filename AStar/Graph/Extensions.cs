using System.Collections;

namespace AStar
{
    public static class IListExtensions
    {
        public static void AddUnique<T>(this IList list, T item)
        {
            if (!list.Contains(item))
                list.Add(item);
        }
    }
}