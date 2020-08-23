using System;
using System.Collections.Generic;
using System.Linq;

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
                Reset();

            }
        }

        public void Reset()
        {
            foundedPath = null;
            foundedPathLength = -1;
            searchEnded = false;
            pathFound = false;
            ClearWave();
        }

        void ClearWave()
        {
            if(graph != null)
            {
                foreach(Node n in graph.Nodes)
                    n.WaveWeight = null;
#if DEBUG_LOG
                AStarLogger.WriteLine(LogType.Log, $"{nameof(WaveSearch)}.{nameof(ClearWave)}: {graph.Nodes.Count} nodes processed");
#endif
            }
            processingQueue.Clear();
        }

        /// <summary>
        /// Поиск пути от узла StartNode к узлу EndNode
        /// </summary>
        /// <param name="StartNode"></param>
        /// <param name="EndNode"></param>
        /// <returns></returns>
        public override bool SearchPath(Node StartNode, Node EndNode)
        {
            if (StartNode is null || EndNode is null
                || !StartNode.Passable || !EndNode.Passable)
            {
                foundedPath = null;
                foundedPathLength = -1;
                pathFound = false;
                searchEnded = false;
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
            if (startWW.IsTarget(EndNode))
            {
#if DEBUG_LOG
                AStarLogger.WriteLine(LogType.Debug, $"{nameof(WaveSearch)}.{nameof(SearchPath)}: Найден кэш волны из End{EndNode}");
#endif
                // найден кэш волнового поиска для EndNode
                // Пытаемся построить путь
                // формируем путь
                LinkedList<Node> track = new LinkedList<Node>();
                try
                {
                    GoBackUpNodes(EndNode, StartNode, ref track);

                    if (track != null && track.Count > 0
                        && Equals(track.First.Value, StartNode)
                        && Equals(track.Last.Value, EndNode))
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

#if DEBUG_LOG
                    AStarLogger.WriteLine(LogType.Debug, $"{nameof(WaveSearch)}.{nameof(SearchPath)}: Путь в кэше не найден (или некорректен). Стираем волну");
#endif
                    Reset();

                    // Построить путь не удалось
                    // пробуем пересчитать граф
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
                    Reset();
                }
            }

            lock (graph.Locker)
            {
                ClearWave();
                EndNode.WaveWeight = new WaveWeight(EndNode, 0, null);

                if (processingQueue.Count == 0 || processingQueue.FirstOrDefault()?.WaveWeight?.IsTarget(EndNode) != true)
                {
#if DEBUG_LOG
                    AStarLogger.WriteLine(LogType.Debug, $"{nameof(WaveSearch)}.{nameof(SearchPath)}: Инициализируем новую волну из End{EndNode}");
#endif
                    // Помещаем в очередь обработки конечный узел
                    processingQueue.Clear();

#if processingQueue_SortedSet
                    processingQueue.Add(EndNode); 
#else
                    processingQueue.Enqueue(EndNode, CompareNodesWeight);
#endif
                }
#if DEBUG_LOG
                else
                {
                    AStarLogger.WriteLine(LogType.Debug, $"{nameof(WaveSearch)}.{nameof(SearchPath)}: Продолжаем кэшированну волну");
                    AStarLogger.WriteLine(LogType.Debug, $"{nameof(WaveSearch)}.{nameof(SearchPath)}: старая ProcessingQueue");
                    foreach (var node in processingQueue)
                    {
                        AStarLogger.WriteLine(LogType.Debug, $"\t\t{node}\t|\t{node.WaveWeight}");
                    }
                }
#endif

                // Обсчитываем все узлы графа
                while (processingQueue.Count > 0)
                {
                    Node node = processingQueue.ProcessingFirst(EndNode);

                    if (Equals(node, StartNode))
                    {
                        pathFound = true;
#if !continue_waveSearch_after_reaching_startNode
#if DEBUG_LOG
                        AStarLogger.WriteLine(LogType.Debug, $"{nameof(WaveSearch)}.{nameof(SearchPath)}: Путь найден. Достигнута вершина Start{StartNode}... Завершаем поиск");
                        AStarLogger.WriteLine(LogType.Debug, $"{nameof(WaveSearch)}.{nameof(SearchPath)}: старая ProcessingQueue");
                        foreach (var n in processingQueue)
                        {
                            AStarLogger.WriteLine(LogType.Debug, $"{nameof(WaveSearch)}.{nameof(SearchPath)}:\t{n}\t|\t{n.WaveWeight}");
                        }
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
                LinkedList<Node> track = new LinkedList<Node>();
                try
                {
#if DEBUG_LOG
                    AStarLogger.WriteLine(LogType.Log, $"{nameof(WaveSearch)}.{nameof(SearchPath)}: Формируем путь из Start{StartNode} ==> End{EndNode}");
#endif
                    GoBackUpNodes(EndNode, StartNode, ref track);

                    if (track != null && track.Count > 0
                        && Equals(track.First.Value, StartNode)
                        && Equals(track.Last.Value, EndNode))
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
                        AStarLogger.WriteLine(LogType.Error, $"{nameof(WaveSearch)}.{nameof(SearchPath)}: Не удалось построить путь");
#endif
                        foundedPath = null;
                        foundedPathLength = -1;
                        searchEnded = true;
                        pathFound = false;
                    }
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
                    Reset();
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

        /// <summary>
        /// Построение списка узлов, задающих найденный путь
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
            if (ww.IsTarget(endNode))
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
                {
                    GoBackUpNodes(endNode, ww.Arc.EndNode, ref track);
                    return;
                }
            }
            throw new ArgumentException($"Вершина {node} не имеет волновой оценки, позволяющей построить путь к End{endNode}");
        }

        /// <summary>
        /// Метод сравнение вершин по их волновому весу
        /// </summary>
        /// <param name="n1"></param>
        /// <param name="n2"></param>
        /// <returns></returns>
        internal static int CompareNodesWeight(Node n1, Node n2)
        {
            WaveWeight ww1 = n1.WaveWeight;
            WaveWeight ww2 = n2.WaveWeight;
            if(ww1 is null)
            {
                if (ww2 is null)
                    return 0;
                else return 1;
            }
            else if(ww2 is null)
                return -1;
            if (n1.WaveWeight.Target != null && Equals(n1.WaveWeight.Target, n2.WaveWeight.Target))
            {
                double w1 = n1.WaveWeight.Weight;
                double w2 = n2.WaveWeight.Weight;

                if (w1 < w2)
                    return -1;
                else if (w1 > w2)
                    return +1;
                else return 0;
                //return Convert.ToInt32(w1 - w2);
            }
            return 0;
        }

        /// <summary>
        /// Функтор сравнения верши по их волновому весу
        /// </summary>
        protected class NodesWeightComparer : IComparer<Node>
        {
            public int Compare(Node n1, Node n2)
            {
                if(n1 is null)
                    if (n2 is null)
                        return 0;
                    else return +1;
                else if (n2 is null)
                    return -1;

                WaveWeight ww1 = n1.WaveWeight;
                WaveWeight ww2 = n2.WaveWeight;
                if (ww1 is null)
                {
                    if (ww2 is null)
                        return 0;
                    else return 1;
                }
                else if (ww2 is null)
                    return -1;
                if (n1.WaveWeight.Target != null && Equals(n1.WaveWeight.Target, n2.WaveWeight.Target))
                {
                    double w1 = n1.WaveWeight.Weight;
                    double w2 = n2.WaveWeight.Weight;

                    if (w1 < w2)
                        return -1;
                    else if (w1 > w2)
                        return +1;
                    else return 0;
                    //return Convert.ToInt32(w1 - w2);
                }
                return 0;
            }
        }

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
        private SortedSet<Node> processingQueue = new SortedSet<Node>(new NodesWeightComparer());
#else
        private LinkedList<Node> processingQueue = new LinkedList<Node>();
#endif
        //WaveStatistic wave = new WaveStatistic();

        //private LinkedList<Node> priorityList = new LinkedList<Node>();
        //private Node targetNode = null;
        private Graph graph = null;
    }

    internal static class LinkedListExtension
    {
        /// <summary>
        /// Добавление узла в упорядоченную очередь List
        /// в порядке возрастания WaveWeight
        /// </summary>
        /// <param name="List"></param>
        /// <param name="n"></param>
        /// <param name="comparer"></param>
        public static void Enqueue(this LinkedList<Node> List, Node n, Func<Node, Node, int> comparer)
        {
            LinkedListNode<Node> pos = List.First;

            while (pos != null)
            {
                // Нужна ли эта проверка, ибо она не гарантирует что n будет представлена в очереди в единственном экземпляре
                if (Equals(n, pos.Value))
                    return;

                if (comparer(n, pos.Value) < 0)
                {
                    List.AddBefore(pos, n);
                    return;
                }
                pos = pos.Next;
            }

            List.AddLast(n);
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
        /// <param name="processingQueue">Очередь обработки вершин</param>
        public static Node ProcessingFirst(this LinkedList<Node> processingQueue, Node endNode)
        {
            Node node = processingQueue.Dequeue();

            if (node != null)
            {
#if DEBUG_LOG
                string logStr = string.Concat(nameof(ProcessingFirst), ": Processing Node #", node.GetHashCode().ToString("X"));

                AStarLogger.WriteLine(logStr);
#endif
                WaveWeight nWeight = node.WaveWeight;
                //if (nWeight is null)
                //    throw new ArgumentException($"В {MethodBase.GetCurrentMethod().Name} передан узел Node #{node.GetHashCode()} без WaveWeight");

                // Просмотр входящих ребер
                foreach (Arc inArc in node.IncomingArcs)
                {
                    if (inArc.Passable && inArc.StartNode.Passable)
                    {
                        double weight = nWeight.Weight + inArc.Length;

                        WaveWeight inNodeWeight = inArc.StartNode.WaveWeight;
                        if (inNodeWeight.IsTarget(endNode))
                        {
                            // стартовая вершина входящего ребра уже имеет "волновой вес"
                            if (inNodeWeight.Weight > weight)
                            {
                                // производим переоценку, если "волновой вес" стартовой вершины ребра больше чем weight
                                // это значит что путь через текущую вершину node - короче
                                inNodeWeight.Weight = weight;
                                inNodeWeight.Arc = inArc;
                                // добавляем стартовую вершину ребра в очередь, для обработки
                                processingQueue.Enqueue(inArc.StartNode, WaveSearch.CompareNodesWeight);
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

                            processingQueue.Enqueue(inArc.StartNode, WaveSearch.CompareNodesWeight);
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
#if DEBUG_LOG
                AStarLogger.WriteLine(LogType.Debug, string.Concat(nameof(ProcessingFirst), ": Обрабатываем вершину ", node));
#endif
                processingQueue.Remove(node);

                WaveWeight nWeight = node.WaveWeight;
                //if (nWeight is null)
                //    throw new ArgumentException($"В {MethodBase.GetCurrentMethod().Name} передан узел Node #{node.GetHashCode()} без WaveWeight");

                // Просмотр входящих ребер
                foreach (Arc inArc in node.IncomingArcs)
                {
                    if (inArc.Passable && inArc.StartNode.Passable)
                    {
#if DEBUG_LOG
                        AStarLogger.WriteLine(LogType.Debug, string.Concat(nameof(ProcessingFirst), ":\tАнализируем ребро ", inArc));
#endif
                        double weight = nWeight.Weight + inArc.Length;

                        WaveWeight inNodeWeight = inArc.StartNode.WaveWeight;

                        bool inNodeTargetMatch = inNodeWeight.IsTarget(endNode);
#if DEBUG_LOG
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
#if DEBUG_LOG
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
#if DEBUG_LOG
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
