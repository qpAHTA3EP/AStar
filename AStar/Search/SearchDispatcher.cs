using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using AStar.Search;
using AStar.Search.AStar;
using AStar.Search.Wave;
using MyNW.Classes;

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

#if DEBUG || DEBUG_LOG
        StringBuilder sb = new StringBuilder();
        Stopwatch sw = new Stopwatch();
#endif

        /// <summary>
        /// Поиск пути из точки с координатами <paramref name="x1"/>, <paramref name="y1"/>, <paramref name="z1"/> в точку с координатами <paramref name="x2"/>, <paramref name="y2"/>, <paramref name="z2"/>
        /// </summary>
        public bool SearchPath(double x1, double y1, double z1,
            double x2, double y2, double z2)
        {
            if (graph != null
                && Math.Abs(x1 - x2) > 1 && Math.Abs(y1 - y2) > 1 && Math.Abs(z1 - z2) > 1
                && !(x1 == 0 && y1 == 0 && z1 == 0)
                && !(x2 == 0 && y2 == 0 && z2 == 0)
                && Point3D.SquaredDistanceBetween(x1, y1, z1, x2, y2, z2) < 25)
            {
#if DEBUG || DEBUG_LOG
                sb.Clear();
                sw.Start();
#endif
                graph.ClosestNodes(x1, y1, z1, out double dist1, out Node startNode,
                    x2, y2, z2, out double dist2, out Node endNode);
#if DEBUG || DEBUG_LOG
                sw.Stop();

                sb.Append("Start position {").Append(x1).Append(';').Append(y1).Append(';').Append(z1).Append("} => StartNode ")
                    .Append(startNode).Append(" at distance ").AppendLine(dist1.ToString());
                sb.Append("End position {").Append(x2).Append(';').Append(y2).Append(';').Append(z2).Append("} => EndNode ")
                    .Append(endNode).Append(" at distance ").AppendLine(dist1.ToString());
#endif
                bool result = SearchPath(startNode, endNode);
#if DEBUG || DEBUG_LOG
                sb.Append("Used algorithm: ").Append(searcher?.GetType().Name).Append(" result is ").AppendLine(result ? "SUCCEEDED" : "FAILED");

#endif
                return result;
            }
            searcher?.Reset();
            return false;
        }

        /// <summary>
        /// Поиск пути от вершины <paramref name="startNode"/> к вершине <paramref name="endNode"/>
        /// </summary>
        public bool SearchPath(Node startNode, Node endNode)
        {
            if (startNode is null
                || startNode.Position.IsOrigin
                || endNode is null
                || endNode.Position.IsOrigin
                || Point3D.SquaredDistanceBetween(startNode.Position, endNode.Position) < 25)
            {
                searcher?.Reset();
                return false;
            }
#if false && (DEBUG || DEBUG_LOG)
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
                    AStarLogger.WriteLine(LogType.Log, $"WaveSearch: Start pathfinding Start{startNode} ==> End{endNode}");
                    AStarLogger.WriteLine(LogType.Log, $"QuesterProfile: {Astral.API.CurrentSettings.LastQuesterProfile}");
                    sw.Start();
#if PRINT_GRAPH
                    sb.AppendLine("Graph nodes:");
                    foreach (Node n in graph.Nodes)
                        sb.AppendLine($"\t{n}\t|\t{n.WaveWeight}");
                    AStarLogger.WriteLine(LogType.Log, sb.ToString()); 
#endif
#elif DEBUG
                    sb.AppendLine($"WaveSearch: Start pathfinding Start{startNode} ==> End{endNode}");
                    sb.AppendLine($"QuesterProfile: {Astral.API.CurrentSettings.LastQuesterProfile}");
                    sw.Start();
#endif
                    if (searcher.SearchPath(startNode, endNode))
                    {
#if DEBUG_LOG
                        sw.Stop();
                        AStarLogger.WriteLine(LogType.Log, $"WaveSearch SUCCEEDED: Start{startNode} ==> End{endNode}");
                        AStarLogger.WriteLine(LogType.Log, $"\tCurrentWaveSlotIndex: {waveSearcher.CurrentWaveSlotIndex}");
                        AStarLogger.WriteLine(LogType.Log, $"\tElapsed time: {sw.ElapsedMilliseconds} ({sw.ElapsedTicks})");
                        AStarLogger.WriteLine(LogType.Log, $"\t\tNodes in path: {searcher.PathNodeCount}; Length: {searcher.PathLength:N2}");
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
                        AStarLogger.WriteLine(LogType.Debug, $"WaveSearch FAILED: Start{startNode} ==> End{endNode}", true);
                        AStarLogger.WriteLine(LogType.Debug, $"{nameof(WaveSearch)}: Elapsed time: {sw.ElapsedMilliseconds} ({sw.ElapsedTicks})");
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
                catch (ThreadInterruptedException e)
                {
                    AStarLogger.WriteLine(LogType.Error, $"{nameof(AStar)}.{nameof(SearchPath)}: Перехвачено исключение '{e}'", true);
                    throw;
                }
                catch (ThreadAbortException e)
                {
                    AStarLogger.WriteLine(LogType.Error, $"{nameof(AStar)}.{nameof(SearchPath)}: Перехвачено исключение '{e}'", true);
                    throw;
                }
#if DEBUG_LOG
                catch (Exception e)
                {
                    sw.Stop();
                    AStarLogger.WriteLine(LogType.Error, $"WaveSearch EXCEPTION: Start{startNode} ==> End{endNode}", true);
                    AStarLogger.WriteLine(LogType.Error, $"{nameof(WaveSearch)}: Elapsed time: {sw.ElapsedMilliseconds} ({sw.ElapsedTicks})");
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
                catch (Exception e)
                {
                    sw.Stop();
                    sb.AppendLine($"WaveSearch EXCEPTION: {e}");
                    sb.AppendLine($"{nameof(WaveSearch)}: Elapsed time: {sw.ElapsedMilliseconds:N3} ({sw.ElapsedTicks})");
#if false
                    sb.AppendLine(e.StackTrace);
                    Exception innExc = e.InnerException;
                    while (innExc != null)
                    {
                        sb.AppendLine($"\t{innExc.Message}");
                        innExc = innExc.InnerException;
                    } 
#endif
                    sb.AppendLine();
#if PRINT_GRAPH
                    sb.AppendLine("Graph nodes:");
                    foreach (Node n in graph.Nodes)
                        sb.AppendLine($"\t{n}\t|\t{n.WaveWeight}");
                    AStarLogger.WriteLine(LogType.Log, sb.ToString()); 
#endif
#else
                catch
                {
#endif
                }
            }

            // используем AStar
#if DEBUG_LOG
            AStarLogger.WriteLine(LogType.Debug, $"AStar: Start pathfinding Start{startNode} ==> End{endNode}");
            AStarLogger.WriteLine(LogType.Debug, $"QuesterProfile: {Astral.API.CurrentSettings.LastQuesterProfile}");
            sw.Restart();
#elif DEBUG
            sb.AppendLine($"AStar: Start pathfinding Start{startNode} ==> End{endNode}");
            sb.AppendLine($"QuesterProfile: {Astral.API.CurrentSettings.LastQuesterProfile}");
            sw.Restart();
#endif
            if (aStarSearcher is null)
                aStarSearcher = new AStarSearch(graph);
            else aStarSearcher.Rebase(graph);
            searcher = aStarSearcher;
            bool result = searcher.SearchPath(startNode, endNode)
                          && searcher.PathFound;
#if DEBUG_LOG
            sw.Stop();
            if (result)
            {
                AStarLogger.WriteLine(LogType.Debug, $"AStar SUCCEEDED: Start{startNode} ==> End{endNode}");
                AStarLogger.WriteLine(LogType.Debug, $"AStar: Elapsed time: {sw.ElapsedMilliseconds} ({sw.ElapsedTicks})");
                AStarLogger.WriteLine(LogType.Debug, $"\tNodes in path: {searcher.PathNodeCount}; Length: {searcher.PathLength:N2}");
            }
            else
            {
                AStarLogger.WriteLine(LogType.Debug, $"WaveSearch FAILED: Start{startNode} ==> End{endNode}");
                AStarLogger.WriteLine(LogType.Debug, $"AStar: Elapsed time: {sw.ElapsedMilliseconds} ({sw.ElapsedTicks})");
            }
#elif DEBUG
            sw.Stop();
            if(result)
            {
                sb.AppendLine($"AStar SUCCEEDED: Start{startNode} ==> End{endNode}");
                sb.AppendLine($"AStar: Elapsed time: {sw.ElapsedMilliseconds:N3} ({sw.ElapsedTicks})");
                sb.AppendLine($"\tNodes in path: {searcher.PathByNodes.Length}; Length: {searcher.PathLength:N2}");
            }
            else
            {
                sb.AppendLine($"AStar: FAILED");
                sb.AppendLine($"AStar: Elapsed time: {sw.ElapsedMilliseconds:N3} ({sw.ElapsedTicks})");
            }

            AStarLogger.WriteLine(LogType.Error, sb.ToString());
            sw.Reset();
            sb.Clear();
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
                if (PathFound)
                    return searcher.PathByNodes;
                return new Node[0];
            }
        }

        public IEnumerable<Node> PathNodes
        {
            get
            {
                if (PathFound)
                    return searcher.PathNodes;
                return Enumerable.Empty<Node>();
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
