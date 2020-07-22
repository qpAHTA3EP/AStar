using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AStar
{
    // Класс-заглушка, выбирающая алгоритм поиска
    public class AStar
    {
        public AStar(Graph g)
        {
            graph = g;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="StartNode"></param>
        /// <param name="EndNode"></param>
        /// <returns></returns>
        public bool SearchPath(Node StartNode, Node EndNode)
        {
            if (StartNode == null || EndNode == null)
                return false;

            if (StartNode.Tags.ContainsKey(EndNode.Position)
                && EndNode.Tags.ContainsKey(EndNode.Position))
            {
                if (StartNode.Tags[EndNode.Position] is WaveWeight startWeight
                    && startWeight.Arc != null && startWeight.Weight > 0)
                {
                    // вершины имеют волновую оценку для StartNode
                    // используем волновой поиск
                    searcher = new WaveSearch(graph);
                    searcher.SearchPath(StartNode, EndNode);
                    return PathFound;
                }
            }

            // вершины не имеют волновой оценки для EndNode
            double distance = Node.EuclidianDistance(StartNode, EndNode);
            if(distance > 90)
                // используем волновой поиск
                searcher = new WaveSearch(graph);
            else
                // используем AStar
                searcher = new AStarSearch(graph);

            searcher.SearchPath(StartNode, EndNode);
            return PathFound;
        }

        /// <summary>
        /// Список узлов, определяющих найденный уть
        /// </summary>
        public Node[] PathByNodes
        {
            get
            {
                if (searcher != null
                    && searcher.SearchEnded && searcher.PathFound)
                {
                    return searcher.PathByNodes;
                }
                return null;
            }
        }

        /// <summary>
        /// Флаг, указыающий на завершение поиска
        /// </summary>
        public bool SearchEnded
        {
            get
            {
                return searcher?.SearchEnded == true;
            }
        }

        /// <summary>
        /// Флаг, указывающие на успешное построение пути
        /// </summary>
        public bool PathFound
        {
            get
            {
                return searcher?.PathFound == true;
            }
        }

        private SearchPathBase searcher = null;
        private readonly Graph graph = null;
        //private int targetHash = 0;
    }
}
