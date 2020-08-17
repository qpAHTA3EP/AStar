using System;
using System.Collections.Generic;

namespace AStar.Search.Wave
{
    /// <summary>
    /// Волновой вес вершины
    /// </summary>
    public class WaveWeight //: IComparable<WaveWeight>
    {
        public WaveWeight() { }
        public WaveWeight(Node tar, double w, Arc arc)
        {
            Target = tar;
            Weight = w;
            Arc = arc;
        }

        public double Weight = double.MaxValue;
        public Arc Arc { get; set; } = null;

        /// <summary>
        /// Конечная вершина, к которой стремится путь (источник волны)
        /// </summary>
        public Node Target { get; }

        public bool IsValid => Target != null /* && Arc != null */;

        public bool IsTarget(Node tar)
        {
            return /*Target != null  && Arc != null && */ Equals(tar, Target);
        }
    }

    /// <summary>
    /// Волновой поиск путей в графе
    /// </summary>
#if false
    public class WaveSearch : SearchPathBase
    {
        public WaveSearch(Graph G)
        {
            graph = G;
        }

        /// <summary>
        /// Поиск пути от узла StartNode к узлу EndNode
        /// </summary>
        /// <param name="StartNode"></param>
        /// <param name="EndNode"></param>
        /// <returns></returns>
        public override bool SearchPath(Node StartNode, Node EndNode)
        {
            path = null;
            pathFound = false;
            searchEnded = false;
            nodeEvaluated = 0;

            if (StartNode == null || EndNode == null
                || !StartNode.Passable || !EndNode.Passable)
                return false;

            targetNode = EndNode;

#if SearchStatistics
            SearchStatistics.Start(EndNode); 
#endif
            // Проверяем наличие "кэша" волнового поиска для EndNode
            if (StartNode.WaveWeight.Target != null && Equals(StartNode.WaveWeight.Target, EndNode))
            {
                // найден кэш волнового поиска для EndNode
                // Пытаемся построить путь
                // формируем путь
                LinkedList<Node> nodes = new LinkedList<Node>();
                GoBackUpNodes(StartNode, ref nodes);

                if (nodes != null && nodes.Count > 0
                    && nodes.First.Value == StartNode
                    && nodes.Last.Value == EndNode)
                {
                    // путь найден
                    pathFound = true;
                    searchEnded = true;
                    path = new Node[nodes.Count];
                    nodes.CopyTo(path, 0);

#if SearchStatistics
                    SearchStatistics.Finish(SearchMode.WaveRepeated, EndNode, path.Length); 
#endif
                    return true;
                }
                else
                {
                    pathFound = false;
                    searchEnded = false;
                    path = null;
                    targetNode = null;
                }

                // Построить путь не удалось
                // пробуем пересчитать граф
            }

            lock (graph)
            {
                if (StartNode.WaveWeight.Target == null || !Equals(EndNode.WaveWeight.Target, EndNode))
                {
                    EndNode.WaveWeight = new WaveWeight();
                }
                // Помещаем в очередь обработки конечный узел
                priorityList.Enqueue(EndNode, CompareNodesWeight);

                // Обсчитываем все узлы графа
                while (priorityList.Count > 0)
                {
                    Node node = priorityList.Dequeue();
                    Evaluate(node);

                    if (node == StartNode)
                        pathFound = true;
                }
                searchEnded = true;

                // формируем путь
                LinkedList<Node> nodes = new LinkedList<Node>();
                GoBackUpNodes(StartNode, ref nodes);

                if (nodes != null && nodes.Count > 0
                    && nodes.First.Value == StartNode
                    && nodes.Last.Value == EndNode)
                {
                    // путь найден
                    pathFound = true;
                    path = new Node[nodes.Count];
                    nodes.CopyTo(path, 0);
                }
                else
                {
                    path = null;
                    searchEnded = true;
                    pathFound = false;
                }
            }

#if SearchStatistics
            SearchStatistics.Finish(SearchMode.WaveFirst, EndNode, path?.Length ?? 0);

#endif
            return pathFound;
        }

