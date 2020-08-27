using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AStar.Search.Wave
{
    /// <summary>
    /// Волновой вес вершины
    /// </summary>
    public class WaveWeight //: IComparable<WaveWeight>
    {
        public WaveWeight() { }
        public WaveWeight(Node tar, double w, Arc arc)
        {
            Target = tar;
            Weight = w;
            Arc = arc;
        }

        public double Weight = double.MaxValue;
        public Arc Arc { get; set; } = null;

        /// <summary>
        /// Конечная вершина, к которой стремится путь (источник волны)
        /// </summary>
        public Node Target { get; }

        public bool IsValid => Target != null /* && Arc != null */;

        public bool IsTargetTo(Node tar)
        {
            return /*Target != null  && Arc != null  &&*/ Target?.Equals(tar) == true;
        }

        public override string ToString()
        {
            if (Target is null)
                return "INVALID";
            if (Arc is null)
                return $"{Weight}:\t{Target}";
            return $"{Weight:N2}:\t{Target} <== {Arc.EndNode} <-- {Arc.StartNode}";
        }
    }

    /// <summary>
    /// Функтор сравнения вершин по их волновому весу
    /// </summary>
    public class NodesWeightComparer : IComparer<Node>
    {
        /// <summary>
        /// Метод сравнение вершин по их волновому весу
        /// </summary>
        /// <param name="n1"></param>
        /// <param name="n2"></param>
        /// <returns></returns>
        public static int CompareNodesWeight(Node n1, Node n2)
        {
            if (n1 is null)
                if (n2 is null)
                    return 0;
                else return +1;
            else if (n2 is null)
                return -1;

            WaveWeight ww1 = n1.WaveWeight;
            WaveWeight ww2 = n2.WaveWeight;
            if (ww1 is null)
            {
                if (ww2 is null)
                    return 0;
                else return 1;
            }
            else if (ww2 is null)
                return -1;
            if (n1.WaveWeight.Target != null && Equals(n1.WaveWeight.Target, n2.WaveWeight.Target))
            {
                double w1 = n1.WaveWeight.Weight;
                double w2 = n2.WaveWeight.Weight;

                if (w1 < w2)
                    return -1;
                else if (w1 > w2)
                    return +1;
                //return Convert.ToInt32(w1 - w2);
            }
            return 0;
        }

        public int Compare(Node n1, Node n2)
        {
            return CompareNodesWeight(n1, n2);
        }
    }
}
