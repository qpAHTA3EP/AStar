using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                || StartNode.Position.IsOrigin
                || EndNode == null
                || EndNode.Position.IsOrigin)
                return false;
#if DEBUG || DEBUG_LOG
            StringBuilder sb = new StringBuilder();
            Stopwatch sw = new Stopwatch();
#endif
            {
                // используем волновой поиск
                if (waveSearcher is null)
                    waveSearcher = new WaveSearch(graph);
                else waveSearcher.Rebase(graph);
                searcher = waveSearcher;
                try
                {
#if DEBUG_LOG
                    AStarLogger.WriteLine(LogType.Log, $"WaveSearch: Start pathfinding Start{StartNode} ==> End{EndNode}");
                    AStarLogger.WriteLine(LogType.Log, $"QuesterProfile: {Astral.API.CurrentSettings.LastQuesterProfile}");
                    sw.Start();
#if PRINT_GRAPH
                    sb.AppendLine("Graph nodes:");
                    foreach (Node n in graph.Nodes)
                        sb.AppendLine($"\t{n}\t|\t{n.WaveWeight}");
                    AStarLogger.WriteLine(LogType.Log, sb.ToString()); 
#endif
#elif DEBUG
                    sb.AppendLine($"WaveSearch: Start pathfinding Start{StartNode} ==> End{EndNode}");
                    sb.AppendLine($"QuesterProfile: {Astral.API.CurrentSettings.LastQuesterProfile}");
                    sw.Start(); 
#endif
                    if (searcher.SearchPath(StartNode, EndNode))
                    {
#if DEBUG_LOG
                        sw.Stop();
                        AStarLogger.WriteLine(LogType.Log, $"WaveSearch SUCCEEDED: Start{StartNode} ==> End{EndNode}");
                        AStarLogger.WriteLine(LogType.Log, $"{nameof(WaveSearch)}: Elapsed time: {sw.ElapsedMilliseconds:N3} ({sw.ElapsedTicks})");
                        AStarLogger.WriteLine(LogType.Log, $"\tNodes in path: {searcher.PathByNodes?.Length}; Length: {searcher.PathLength:N2}");
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
#if DEBUG_LOG
                    else
                    {
                        sw.Stop();
                        AStarLogger.WriteLine(LogType.Debug, $"WaveSearch FAILED: Start{StartNode} ==> End{EndNode}", true);
                        AStarLogger.WriteLine(LogType.Debug, $"{nameof(WaveSearch)}: Elapsed time: {sw.ElapsedMilliseconds:N3} ({sw.ElapsedTicks})");
                        AStarLogger.WriteLine(LogType.Debug, $"QuesterProfile: {Astral.API.CurrentSettings.LastQuesterProfile}", true);
#if PRINT_GRAPH
                        sb.Clear();
                        sb.AppendLine("Graph nodes:");
                        foreach (Node n in graph.Nodes)
                            sb.AppendLine($"\t{n}\t|\t{n.WaveWeight}");
                        AStarLogger.WriteLine(LogType.Debug, sb.ToString()); 
#endif
                    }
#elif DEBUG
                    sw.Stop();
                    sb.AppendLine($"WaveSearch FAILED");
                    sb.AppendLine($"WaveSearch: Elapsed time: {sw.ElapsedMilliseconds:N3} ({sw.ElapsedTicks})");
#endif
                }
                catch (Exception e)
                {
#if DEBUG_LOG
                    sw.Stop();
                    AStarLogger.WriteLine(LogType.Error, $"WaveSearch EXCEPTION: Start{StartNode} ==> End{EndNode}", true);
                    AStarLogger.WriteLine(LogType.Error, $"{nameof(WaveSearch)}: Elapsed time: {sw.ElapsedMilliseconds:N3} ({sw.ElapsedTicks})");
                    AStarLogger.WriteLine(LogType.Error, $"QuesterProfile: {Astral.API.CurrentSettings.LastQuesterProfile}", true);
                    AStarLogger.WriteLine(LogType.Error, e.Message, true);
                    AStarLogger.WriteLine(LogType.Error, e.StackTrace, true);
                    Exception innExc = e.InnerException;
                    while (innExc != null)
                    {
                        AStarLogger.WriteLine(LogType.Error, innExc.Message, true);
                        innExc = innExc.InnerException;
                    }
#if PRINT_GRAPH
                    sb.AppendLine("Graph nodes:");
                    foreach (Node n in graph.Nodes)
                        sb.AppendLine($"\t{n}\t|\t{n.WaveWeight}");
                    AStarLogger.WriteLine(LogType.Log, sb.ToString()); 
#endif
#elif DEBUG
                    sw.Stop();
                    sb.AppendLine($"WaveSearch EXCEPTION: {e.Message}");
                    sb.AppendLine($"{nameof(WaveSearch)}: Elapsed time: {sw.ElapsedMilliseconds:N3} ({sw.ElapsedTicks})");
                    sb.AppendLine(e.StackTrace);
                    Exception innExc = e.InnerException;
                    while (innExc != null)
                    {
                        sb.AppendLine($"\t{innExc.Message}");
                        innExc = innExc.InnerException;
                    }
                    sb.AppendLine();
#if PRINT_GRAPH
                    sb.AppendLine("Graph nodes:");
                    foreach (Node n in graph.Nodes)
                        sb.AppendLine($"\t{n}\t|\t{n.WaveWeight}");
                    AStarLogger.WriteLine(LogType.Log, sb.ToString()); 
#endif
#endif
                }
            }

            // используем AStar
#if DEBUG_LOG
            AStarLogger.WriteLine(LogType.Debug, $"AStar: Start pathfinding Start{StartNode} ==> End{EndNode}");
            AStarLogger.WriteLine(LogType.Debug, $"QuesterProfile: {Astral.API.CurrentSettings.LastQuesterProfile}");
            sw.Restart();
#elif DEBUG
            sb.AppendLine($"AStar: Start pathfinding Start{StartNode} ==> End{EndNode}");
            sb.AppendLine($"QuesterProfile: {Astral.API.CurrentSettings.LastQuesterProfile}");
            sw.Restart();
#endif
            if (aStarSearcher is null)
                aStarSearcher = new AStarSearch(graph);
            else aStarSearcher.Rebase(graph);
            searcher = aStarSearcher;
            bool result = searcher.SearchPath(StartNode, EndNode)
                          && searcher.PathFound;
#if DEBUG_LOG
            sw.Stop();
            if (result)
            {
                AStarLogger.WriteLine(LogType.Debug, $"AStar SUCCEEDED: Start{StartNode} ==> End{EndNode}");
                AStarLogger.WriteLine(LogType.Debug, $"AStar: Elapsed time: {sw.ElapsedMilliseconds:N3} ({sw.ElapsedTicks})");
                AStarLogger.WriteLine(LogType.Debug, $"\tNodes in path: {searcher.PathByNodes.Length}; Length: {searcher.PathLength:N2}");
            }
            else
            {
                AStarLogger.WriteLine(LogType.Debug, $"WaveSearch FAILED: Start{StartNode} ==> End{EndNode}");
                AStarLogger.WriteLine(LogType.Debug, $"AStar: Elapsed time: {sw.ElapsedMilliseconds:N3} ({sw.ElapsedTicks})");
            }
#elif DEBUG
            sw.Stop();
            if(result)
            {
                sb.AppendLine($"AStar SUCCEEDED: Start{StartNode} ==> End{EndNode}");
                sb.AppendLine($"AStar: Elapsed time: {sw.ElapsedMilliseconds:N3} ({sw.ElapsedTicks})");
                sb.AppendLine($"\tNodes in path: {searcher.PathByNodes.Length}; Length: {searcher.PathLength:N2}");
            }
            else
            {
                sb.AppendLine($"AStar: FAILED");
                sb.AppendLine($"AStar: Elapsed time: {sw.ElapsedMilliseconds:N3} ({sw.ElapsedTicks})");
            }

            AStarLogger.WriteLine(LogType.Error, sb.ToString());
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
                    && searcher.PathFound)
                {
                    return searcher.PathByNodes;
                }
                return null;
            }
        }

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