        /// <summary>
        /// Оценка вершины и присвоение "волновой оценки"
        /// </summary>
        /// <param name="node"></param>
        private void Evaluate(Node node)
        {
            WaveWeight nWeight = node.WaveWeight;
            if (nWeight is null)
                throw new ArgumentException($"В Evaluate передан узел Node #{node.GetHashCode()} без WaveWeight");

            // Просмотр входящий ребер
            foreach (Arc inArc in node.IncomingArcs)
            {
                if (inArc.Passable && inArc.StartNode.Passable)
                {
                    double weight = nWeight.Weight + inArc.Length;

                    WaveWeight inNodeWeight = inArc.StartNode.WaveWeight;
                    if (inNodeWeight.Target != null && Equals(inNodeWeight.Target, targetNode))
                    {
                        // стартовая вершина входящего ребра уже имеет "волновой вес"
                        if (inNodeWeight.Weight > weight)
                        {
                            // производим переоценку, если "волновой вес" стартовой вершины ребра больше чем weight
                            // это значет что путь через текущую вершину node - короче
                            inNodeWeight.Weight = weight;
                            inNodeWeight.Arc = inArc;
                            // добавляем вершину в очередь, для пересчета
                            priorityList.Enqueue(inArc.StartNode, CompareNodesWeight);
                        }
                    }
                    else
                    {
                        // присваиваем "волновой вес" стартовой вершине ребра
                        inArc.StartNode.WaveWeight = new WaveWeight(targetNode, weight, inArc);

                        priorityList.Enqueue(inArc.StartNode, CompareNodesWeight);
                    }
                }
            }

            nodeEvaluated++;
        }

        /// <summary>
        /// Список узлов, определяющих найденный уть
        /// </summary>
        public override Node[] PathByNodes
        {
            get
            {
                if (searchEnded && pathFound)
                {
                    return path;
                }
                return null;
            }
        }

        /// <summary>
        /// Построение списка узлов, задающих найденный путь
        /// </summary>
        /// <param name="node"></param>
        /// <param name="nodes"></param>
        private void GoBackUpNodes(Node node, ref LinkedList<Node> nodes)
        {
            WaveWeight ww = node.WaveWeight;
            if (ww.Target != null && Equals(ww.Target, targetNode))
            {
                //TODO : сделать проверку (ww.Arc.Passable && ww.Arc.EndNode.Passable)
                nodes.AddLast(node);
                if (ww.Arc != null)
                    GoBackUpNodes(ww.Arc.EndNode, ref nodes);
            }
        }

        /// <summary>
        /// сравнение весов вершины
        /// </summary>
        /// <param name="n1"></param>
        /// <param name="n2"></param>
        /// <param name="hash"></param>
        /// <returns></returns>
        public int CompareNodesWeight(Node n1, Node n2)
        {
            if (n1.WaveWeight.Target != null && Equals(n1.WaveWeight.Target, n2.WaveWeight.Target))
            {
                double w1 = n1.WaveWeight.Weight;
                double w2 = n2.WaveWeight.Weight;

                return Convert.ToInt32(w1 - w2);
            }
            return 0;
        }

        /// <summary>
        /// Флаг, обозначающий, что поиск пути завершен
        /// </summary>
        public override bool SearchEnded
        {
            get => searchEnded;
        }

        /// <summary>
        /// Флаг, обозначающий, что путь найден
        /// </summary>
        public override bool PathFound
        {
            get => pathFound;
        }

        private Node[] path = null;
        private bool searchEnded = false;
        private bool pathFound = false;
        //private Queue<Node> queue = new Queue<Node>();
        private LinkedList<Node> priorityList = new LinkedList<Node>();
        private Node targetNode = null;
        private Graph graph = null;
        private int nodeEvaluated = 0;
    }

#endif
    public class WaveSearch : SearchPathBase
    {
        public WaveSearch(Graph G)
        {
            graph = G;
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
                path = null;
                pathFound = false;
                searchEnded = false;
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
                // найден кэш волнового поиска для EndNode
                // Пытаемся построить путь
                // формируем путь
                LinkedList<Node> road = new LinkedList<Node>();
                try
                {
                    GoBackUpNodes(EndNode, StartNode, ref road);

                    if (road != null && road.Count > 0
                        && Equals(road.First.Value, StartNode)
                        && Equals(road.Last.Value, EndNode))
                    {
                        // путь найден
                        pathFound = true;
                        searchEnded = true;
                        path = new Node[road.Count];
                        road.CopyTo(path, 0);

#if SearchStatistics
                    SearchStatistics.Finish(SearchMode.WaveRepeated, EndNode, path.Length); 
#endif
                        return true;
                    }

                    pathFound = false;
                    searchEnded = false;
                    path = null;
                    //targetNode = null;

                    // Построить путь не удалось
                    // пробуем пересчитать граф
                }
                catch
                {
                    pathFound = false;
                    searchEnded = false;
                    path = null;
                    //targetNode = null;
                }
            }

