using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace AStar
{
    public class WaveWeight
    {
        public WaveWeight() { }

        public WaveWeight(double w, Arc arc)
        {
            Weight = w;
            Arc = arc;
        }
        public double Weight = double.MaxValue;
        public Arc Arc { get; set; } = null;
    }

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

            SearchStatistics.Start(EndNode);
            // Проверяем наличие "кэша" волнового поиска для EndNode
            if (StartNode.Tags.TryGetValue(targetNode.Position, out object objStartWeight)
                && objStartWeight is WaveWeight startWeight && startWeight.Arc != null && startWeight.Weight > 0
                && EndNode.Tags.TryGetValue(targetNode.Position, out object objEndNode)
                && objEndNode is WaveWeight endWeight)
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

                    SearchStatistics.Finish(SearchMode.WaveRepeated, EndNode, path.Length);
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
                if (!EndNode.Tags.ContainsKey(EndNode.Position))
                {
                    EndNode.Tags.Add(EndNode.Position, new WaveWeight());
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

            SearchStatistics.Finish(SearchMode.WaveFirst, EndNode, path?.Length ?? 0);

            return pathFound;
        }

        /// <summary>
        /// Оценка вершины и присвоение "волновой оценки"
        /// </summary>
        /// <param name="node"></param>
        private void Evaluate(Node node)
        {
            WaveWeight nWeight = null;
            if (node.Tags.TryGetValue(targetNode.Position, out object objWeight))
                nWeight = objWeight as WaveWeight;
            else throw new ArgumentException($"В Evaluate передан узел Node #{node.GetHashCode()} без WaveWeight");

            // Просмотр входящий ребер
            foreach (Arc inArc in node.IncomingArcs)
            {
                if (inArc.StartNode.Passable)
                {
                    double weight = nWeight.Weight + inArc.Length;

                    if (inArc.StartNode.Tags.ContainsKey(targetNode.Position))
                    {
                        // стартовая вершина входящего ребра уже имеет "волновой вес"
                        if (inArc.StartNode.Tags[targetNode.Position] is WaveWeight inNodeWeight)
                        {
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
                            inArc.StartNode.Tags[targetNode.Position] = new WaveWeight(weight, inArc);
                        }
                    }
                    else
                    {
                        // присваиваем "волновой вес" стартовой вершине ребра
                        inArc.StartNode.Tags.Add(targetNode.Position, new WaveWeight(weight, inArc));
                        //queue.Enqueue(inArc.StartNode);
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
            //if (nodes == null)
            //    nodes = new LinkedList<Node>();

            if (node.Tags.TryGetValue(targetNode.Position, out object obj)
                && obj is WaveWeight ww)
            {
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
            if (n1.Tags.TryGetValue(targetNode.Position, out object tag1)
                && n2.Tags.TryGetValue(targetNode.Position, out object tag2))
            {
                double w1 = ((WaveWeight)tag1).Weight;
                double w2 = ((WaveWeight)tag2).Weight;

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

    public static class LinkedListExtention
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
            bool enquered = false;
            LinkedListNode<Node> pos = List.First;

            while (pos != null)
            {
                if (comparer(n, pos.Value) < 0)
                {
                    List.AddBefore(pos, n);
                    enquered = true;
                    break;
                }
                pos = pos.Next;
            }
            if (!enquered)
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
    }
}
