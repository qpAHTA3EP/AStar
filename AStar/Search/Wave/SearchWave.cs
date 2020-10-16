using MyNW.Classes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using static AStar.Search.Wave.WaveSource;

namespace AStar.Search.Wave
{
    /// <summary>
    /// Волновой поиск путей в графе
    /// </summary>
    public class WaveSearch : SearchPathBase
    {
        Graph graph;

        public WaveSearch(Graph G)
        {
            graph = G;
        }

        /// <summary>
        /// Привязка к новому графу
        /// </summary>
        /// <param name="g"></param>
        public override void Rebase(Graph g)
        {

            if (g != null && graph != g)
            {
                graph = g;
                ResetFlags();
            }
        }

        public void ResetFlags()
        {
            foundedPath = null;
            foundedPathLength = -1;
            pathFound = false;
        }

        /// <summary>
        /// Поиск пути из точки с координатами <paramref name="x1"/>, <paramref name="y1"/>, <paramref name="z1"/> в точку с координатами <paramref name="x2"/>, <paramref name="y2"/>, <paramref name="z2"/>
        /// </summary>
        public bool SearchPath(double x1, double y1, double z1,
                               double x2, double y2, double z2)
        {
            if(graph != null)
            {
                graph.ClosestNodes(x1, y1, z1, out double dist1, out Node startNode,
                    x2, y2, z2, out double dist2, out Node endtNode);
                return SearchPath(startNode, endtNode);
            }
            return false;
        }

