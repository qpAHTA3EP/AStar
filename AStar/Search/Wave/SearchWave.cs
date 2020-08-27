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
            graph = G;
        }

        /// <summary>
        /// Привязка к новому графу
        /// </summary>
        /// <param name="g"></param>
        public override void Rebase(Graph g)
        {
            if (graph != g)
            {
                graph = g;
                ClearWave();

            }
        }

        public void Reset()
        {
            foundedPath = null;
            foundedPathLength = -1;
            searchEnded = false;
            pathFound = false;
        }

        void ClearWave()
        {
            foundedPath = null;
            foundedPathLength = -1;
            searchEnded = false;
            pathFound = false;
            if(graph != null)
            {
                foreach(Node n in graph.Nodes)
                    n.WaveWeight = null;
#if DEBUG_LOG
                AStarLogger.WriteLine(LogType.Log, $"{nameof(WaveSearch)}.{nameof(ClearWave)}: {graph.Nodes.Count} nodes processed");
#endif
            }
            waveFront.Clear();
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

#if DEBUG_LOG
                AStarLogger.WriteLine(LogType.Debug, $"{nameof(WaveSearch)}.{nameof(SearchPath)}: Некорректные входные данные. Прерываем поиск");
#endif
                return false;
            }
#if SearchStatistics
            SearchStatistics.Start(EndNode); 
#endif
            // Проверяем наличие "кэша" волнового поиска для EndNode
            WaveWeight startWW = StartNode.WaveWeight;
            //WaveWeight endWW = EndNode.WaveWeight;
            if (startWW.IsTargetTo(EndNode))
            {
#if DEBUG_LOG
                AStarLogger.WriteLine(LogType.Debug, $"{nameof(WaveSearch)}.{nameof(SearchPath)}: Найден кэш волны из End{EndNode}");
#endif
                // найден кэш волнового поиска для EndNode
                // Пытаемся построить путь
                // формируем путь
                try
                {

                    if (GoBackUpNodes(StartNode, EndNode, out LinkedList<Node> track))
                    {
                        // путь найден
#if DEBUG_LOG
                        AStarLogger.WriteLine(LogType.Debug, $"{nameof(WaveSearch)}.{nameof(SearchPath)}: В кэше найден путь: End{EndNode} <== Start{StartNode}");
#endif
                        pathFound = true;
                        searchEnded = true;
                        foundedPath = new Node[track.Count];
                        foundedPathLength = -1;

                        track.CopyTo(foundedPath, 0);

#if SearchStatistics
                    SearchStatistics.Finish(SearchMode.WaveRepeated, EndNode, path.Length); 
#endif
                        return true;
                    }

                    // Построить путь не удалось
#if DEBUG_LOG
                    AStarLogger.WriteLine(LogType.Debug, $"{nameof(WaveSearch)}.{nameof(SearchPath)}: Путь в кэше не найден (или некорректен). Стираем волну");
#endif
                    ClearWave(); 

                }
#if DEBUG || DEBUG_LOG
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
                    ClearWave();
                }
            }

            lock (graph.Locker)
            {
                //ClearWave();
                EndNode.WaveWeight = new WaveWeight(EndNode, 0, null);

                if (waveFront.Count == 0 || waveFront.FirstOrDefault()?.WaveWeight?.IsTargetTo(EndNode) != true)
                {
#if DEBUG_LOG
                    AStarLogger.WriteLine(LogType.Debug, $"{nameof(WaveSearch)}.{nameof(SearchPath)}: Инициализируем новую волну из End{EndNode}");
#endif
                    // Помещаем в очередь обработки конечный узел
                    waveFront.Clear();

#if processingQueue_SortedSet
                    processingQueue.Add(EndNode); 
#else
                    waveFront.Enqueue(EndNode, NodesWeightComparer.CompareNodesWeight);
#endif
                }
#if DEBUG_LOG
                else
                {
#if DETAIL_LOG
                    AStarLogger.WriteLine(LogType.Debug, $"{nameof(WaveSearch)}.{nameof(SearchPath)}: Продолжаем кэшированну волну");
                    AStarLogger.WriteLine(LogType.Debug, $"{nameof(WaveSearch)}.{nameof(SearchPath)}: Кэшированный фронт волны (ProcessingQueue)");
                    foreach (var node in waveFront)
                    {
                        AStarLogger.WriteLine(LogType.Debug, $"\t\t{node}\t|\t{node.WaveWeight}");
                    } 
#else
                    AStarLogger.WriteLine(LogType.Debug, $"{nameof(WaveSearch)}.{nameof(SearchPath)}: Продолжаем кэшированный волновой фронт, содержащий {waveFront.Count} вершин");
#endif
                }
#endif

                // Обсчитываем все узлы графа
                while (waveFront.Count > 0)
                {
                    Node node = waveFront.ProcessingFirst(EndNode);

                    if (Equals(node, StartNode))
                    {
                        pathFound = true;
#if !continue_waveSearch_after_reaching_startNode
#if DEBUG_LOG
                        AStarLogger.WriteLine(LogType.Debug, $"{nameof(WaveSearch)}.{nameof(SearchPath)}: Путь найден. Достигнута вершина Start{StartNode} | {StartNode.WaveWeight} ... Завершаем поиск");
#if DETAIL_LOG
                        AStarLogger.WriteLine(LogType.Debug, $"{nameof(WaveSearch)}.{nameof(SearchPath)}: Фронт волны (ProcessingQueue):");
                        foreach (var n in waveFront)
                        {
                            AStarLogger.WriteLine(LogType.Debug, $"{nameof(WaveSearch)}.{nameof(SearchPath)}:\t{n}\t|\t{n.WaveWeight}");
                        } 
#endif
#endif
                        break;
#else
#if DEBUG_LOG
                        AStarLogger.WriteLine(LogType.Log, $"{nameof(WaveSearch)}.{nameof(SearchPath)}: Путь найден. Достигнута вершина Start{StartNode}... Продолжаем волну");
#endif
#endif
                    }
                }
                searchEnded = true;

                // формируем путь
                LinkedList<Node> track = null;
                try
                {
                    startWW = StartNode.WaveWeight;
                    if (startWW.IsTargetTo(EndNode))
                    {
#if DEBUG_LOG
                        AStarLogger.WriteLine(LogType.Log, $"{nameof(WaveSearch)}.{nameof(SearchPath)}: Формируем путь из Start{StartNode} ==> End{EndNode}");
#endif
                        if(GoBackUpNodes(StartNode, EndNode, out track))
                        {
#if DEBUG_LOG
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
#if DEBUG_LOG
                            AStarLogger.WriteLine(LogType.Error, $"{nameof(WaveSearch)}.{nameof(SearchPath)}: Не удалось построить путь. Очищаем кэш волны");
#endif
                            ClearWave();
                            searchEnded = true;
                        }
                    }
#if DEBUG_LOG
                    else AStarLogger.WriteLine(LogType.Log, $"{nameof(WaveSearch)}.{nameof(SearchPath)}: Вершина Start{StartNode} не имеет волновой оценки, соответствующей End{EndNode}");
#endif
                }
#if DEBUG || DEBUG_LOG
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
                    ClearWave();
                }
            }

