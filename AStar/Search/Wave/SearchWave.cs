using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AStar.Search.Wave
{
    /// <summary>
    /// Волновой поиск путей в графе
    /// </summary>
    public class WaveSearch : SearchPathBase
    {
        public WaveSearch(Graph G)
        {
            waveSource = new WaveSource(G);
        }

        /// <summary>
        /// Привязка к новому графу
        /// </summary>
        /// <param name="g"></param>
        public override void Rebase(Graph g)
        {
            if (g != null && waveSource?.Graph != g)
            {
                waveSource = new WaveSource(g);
                Reset();
            }
        }

        public void Reset()
        {
            foundedPath = null;
            foundedPathLength = -1;
            searchEnded = false;
            pathFound = false;
        }

        /// <summary>
        /// Поиск пути от узла StartNode к узлу EndNode
        /// </summary>
        /// <param name="StartNode"></param>
        /// <param name="EndNode"></param>
        /// <returns></returns>
        public override bool SearchPath(Node StartNode, Node EndNode)
        {
            Reset();

            if (StartNode is null || EndNode is null
                || !StartNode.Passable || !EndNode.Passable
                || StartNode.Position.IsOrigin || EndNode.Position.IsOrigin)
            {

#if WAVE_DEBUG_LOG
                AStarLogger.WriteLine(LogType.Debug, $"{nameof(WaveSearch)}.{nameof(SearchPath)}: Некорректные входные данные. Прерываем поиск");
#endif
                return false;
            }
#if SearchStatistics
            SearchStatistics.Start(EndNode); 
#endif
            if (waveSource is null)
                throw new Exception($"{nameof(WaveSource)} не инициализирован");

            lock (waveSource.Graph.Locker)
            {
                LinkedList<Node> track = null;

                waveSource.Target = EndNode;

                // Проверяем наличие "кэша" волнового поиска для EndNode
                WaveWeight startWW = StartNode.WaveWeight;
                //WaveWeight endWW = EndNode.WaveWeight;
                if (waveSource.Validate(startWW))
                {
#if WAVE_DEBUG_LOG
                    AStarLogger.WriteLine(LogType.Debug, $"{nameof(WaveSearch)}.{nameof(SearchPath)}: Найден кэш волны из End{EndNode}");
#endif
                    // найден кэш волнового поиска для EndNode
                    // Пытаемся построить путь
                    // формируем путь
                    try
                    {

                        if (GoBackUpNodes(StartNode, EndNode, out track))
                        {
                            // путь найден
#if WAVE_DEBUG_LOG
                            AStarLogger.WriteLine(LogType.Debug, $"{nameof(WaveSearch)}.{nameof(SearchPath)}: В кэше найден путь: End{EndNode} <== Start{StartNode}");
#endif
                            pathFound = true;
                            foundedPath = new Node[track.Count];
                            foundedPathLength = -1;

                            track.CopyTo(foundedPath, 0);

#if SearchStatistics
                        SearchStatistics.Finish(SearchMode.WaveRepeated, EndNode, path.Length); 
#endif
                            return true;
                        }

                        // Построить путь не удалось
#if WAVE_DEBUG_LOG
                        AStarLogger.WriteLine(LogType.Debug, $"{nameof(WaveSearch)}.{nameof(SearchPath)}: Путь в кэше не найден (или некорректен). Стираем волну");
#endif
                        waveSource.ClearWave(); 
                    }
#if DEBUG || WAVE_DEBUG_LOG
                    catch (Exception e)
                    {
                        AStarLogger.WriteLine(LogType.Error, $"{nameof(WaveSearch)}.{nameof(SearchPath)}: Перехвачено исключение '{e.Message}'", true);
                        AStarLogger.WriteLine(LogType.Error, e.StackTrace, true);
                        Exception innExc = e.InnerException;
                        while (innExc != null)
                        {
                            AStarLogger.WriteLine(LogType.Error, innExc.Message, true);
                            innExc = innExc.InnerException;
                        }
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
                    if (waveSource.GenerateWave(StartNode, EndNode))
                    {
#if WAVE_DEBUG_LOG
                        AStarLogger.WriteLine(LogType.Log, $"{nameof(WaveSearch)}.{nameof(SearchPath)}: {nameof(WaveSource.GenerateWave)} завершилось успешно");
#endif

                        startWW = StartNode.WaveWeight;
                        if (startWW.IsTargetTo(EndNode))
                        {
#if WAVE_DEBUG_LOG
                            AStarLogger.WriteLine(LogType.Log, $"{nameof(WaveSearch)}.{nameof(SearchPath)}: Формируем путь из Start{StartNode} ==> End{EndNode}");
#endif
                            if (GoBackUpNodes(StartNode, EndNode, out track))
                            {
#if WAVE_DEBUG_LOG
                                AStarLogger.WriteLine(LogType.Log, $"{nameof(WaveSearch)}.{nameof(SearchPath)}: Путь сформирован");
#endif
                                // путь найден
                                pathFound = true;
                                foundedPath = new Node[track.Count];
                                foundedPathLength = -1;
                                track.CopyTo(foundedPath, 0);
                            }
                            else
                            {
#if WAVE_DEBUG_LOG
                                AStarLogger.WriteLine(LogType.Error, $"{nameof(WaveSearch)}.{nameof(SearchPath)}: Не удалось построить путь. Очищаем кэш волны");
#endif
                                waveSource.ClearWave();
                            }
                        }
#if WAVE_DEBUG_LOG
                        else AStarLogger.WriteLine(LogType.Log, $"{nameof(WaveSearch)}.{nameof(SearchPath)}: Вершина Start{StartNode} не имеет волновой оценки, соответствующей End{EndNode}");
#endif
                    }
#if WAVE_DEBUG_LOG
                    else
                    {
                        AStarLogger.WriteLine(LogType.Log, $"{nameof(WaveSearch)}.{nameof(SearchPath)}: {nameof(WaveSource.GenerateWave)} завершилось безрезультатно");
                    }
#endif
                }
#if DEBUG || WAVE_DEBUG_LOG
                catch (Exception e)
                {
                    AStarLogger.WriteLine(LogType.Error, $"{nameof(WaveSearch)}.{nameof(SearchPath)}: Перехвачено исключение '{e.Message}'", true);
                    AStarLogger.WriteLine(LogType.Error, e.StackTrace, true);
                    Exception innExc = e.InnerException;
                    while (innExc != null)
                    {
                        AStarLogger.WriteLine(LogType.Error, innExc.Message, true);
                        innExc = innExc.InnerException;
                    }
#else
                catch
                {
#endif
                    waveSource.ClearWave();
                }
            }

#if SearchStatistics
            SearchStatistics.Finish(SearchMode.WaveFirst, EndNode, path?.Length ?? 0);
#endif
            searchEnded = true;
            return pathFound;
        }

        /// <summary>
        /// Список узлов, определяющих найденный путь
        /// </summary>
        public override Node[] PathByNodes
        {
            get
            {
                if (searchEnded && pathFound)
                {
                    return foundedPath;
                }
                return null;
            }
        }
        private Node[] foundedPath = null;

        /// <summary>
        /// Длина найденного пути
        /// </summary>
        public override double PathLength
        {
            get
            {
                if (searchEnded && pathFound && foundedPath?.Length > 0)
                    if (foundedPathLength < 0)
                        return foundedPathLength = foundedPath.Sum((n) => n.WaveWeight.Weight);
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
#if DEBUG || WAVE_DEBUG_LOG
            StringBuilder sb = new StringBuilder();
#endif
            Node currentNode = startNode;
            WaveWeight currentWW;
            while (currentNode != null)
            {
                currentWW = currentNode.WaveWeight;
                if (currentWW.IsTargetTo(endNode))
                {
                    track.AddLast(currentNode);
                    if (Equals(currentNode, endNode))
                    {
                        result = true;
                        break;
                    }
                    currentNode = currentWW.Arc?.EndNode;
                }
                else
                {
#if DEBUG || WAVE_DEBUG_LOG
                    string erroeMsg = $"Вершина {currentNode} не имеет волновой оценки, позволяющей построить путь к End{endNode}";
                    sb.AppendLine(erroeMsg);
#endif
                    result =  false;
                    break;
                }
            }
            result = track.Last?.Value.Equals(endNode) == true;

#if DEBUG || WAVE_DEBUG_LOG
            sb.Append($"{nameof(WaveSearch)}.{nameof(GoBackUpNodes)}: Track:\t");
            var current = track.First;
            var last = track.Last;
            while (current != last)
            {
                sb.Append(current.Value).Append("-->");
                current = current.Next;
            }
            sb.Append(last.Value);
            AStarLogger.WriteLine(result ? LogType.Debug : LogType.Error, sb.ToString(), false);
#endif
            return result;
        } 
#endif

        /// <summary>
        /// Флаг, обозначающий, что поиск пути завершен
        /// </summary>
        public override bool SearchEnded => searchEnded;
        private bool searchEnded = false;

        /// <summary>
        /// Флаг, обозначающий, что путь найден
        /// </summary>
        public override bool PathFound => pathFound;//waveSource?.IsTargetTo(foundedPath?.LastOrDefault()) == true;
        private bool pathFound = false;

        private WaveSource waveSource = null;
    }
}
