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

#if disabled_20200723_1220
            double distance = Node.EuclidianDistance(StartNode, EndNode);
            if (distance > 90) 
#endif
            {
                // используем волновой поиск
                if (waveSearcher is null)
                    waveSearcher = new WaveSearch(graph);
                else waveSearcher.Rebase(graph);
                searcher = waveSearcher;
                try
                {
                    searcher.SearchPath(StartNode, EndNode);
                    if (searcher.SearchEnded && searcher.PathFound)
                    {
#if DEBUG || DEBUG_LOG
                        AStarLogger.WriteLine(LogType.Log, $"WaveSearch SUCCEEDED: End{EndNode} <== Start{StartNode}\n\r"+
                                                           $"Nodes in path: {searcher.PathByNodes.Length}; Length: {searcher.PathLength:N2}");
                        AStarLogger.WriteLine(LogType.Log, $"QuesterProfile: {Astral.API.CurrentSettings.LastQuesterProfile}"); 
#endif
                        return true;
                    }
#if DEBUG || DEBUG_LOG
                    else
                    {
                        AStarLogger.WriteLine(LogType.Log, $"WaveSearch FAILED: End{EndNode} <== Start{StartNode}");
                        AStarLogger.WriteLine(LogType.Log, $"QuesterProfile: {Astral.API.CurrentSettings.LastQuesterProfile}");
                    } 
#endif
                }
                catch (Exception e)
                {
                    AStarLogger.WriteLine(LogType.Error, $"WaveSearch EXCEPTION: End{EndNode} <== Start{StartNode}", true);
                    AStarLogger.WriteLine(LogType.Error, $"QuesterProfile: {Astral.API.CurrentSettings.LastQuesterProfile}", true);
                    AStarLogger.WriteLine(LogType.Error, e.Message, true);
                    AStarLogger.WriteLine(LogType.Error, e.StackTrace, true);
                    Exception innExc = e.InnerException;
                    while (innExc != null)
                    {
                        AStarLogger.WriteLine(LogType.Error, innExc.Message, true);
                        innExc = innExc.InnerException;
                    }
                }
            }

            // используем AStar
            if(aStarSearcher is null)
                aStarSearcher = new AStarSearch(graph);
            else aStarSearcher.Rebase(graph);
            searcher = aStarSearcher;
            searcher.SearchPath(StartNode, EndNode);
            bool result = searcher.SearchEnded && searcher.PathFound;
#if DEBUG || DEBUG_LOG
            if (result)
            {
                AStarLogger.WriteLine(LogType.Log, $"AStar SUCCEEDED: End{EndNode} <== Start{StartNode}\n\r"+
                                        $"Nodes in path: {searcher.PathByNodes.Length}; Length: {searcher.PathLength:N2}");
                AStarLogger.WriteLine(LogType.Log, $"QuesterProfile: {Astral.API.CurrentSettings.LastQuesterProfile}");
            }
            else
            {
                AStarLogger.WriteLine(LogType.Log, $"WaveSearch FAILED: End{EndNode} <== Start{StartNode}");
                AStarLogger.WriteLine(LogType.Log, $"QuesterProfile: {Astral.API.CurrentSettings.LastQuesterProfile}");
            } 
#endif

            return result;
        }

        /// <summary>
        /// Список узлов, определяющих найденный путь
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
        /// Флаг, указывающий на завершение поиска
        /// </summary>
        public bool SearchEnded => searcher?.SearchEnded == true;

        /// <summary>
        /// Флаг, указывающие на успешное построение пути
        /// </summary>
        public bool PathFound => searcher?.PathFound == true;

        private static Graph graph = null;
        private static SearchPathBase searcher = null;
        private static WaveSearch waveSearcher = null;
        private static AStarSearch aStarSearcher = null;
    }
}
