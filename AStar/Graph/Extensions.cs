using System.Collections;
using System.Linq;

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
        /// <summary>
        /// Поиск индекса элемента <paramref name="item"/> среди элементов списка <paramref name="list"/>,
        /// ограниченного диапазоном [<paramref name="startInd"/>, <paramref name="endInd"/>)
        /// </summary>
        public static int IndexInRange<T>(this IList list, T item, int startInd, int endInd)
        {
            int count = list.Count;
            if (startInd >= 0 && startInd < count
                && endInd > startInd && endInd <= count)
            {
                for (int i = startInd; i < endInd; i++)
                    if (item.Equals(list[i]))
                        return i;
            }
            return -1;
        }
    }
}