            lock (graph.Locker)
            {
                //if (StartNode.WaveWeight?.Target == null || !Equals(EndNode.WaveWeight.Target, EndNode))
                {
                    EndNode.WaveWeight = new WaveWeight(EndNode, 0, null);
                }
                // Помещаем в очередь обработки конечный узел
                LinkedList<Node> processingQueue = new LinkedList<Node>();

                processingQueue.Enqueue(EndNode, CompareNodesWeight);

                // Обсчитываем все узлы графа
                while (processingQueue.Count > 0)
                {
                    Node node = processingQueue.ProcessingFirst(EndNode);

                    if (Equals(node, StartNode))
                    {
                        pathFound = true;
                        break;
                    }
                }
                searchEnded = true;

                // формируем путь
                LinkedList<Node> road = new LinkedList<Node>();
                try
                {
                    GoBackUpNodes(EndNode, StartNode, ref road);

                    if (road != null && road.Count > 0
                        && Equals(road.First.Value, StartNode)
                        && Equals(road.Last.Value, EndNode))
                    {
                        // путь найден
                        pathFound = true;
                        path = new Node[road.Count];
                        road.CopyTo(path, 0);
                    }
                    else
                    {
                        path = null;
                        searchEnded = true;
                        pathFound = false;
                    }
                }
                catch
                {
                    road?.Clear();
                    path = null;
                    searchEnded = true;
                    pathFound = false;
                }
            }

#if SearchStatistics
            SearchStatistics.Finish(SearchMode.WaveFirst, EndNode, path?.Length ?? 0);
#endif
            return pathFound;
        }

        /// <summary>
        /// Список узлов, определяющих найденный уть
        /// </summary>
        public override Node[] PathByNodes
        {
            get
            {
                if (searchEnded && pathFound)
                {
                    return path;
                }
                return null;
            }
        }

        /// <summary>
        /// Построение списка узлов, задающих найденный путь
        /// </summary>
        /// <param name="endNode">конечный узел пути</param>
        /// <param name="node">текущий узел пути</param>
        /// <param name="road">сформированный путь</param>
        private static void GoBackUpNodes(Node endNode, Node node, ref LinkedList<Node> road)
        {
            if (Equals(node, endNode))
            {
                road.AddLast(node);
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
                road.AddLast(node);
                if (ww.Arc != null)
                {
                    GoBackUpNodes(endNode, ww.Arc.EndNode, ref road);
                    return;
                }
            }
            throw new ArgumentException("Вершина 'node' не имеет волновой оценки, позволяющей построить путь к 'endNode'");
        }

        /// <summary>
        /// сравнение весов вершины
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

        public override void Rebase(Graph g)
        {
            graph = g;
            path = null;
            searchEnded = false;
            pathFound = false;
            //priorityList?.Clear();
            //targetNode = null;
        }

        /// <summary>
        /// Флаг, обозначающий, что поиск пути завершен
        /// </summary>
        public override bool SearchEnded => searchEnded;

        /// <summary>
        /// Флаг, обозначающий, что путь найден
        /// </summary>
        public override bool PathFound => pathFound;

        private Node[] path = null;
        private bool searchEnded = false;
        private bool pathFound = false;
        //private LinkedList<Node> priorityList = new LinkedList<Node>();
        //private Node targetNode = null;
        private Graph graph = null;
    }


    internal static class LinkedListExtention
    {
        /// <summary>
        /// Добавление узла в упорядоченную очередь List
        /// в порядке возрастания WaveWeight
        /// </summary>
        /// <param name="List"></param>
        /// <param name="n"></param>
        /// <param name="hash"></param>
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
#if AStarLog
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
    }
}