#if SearchStatistics
            SearchStatistics.Finish(SearchMode.WaveFirst, EndNode, path?.Length ?? 0);
#endif
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
            track = new LinkedList<Node>();
            Node currentNode = startNode;
            WaveWeight currentWW;
            while (currentNode != null)
            {
                currentWW = currentNode.WaveWeight;
                if (currentWW.IsTargetTo(endNode))
                {
                    track.AddLast(currentNode);
                    if (Equals(currentNode, endNode))
                        return true;
                    currentNode = currentWW.Arc?.EndNode;
                }
                else
                {
#if DEBUG || DEBUG_LOG
                    string erroeMsg = $"Вершина {currentNode} не имеет волновой оценки, позволяющей построить путь к End{endNode}";
                    StringBuilder sb = new StringBuilder();
                    sb.AppendLine(erroeMsg);
                    sb.Append("Сформированная часть пути: ");
                    var current = track.First;
                    var last = track.Last;
                    while (current != last)
                    {
                        sb.Append(current.Value).Append("-->");
                    }
                    sb.Append(last);
                    AStarLogger.WriteLine(LogType.Error, sb.ToString(), false);
#endif
                    return false;
                }
            }
            return track.Last?.Value.Equals(endNode) == true;
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
        public override bool PathFound => pathFound;
        private bool pathFound = false;