        /// <summary>
        /// Поиск пути от узла StartNode к узлу EndNode
        /// </summary>
        /// <param name="StartNode"></param>
        /// <param name="EndNode"></param>
        /// <returns></returns>
        public override bool SearchPath(Node StartNode, Node EndNode)
        {
            ResetFlags();

            if (StartNode is null || EndNode is null
                || !StartNode.Passable || !EndNode.Passable
                || StartNode.Position.IsOrigin || EndNode.Position.IsOrigin)
            {
#if WAVESEARCH_DEBUG_LOG
                AStarLogger.WriteLine(LogType.Debug, $"{nameof(WaveSearch)}.{nameof(SearchPath)}: Некорректные входные данные. Прерываем поиск");
#endif
                return false;
            }
            if (waveSource is null)
                waveSource = new WaveSource();

#if false
            lock (graph.SyncRoot) 
#else
            using (graph.ReadLock())
#endif
            {
                LinkedList<Node> track = null;

                waveSource.AttachTo(graph, EndNode);
                //waveSource.Target = EndNode;

                // Проверяем наличие "кэша" волнового поиска для EndNode
                WaveWeight startWW = StartNode.WaveWeight;
                //WaveWeight endWW = EndNode.WaveWeight;
#if WaveSource_Validate
                if (waveSource.Validate(startWW))
#else
                if (startWW?.IsTargetTo(EndNode) == true)
#endif
                {
#if WAVESEARCH_DEBUG_LOG
                AStarLogger.WriteLine(LogType.Debug, $"{nameof(WaveSearch)}.{nameof(SearchPath)}: Найден кэш волны #{waveSource.CurrentSlotIndex} из End{EndNode}");
#endif
                    // найден кэш волнового поиска для EndNode
                    // Пытаемся построить путь
                    // формируем путь
                    try
                    {

                        if (GoBackUpNodes(StartNode, EndNode, out track))
                        {
                            // путь найден
#if WAVESEARCH_DEBUG_LOG
                            AStarLogger.WriteLine(LogType.Debug, $"{nameof(WaveSearch)}.{nameof(SearchPath)}: В кэше #{waveSource.CurrentSlotIndex} найден путь: Start{StartNode} ==> End{EndNode}");
#endif
                            pathFound = true;
                            foundedPath = new Node[track.Count];
                            foundedPathLength = -1;

                            track.CopyTo(foundedPath, 0);

                            waveSource.IncreaseUsage();
                            return true;
                        }

                        // Построить путь не удалось
#if WAVESEARCH_DEBUG_LOG
                        AStarLogger.WriteLine(LogType.Debug, $"{nameof(WaveSearch)}.{nameof(SearchPath)}: Путь в кэше не найден (или некорректен). Стираем волну");
#endif
                        waveSource.ClearWave();
                    }
                    catch (ThreadInterruptedException e)
                    {
                        AStarLogger.WriteLine(LogType.Error, $"{nameof(WaveSearch)}.{nameof(SearchPath)}: Перехвачено исключение '{e}'", true);
                        throw;
                    }
                    catch (ThreadAbortException e)
                    {
                        AStarLogger.WriteLine(LogType.Error, $"{nameof(WaveSearch)}.{nameof(SearchPath)}: Перехвачено исключение '{e}'", true);
                        throw;
                    }
#if DEBUG || WAVESEARCH_DEBUG_LOG
                    catch (Exception e)
                    {
                        AStarLogger.WriteLine(LogType.Error, $"{nameof(WaveSearch)}.{nameof(SearchPath)}: Перехвачено исключение '{e}'", true);
#if false
                        AStarLogger.WriteLine(LogType.Error, e.StackTrace, true);
                        Exception innExc = e.InnerException;
                        while (innExc != null)
                        {
                            AStarLogger.WriteLine(LogType.Error, innExc.Message, true);
                            innExc = innExc.InnerException;
                        } 
#endif
#else
                    catch 
                    {
#endif
                        waveSource.ClearWave();
                    }
                }

                // формируем путь
                try
                {
                    if (waveSource.GenerateWave(StartNode))
                    {
#if WAVESEARCH_DEBUG_LOG
                        AStarLogger.WriteLine(LogType.Log, $"{nameof(WaveSearch)}.{nameof(SearchPath)}: {nameof(WaveSource.GenerateWave)} завершилось успешно");
                        AStarLogger.WriteLine(LogType.Log, $"\tИспользован кэш #{waveSource.CurrentSlotIndex}");
#endif

                        startWW = StartNode.WaveWeight;
                        if (startWW?.IsTargetTo(EndNode) == true)
                        {
#if WAVESEARCH_DEBUG_LOG
                            AStarLogger.WriteLine(LogType.Log, $"{nameof(WaveSearch)}.{nameof(SearchPath)}: Формируем путь из Start{StartNode} ==> End{EndNode}");
#endif
                            if (GoBackUpNodes(StartNode, EndNode, out track))
                            {
#if WAVESEARCH_DEBUG_LOG
                                AStarLogger.WriteLine(LogType.Log, $"{nameof(WaveSearch)}.{nameof(SearchPath)}: Путь сформирован");
#endif
                                // путь найден
                                pathFound = true;
                                foundedPath = new Node[track.Count];
                                foundedPathLength = -1;
                                track.CopyTo(foundedPath, 0);
                                waveSource.IncreaseUsage();
                            }
                            else
                            {
#if WAVESEARCH_DEBUG_LOG
                                AStarLogger.WriteLine(LogType.Error, $"{nameof(WaveSearch)}.{nameof(SearchPath)}: Не удалось построить путь. Очищаем кэш волны #{waveSource.CurrentSlotIndex}");
#endif
#if true
                                waveSource.ClearWave();
#endif
                            }
                        }
#if WAVESEARCH_DEBUG_LOG
                        else AStarLogger.WriteLine(LogType.Log, $"{nameof(WaveSearch)}.{nameof(SearchPath)}: Вершина Start{StartNode} не имеет волновой оценки, соответствующей End{EndNode}");
#endif
                    }
#if WAVESEARCH_DEBUG_LOG
                    else
                    {
                        AStarLogger.WriteLine(LogType.Log, $"{nameof(WaveSearch)}.{nameof(SearchPath)}: {nameof(WaveSource.GenerateWave)} завершилось безрезультатно");
                    }
#endif
                }
                catch (ThreadInterruptedException e)
                {
                    AStarLogger.WriteLine(LogType.Error, $"{nameof(WaveSearch)}.{nameof(SearchPath)}: Перехвачено исключение '{e}'", true);
                    throw;
                }
                catch (ThreadAbortException e)
                {
                    AStarLogger.WriteLine(LogType.Error, $"{nameof(WaveSearch)}.{nameof(SearchPath)}: Перехвачено исключение '{e}'", true);
                    throw;
                }
#if DEBUG || WAVESEARCH_DEBUG_LOG
                catch (Exception e)
                {
                    AStarLogger.WriteLine(LogType.Error, $"{nameof(WaveSearch)}.{nameof(SearchPath)}: Перехвачено исключение '{e}'", true);
#if false
                    AStarLogger.WriteLine(LogType.Error, e.StackTrace, true);
                    Exception innExc = e.InnerException;
                    while (innExc != null)
                    {
                        AStarLogger.WriteLine(LogType.Error, innExc.Message, true);
                        innExc = innExc.InnerException;
                    } 
#endif
#else
                catch
                {
#endif
                    ResetFlags();
                    waveSource.ClearWave();
                }
            }
            return pathFound;
        }

        /// <summary>
        /// Список узлов, определяющих найденный путь
        /// </summary>
        public override Node[] PathByNodes
        {
            get
            {
                if (pathFound)
                    return foundedPath;
                return null;
            }
        }
        private Node[] foundedPath = null;


#if false
        /// <summary>
        /// Список узлов, определяющих найденный уть
        /// </summary>
        public override IEnumerable<Vector3> PathNodes
        {
            get
            {
                return null;
            }
        } 
#endif


        /// <summary>
        /// Длина найденного пути
        /// </summary>
        public override double PathLength
        {
            get
            {
                if (pathFound && foundedPath?.Length > 0)
                    if (foundedPathLength < 0)
                        return foundedPathLength = foundedPath[0].WaveWeight.Weight;
                    else return foundedPathLength;
                return 0;
            }
        }
        double foundedPathLength = -1;


#if GoBackUpNodes_Recursion
        /// <summary>
        /// Построение списка узлов, задающих найденный путь, рекурсивным методом
        /// </summary>
        /// <param name="endNode">конечный узел пути</param>
        /// <param name="node">текущий узел пути</param>
        /// <param name="track">сформированный путь</param>
        private static void GoBackUpNodes(Node endNode, Node node, ref LinkedList<Node> track)
        {

            if (Equals(node, endNode))
            {
                track.AddLast(node);
                return;
            }

            WaveWeight ww = node.WaveWeight;
            if (ww.IsTargetTo(endNode))
            {
                //if(!ww.Arc.EndNode.Passable)
                //{
                //    nodes.Clear();
                //    throw new InvalidDataException("Путь проходит через вершину, помеченную как Unpassable");
                //}
                //else if (!ww.Arc.Passable)
                //{
                //    nodes.Clear();
                //    throw new InvalidDataException("Путь проходит через ребро, помеченную как Unpassable");
                //}
                track.AddLast(node);
                if (ww.Arc != null)
                    GoBackUpNodes(endNode, ww.Arc.EndNode, ref track);
                return;
            }

            throw new ArgumentException($"Вершина {node} не имеет волновой оценки, позволяющей построить путь к End{endNode}");
        }
#else
        /// <summary>
        /// Построение списка узлов, задающих найденный путь
        /// </summary>
        private static bool GoBackUpNodes(Node startNode, Node endNode, out LinkedList<Node> track)
        {
            bool result = false;
            track = new LinkedList<Node>();
#if WAVESEARCH_DEBUG_LOG
            StringBuilder sb = new StringBuilder();
#endif
            Node currentNode = startNode;

            while (currentNode != null)
            {
                WaveWeight currentWW = currentNode.WaveWeight;
                if (currentWW?.IsTargetTo(endNode) == true)
                {
                    track.AddLast(currentNode);
                    if (Equals(currentNode, endNode))
                        break;
                    currentNode = currentWW.Arc?.EndNode;
                }
                else
                {
#if WAVESEARCH_DEBUG_LOG
                    string errorMsg = $"Вершина {currentNode} не имеет волновой оценки, позволяющей построить путь к End{endNode}";
                    sb.AppendLine(errorMsg);
#endif
                    break;
                }
            }
            result = track.Count > 0 && track.Last.Value.Equals(endNode);

#if WAVESEARCH_DEBUG_LOG
            if (track.Count > 0)
            {
                sb.Append($"{nameof(WaveSearch)}.{nameof(GoBackUpNodes)}: Track:\t");
                var current = track.First;
                var last = track.Last;
                while (current != last)
                {
                    sb.Append(current.Value).Append("-->");
                    current = current.Next;
                }

                sb.Append(last.Value);
            }
            else sb.Append($"{nameof(WaveSearch)}.{nameof(GoBackUpNodes)}: Track пуст");
            AStarLogger.WriteLine(result ? LogType.Debug : LogType.Error, sb.ToString(), false);
#endif
            return result;
        }
#endif

        /// <summary>
        /// Флаг, обозначающий, что путь найден
        /// </summary>
        public override bool PathFound => pathFound;//waveSource?.IsTargetTo(foundedPath?.LastOrDefault()) == true;
        private bool pathFound = false;

        private WaveSource waveSource = null;
    }
}
