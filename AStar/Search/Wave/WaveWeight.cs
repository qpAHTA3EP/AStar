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

        public bool IsTarget(Node tar)
        {
            return /*Target != null  && Arc != null && */ Equals(tar, Target);
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
}