#if processingQueue_SortedSet
        private SortedSet<Node> waveFront = new SortedSet<Node>(new NodesWeightComparer());
#else
        private LinkedList<Node> waveFront = new LinkedList<Node>();
#endif

        private Graph graph = null;
    }

    internal static class WaveFrontProcessing
    {
        /// <summary>
        /// Добавление узла в упорядоченную очередь List
        /// в порядке возрастания WaveWeight
        /// </summary>
        /// <param name="list"></param>
        /// <param name="n"></param>
        /// <param name="comparer"></param>
        public static void Enqueue(this LinkedList<Node> list, Node n, Func<Node, Node, int> comparer)
        {
            LinkedListNode<Node> pos = list.First;

            while (pos != null)
            {
                // Нужна ли эта проверка, ибо она не гарантирует что n будет представлена в очереди в единственном экземпляре
                if (Equals(n, pos.Value))
                    return;

                if (comparer(n, pos.Value) < 0)
                {
                    list.AddBefore(pos, n);
                    return;
                }
                pos = pos.Next;
            }

            list.AddLast(n);
        }

        /// <summary>
        /// Извлечение узла из упорядоченного списка
        /// </summary>
        /// <param name="list"></param>
        /// <returns></returns>
        public static Node Dequeue(this LinkedList<Node> list)
        {
            LinkedListNode<Node> n = list.First;
            list.RemoveFirst();
            return n.Value;
        }

        /// <summary>
        /// Проверка первой вершины из очереди, просмотр её входящих ребер, присвоение "волновой оценки" вершине и добавление в очередь обработки "входящих" вершин
        /// </summary>
        /// <param name="endNode">Целевая вершина, к которой нужно проложить путь</param>
        /// <param name="waveFront">Очередь обработки вершин</param>
        public static Node ProcessingFirst(this LinkedList<Node> waveFront, Node endNode)
        {
            Node node = waveFront.Dequeue();

            if (node != null)
            {
#if DETAIL_LOG && DEBUG_LOG
                string logStr = string.Concat(nameof(ProcessingFirst), ": Обрабатываем вершину", node);

                AStarLogger.WriteLine(logStr);
#endif
                WaveWeight nWeight = node.WaveWeight;

                // Просмотр входящих ребер
                foreach (Arc inArc in node.IncomingArcs)
                {
                    Node inArcStartNode = inArc.StartNode;
                    if (inArc.Passable && inArcStartNode.Passable)
                    {
#if DETAIL_LOG && DEBUG_LOG
                        AStarLogger.WriteLine(LogType.Debug, string.Concat(nameof(ProcessingFirst), ":\tАнализируем ребро ", inArc));
#endif
                        double weight = nWeight.Weight + inArc.Length;

                        WaveWeight inNodeWeight = inArcStartNode.WaveWeight;

                        bool inNodeTargetMatch = inNodeWeight.IsTargetTo(endNode);
#if DETAIL_LOG && DEBUG_LOG
                        AStarLogger.WriteLine(LogType.Debug, string.Concat(nameof(ProcessingFirst), ":\t", inNodeTargetMatch ? "MATCH " : "MISMATCH ", inNodeWeight));
                        AStarLogger.WriteLine(LogType.Debug, string.Concat(nameof(ProcessingFirst), ":\t\tНовый волновой вес : ", weight.ToString("N2")));
#endif
                        if (inNodeTargetMatch)
                        {
                            // стартовая вершина входящего ребра уже имеет "волновой вес"
                            if (inNodeWeight.Weight > weight)
                            {
                                // производим переоценку, если "волновой вес" стартовой вершины ребра больше чем weight
                                // это значит что путь через текущую вершину node - короче
                                inNodeWeight.Weight = weight;
                                inNodeWeight.Arc = inArc;
#if DETAIL_LOG && DEBUG_LOG
                                AStarLogger.WriteLine(LogType.Debug, string.Concat(nameof(ProcessingFirst), ":\t\tИзменяем волновую оценку на ", inNodeWeight));
                                AStarLogger.WriteLine(LogType.Debug, string.Concat(nameof(ProcessingFirst), ":\t\tПомещаем в очередь обработки ", inArc.StartNode));
#endif
                                // добавляем стартовую вершину ребра в очередь, для обработки
                                waveFront.Enqueue(inArcStartNode, NodesWeightComparer.CompareNodesWeight);
                            }
                            else
                            {
                                // В этом случае в из inArc.StartNode есть более короткий путь к endNode
                            }
                        }
                        else
                        {
                            // присваиваем "волновой вес" стартовой вершине ребра
                            inArcStartNode.WaveWeight = new WaveWeight(endNode, weight, inArc);
#if DETAIL_LOG && DEBUG_LOG
                            AStarLogger.WriteLine(LogType.Debug, string.Concat(nameof(ProcessingFirst), ":\t\tПрисваиваем волновую оценку ", inArc.StartNode.WaveWeight));
                            AStarLogger.WriteLine(LogType.Debug, string.Concat(nameof(ProcessingFirst), ":\t\tПомещаем в очередь обработки ", inArc.StartNode));
#endif
                            waveFront.Enqueue(inArcStartNode, NodesWeightComparer.CompareNodesWeight);
                        }
                    }
                }
            }

            return node;
        }
        public static Node ProcessingFirst(this SortedSet<Node> processingQueue, Node endNode)
        {
            Node node = processingQueue.FirstOrDefault();

            if (node != null)
            {
#if DETAIL_LOG && DEBUG_LOG
                AStarLogger.WriteLine(LogType.Debug, string.Concat(nameof(ProcessingFirst), ": Обрабатываем вершину ", node));
#endif
                processingQueue.Remove(node);

                WaveWeight nWeight = node.WaveWeight;

                // Просмотр входящих ребер
                foreach (Arc inArc in node.IncomingArcs)
                {
                    if (inArc.Passable && inArc.StartNode.Passable)
                    {
#if DETAIL_LOG && DEBUG_LOG
                        AStarLogger.WriteLine(LogType.Debug, string.Concat(nameof(ProcessingFirst), ":\tАнализируем ребро ", inArc));
#endif
                        double weight = nWeight.Weight + inArc.Length;

                        WaveWeight inNodeWeight = inArc.StartNode.WaveWeight;

                        bool inNodeTargetMatch = inNodeWeight.IsTargetTo(endNode);
#if DETAIL_LOG && DEBUG_LOG
                        AStarLogger.WriteLine(LogType.Debug, string.Concat(nameof(ProcessingFirst), ":\t", inNodeTargetMatch ? "MATCH " : "MISMATCH ", inNodeWeight));
                        AStarLogger.WriteLine(LogType.Debug, string.Concat(nameof(ProcessingFirst), ":\t\tНовый волновой вес : ", weight.ToString("N2")));
#endif
                        if (inNodeTargetMatch)
                        {
                            // стартовая вершина входящего ребра уже имеет "волновой вес"
                            if (inNodeWeight.Weight > weight)
                            {

                                // производим переоценку, если "волновой вес" стартовой вершины ребра больше чем weight
                                // это значит что путь через текущую вершину node - короче
                                inNodeWeight.Weight = weight;
                                inNodeWeight.Arc = inArc;
#if DETAIL_LOG && DEBUG_LOG
                                AStarLogger.WriteLine(LogType.Debug, string.Concat(nameof(ProcessingFirst), ":\t\tИзменяем волновую оценку на ", inNodeWeight));
                                AStarLogger.WriteLine(LogType.Debug, string.Concat(nameof(ProcessingFirst), ":\t\tПомещаем в очередь обработки ", inArc.StartNode));
#endif
                                // добавляем стартовую вершину ребра в очередь, для обработки
                                processingQueue.Add(inArc.StartNode);
                            }
                            else
                            {
                                // В этом случае в из inArc.StartNode есть более короткий путь к endNode
                            }

                        }
                        else
                        {
                            // присваиваем "волновой вес" стартовой вершине ребра
                            inArc.StartNode.WaveWeight = new WaveWeight(endNode, weight, inArc);
#if DETAIL_LOG && DEBUG_LOG
                            AStarLogger.WriteLine(LogType.Debug, string.Concat(nameof(ProcessingFirst), ":\t\tПрисваиваем волновую оценку ", inArc.StartNode.WaveWeight));
                            AStarLogger.WriteLine(LogType.Debug, string.Concat(nameof(ProcessingFirst), ":\t\tПомещаем в очередь обработки ", inArc.StartNode));
#endif
                            processingQueue.Add(inArc.StartNode);
                        }
                    }
                }
            }

            return node;
        }
    }
}
