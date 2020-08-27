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
            if (StartNode == null
                || !StartNode.Position.IsOrigin
                || EndNode == null
                || !EndNode.Position.IsOrigin)
                return false;
#if DEBUG_LOG
            StringBuilder sb = new StringBuilder();
#endif
#if disabled_20200723_1220
            double distance = Node.EuclideanDistance(StartNode, EndNode);
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
#if DEBUG || DEBUG_LOG
                    AStarLogger.WriteLine(LogType.Log, $"WaveSearch: Start pathfinding End{EndNode} <== Start{StartNode}");
                    AStarLogger.WriteLine(LogType.Log, $"QuesterProfile: {Astral.API.CurrentSettings.LastQuesterProfile}");
#if PRINT_GRAPH
                    sb.AppendLine("Graph nodes:");
                    foreach (Node n in graph.Nodes)
                        sb.AppendLine($"\t{n}\t|\t{n.WaveWeight}");
                    AStarLogger.WriteLine(LogType.Log, sb.ToString()); 
#endif
#endif
                    searcher.SearchPath(StartNode, EndNode);
                    if (searcher.SearchEnded && searcher.PathFound)
                    {
#if DEBUG || DEBUG_LOG
                        AStarLogger.WriteLine(LogType.Log, $"WaveSearch SUCCEEDED: End{EndNode} <== Start{StartNode}");
                        AStarLogger.WriteLine(LogType.Log, $"\tNodes in path: in path: {searcher.PathByNodes.Length}; Length: {searcher.PathLength:N2}");
#if PRINT_GRAPH
                        sb.Clear();
                        sb.AppendLine("Graph nodes:");
                        foreach (Node n in graph.Nodes)
                            sb.AppendLine($"\t{n}\t|\t{n.WaveWeight}");
                        AStarLogger.WriteLine(LogType.Log, sb.ToString()); 
#endif
#endif
                        return true;
                    }
#if DEBUG || DEBUG_LOG
                    else
                    {
                        AStarLogger.WriteLine(LogType.Log, $"WaveSearch FAILED: End{EndNode} <== Start{StartNode}");
                        //AStarLogger.WriteLine(LogType.Log, $"QuesterProfile: {Astral.API.CurrentSettings.LastQuesterProfile}");
#if PRINT_GRAPH
                        sb.Clear();
                        sb.AppendLine("Graph nodes:");
                        foreach (Node n in graph.Nodes)
                            sb.AppendLine($"\t{n}\t|\t{n.WaveWeight}");
                        AStarLogger.WriteLine(LogType.Log, sb.ToString()); 
#endif
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
#if DEBUG_LOG

                    sb.Clear();
                    sb.AppendLine("Graph nodes:");
                    foreach (Node n in graph.Nodes)
                        sb.AppendLine($"\t{n}\t|\t{n.WaveWeight}");
                    AStarLogger.WriteLine(LogType.Log, sb.ToString()); 
#endif
                }
            }

            // используем AStar
#if DEBUG || DEBUG_LOG
            AStarLogger.WriteLine(LogType.Log, $"AStar: Start pathfinding End{EndNode} <== Start{StartNode}");
            AStarLogger.WriteLine(LogType.Log, $"QuesterProfile: {Astral.API.CurrentSettings.LastQuesterProfile}");
#endif
            if (aStarSearcher is null)
                aStarSearcher = new AStarSearch(graph);
            else aStarSearcher.Rebase(graph);
            searcher = aStarSearcher;
            searcher.SearchPath(StartNode, EndNode);
            bool result = searcher.SearchEnded && searcher.PathFound;
#if DEBUG || DEBUG_LOG
            if (result)
            {
                AStarLogger.WriteLine(LogType.Log, $"AStar SUCCEEDED: End{EndNode} <== Start{StartNode}");
                AStarLogger.WriteLine(LogType.Log, $"\tNodes in path: {searcher.PathByNodes.Length}; Length: {searcher.PathLength:N2}");
            }
            else
            {
                AStarLogger.WriteLine(LogType.Log, $"WaveSearch FAILED: End{EndNode} <== Start{StartNode}");
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
