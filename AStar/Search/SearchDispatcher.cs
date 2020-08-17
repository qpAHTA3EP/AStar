using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AStar.Search;
using AStar.Search.AStar;
using AStar.Search.Wave;

namespace AStar
{
    // Класс-заглушка, выбирающая алгоритм поиска
    public class AStar
    {
        public AStar(Graph g)
        {
            if (g != graph)
            {
                graph = g;
                searcher?.Rebase(graph);
                waveSearcher?.Rebase(graph);
            }
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

#if false
            WaveWeight startWeight = StartNode.WaveWeight;
            WaveWeight endWeight = EndNode.WaveWeight;
            if (startWeight.Target != null
                && Equals(startWeight.Target, EndNode)
                && endWeight.Target != null
                && Equals(endWeight.Target, EndNode))
            {

                //if (startWeight.Arc != null && startWeight.Weight > 0 && startWeight.Arc != null)
                {
                    // вершины имеют волновую оценку для StartNode
                    // используем волновой поиск
                    if (waveSearcher is null)
                        waveSearcher = new WaveSearch(graph);
                    searcher = waveSearcher;
                    searcher.SearchPath(StartNode, EndNode);
                    if (searcher.SearchEnded && searcher.PathFound)
                        return true;
                }
            } 
            // вершины не имеют волновой оценки для EndNode
#endif

#if disabled_20200723_1220
            double distance = Node.EuclidianDistance(StartNode, EndNode);
            if (distance > 90) 
#endif
            { // используем волновой поиск
                if (waveSearcher is null)
                    waveSearcher = new WaveSearch(graph);
                searcher = waveSearcher;
                try
                {
                    searcher.SearchPath(StartNode, EndNode);
                    if (searcher.SearchEnded && searcher.PathFound)
                        return true;
                }
                catch  { }
            }

            // используем AStar
            if(aStarSearcher is null)
                aStarSearcher = new AStarSearch(graph);
            searcher = aStarSearcher;
            searcher.SearchPath(StartNode, EndNode);
            return searcher.SearchEnded && searcher.PathFound;
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

        private static Graph graph = null;
        private static SearchPathBase searcher = null;
        private static WaveSearch waveSearcher = null;
        private static AStarSearch aStarSearcher = null;
    }
}
