using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AStar.Search.Wave
{
    public class WaveSource
    {
        public WaveSource(Graph g)
        {
            _graph = g;
        }

        public Graph Graph { get => _graph; }
        Graph _graph;

        public Node Target { get =>_target;
            set
            {
                if(!Equals(value, _target) || !Validate(value.WaveWeight))
                {
                    _target = value;
                    _target.WaveWeight = new WaveWeight(this, null);
                    waveFront.Clear();
                    _initializationTime = Environment.TickCount;
                }
            }
        }
        Node _target;

        public int InitializationTime { get => _initializationTime; }
        int _initializationTime;

        public bool IsTargetTo(Node node)
        {
            return _target != null && _target.Equals(node) && _initializationTime > 0;
        }

        public bool IsValid
        {
            get
            {
                return _target != null && _initializationTime > 0;
            }
            set
            {
                if(!value)
                {
                    _target = null;
                    WaveFront.Clear();
                    _initializationTime = 0;
                }
            }
        }

        public bool Validate(WaveWeight ww)
        {
            return ww != null && ww.Source == this && ww.InitializationTime == _initializationTime;
        }

        public LinkedList<Node> WaveFront { get => waveFront; }

        private LinkedList<Node> waveFront = new LinkedList<Node>();

        /// <summary>
        /// Добавление узла в упорядоченную очередь List
        /// в порядке возрастания WaveWeight
        /// </summary>
        /// <param name="list"></param>
        /// <param name="n"></param>
        /// <param name="comparer"></param>
        private void Enqueue(Node n)
        {
            LinkedListNode<Node> pos = waveFront.First;

            while (pos != null)
            {
                // Нужна ли эта проверка, ибо она не гарантирует что n будет представлена в очереди в единственном экземпляре
                if (Equals(n, pos.Value))
                    return;

                if (NodesWeightComparer.CompareNodesWeight(n, pos.Value) < 0)
                {
                    waveFront.AddBefore(pos, n);
                    return;
                }
                pos = pos.Next;
            }

            waveFront.AddLast(n);
        }

        /// <summary>
        /// Извлечение узла из упорядоченного списка
        /// </summary>
        /// <returns></returns>
        private Node Dequeue()
        {
            LinkedListNode<Node> n = waveFront.First;
            waveFront.RemoveFirst();
            return n.Value;
        }

        /// <summary>
        /// Формирование волны из <paramref name="endNode"/> к <paramref name="startNode"/>
        /// </summary>
        /// <param name="startNode"></param>
        /// <param name="endNode"></param>
        /// <returns></returns>
        public bool GenerateWave(Node startNode, Node endNode)
        {
            if (startNode is null || endNode is null)
            {
#if WAVE_DEBUG_LOG
                AStarLogger.WriteLine(LogType.Debug, $"{nameof(WaveSource)}.{nameof(GenerateWave)}: Некорректные исходные данные");
#endif
                return false;
            }

            Target = endNode;

            if (Validate(startNode.WaveWeight))
            {
#if WAVE_DEBUG_LOG
                AStarLogger.WriteLine(LogType.Debug, $"{nameof(WaveSource)}.{nameof(GenerateWave)}: Start{startNode} имеет волновой вес {startNode.WaveWeight}... Прерываем волну");
#endif
                return true;
            }

            bool pathFound = false;

            if (waveFront.Count == 0 || waveFront.First.Value.WaveWeight?.IsTargetTo(_target) != true)
            {
#if WAVE_DEBUG_LOG
                AStarLogger.WriteLine(LogType.Debug, $"{nameof(WaveSource)}.{nameof(GenerateWave)}: Инициализируем новую волну из End{endNode}");
#endif
                // Помещаем в очередь обработки конечный узел
                waveFront.Clear();

#if processingQueue_SortedSet
                    processingQueue.Add(EndNode); 
#else
                _target.WaveWeight = new WaveWeight(this, null);
                Enqueue(_target);
#endif
            }
#if WAVE_DEBUG_LOG
            else
            {
#if DETAIL_LOG
                AStarLogger.WriteLine(LogType.Debug, $"{nameof(WaveSource)}.{nameof(GenerateWave)}: Продолжаем кэшированну волну");
                AStarLogger.WriteLine(LogType.Debug, $"{nameof(WaveSource)}.{nameof(GenerateWave)}: Кэшированный фронт волны ({nameof(waveFront)})");
                foreach (var node in waveFront)
                {
                    AStarLogger.WriteLine(LogType.Debug, $"\t\t{node}\t|\t{node.WaveWeight}");
                }
#else
                    AStarLogger.WriteLine(LogType.Debug, $"{nameof(WaveSource)}.{nameof(GenerateWave)}: Продолжаем кэшированный волновой фронт, содержащий {waveFront.Count} вершин");
#endif
            }
#endif

            // Обсчитываем все узлы графа
            while (waveFront.Count > 0)
            {
                Node node = ProcessingFirst();

                if (Equals(node, startNode))
                {
                    pathFound = true;
#if !continue_waveSearch_after_reaching_startNode
#if WAVE_DEBUG_LOG
                    AStarLogger.WriteLine(LogType.Debug, $"{nameof(WaveSource)}.{nameof(GenerateWave)}: Путь найден. Достигнута вершина Start{startNode} | {startNode.WaveWeight} ... Завершаем поиск");
#if DETAIL_LOG
                    AStarLogger.WriteLine(LogType.Debug, $"{nameof(WaveSource)}.{nameof(GenerateWave)}: Фронт волны (ProcessingQueue):");
                    foreach (var n in waveFront)
                    {
                        AStarLogger.WriteLine(LogType.Debug, $"{nameof(WaveSource)}.{nameof(GenerateWave)}:\t{n}\t|\t{n.WaveWeight}");
                    }
#endif
#endif
                    break;
#else
#if WAVE_DEBUG_LOG
                        AStarLogger.WriteLine(LogType.Log, $"{nameof(WaveSource)}.{nameof(GenerateWave)}: Путь найден. Достигнута вершина Start{StartNode}... Продолжаем волну");
#endif
#endif
                }
            }
            return pathFound;
        }

        /// <summary>
        /// Проверка первой вершины из очереди (фронта волны), 
        /// просмотр её входящих ребер, присвоение "волновой оценки" вершине и 
        /// добавление в очередь обработки "входящих" вершин
        /// </summary>
        public Node ProcessingFirst()
        {
            Node node = Dequeue();

            if (node != null)
            {
                if (node.WaveWeight is null)
                    throw new Exception($"Node{node} не имеет волновой оценки");

#if DETAIL_LOG && WAVE_DEBUG_LOG
                AStarLogger.WriteLine(LogType.Debug, string.Concat(nameof(WaveSource), '.', nameof(ProcessingFirst), ": Обрабатываем Node", node));
#endif
                // Просмотр входящих ребер
                foreach (Arc inArc in node.IncomingArcs)
                {
                    if (inArc.Passable && inArc.StartNode.Passable)
                    {
#if DETAIL_LOG && WAVE_DEBUG_LOG
                        AStarLogger.WriteLine(LogType.Debug, string.Concat(nameof(WaveSource), '.', nameof(ProcessingFirst), ": \tАнализируем ребро ", inArc));
#endif
                        double weight = node.WaveWeight.Weight + inArc.Length;

                        bool inNodeTargetMatch = inArc.StartNode.WaveWeight != null && inArc.StartNode.WaveWeight.IsTargetTo(_target);
#if DETAIL_LOG && WAVE_DEBUG_LOG
                        AStarLogger.WriteLine(LogType.Debug, string.Concat(nameof(WaveSource), '.', nameof(ProcessingFirst), ": \t\t", inNodeTargetMatch ? "MATCH " : "MISMATCH ", inArc.StartNode.WaveWeight));
                        //AStarLogger.WriteLine(LogType.Debug, string.Concat(nameof(WaveSource), '.', nameof(ProcessingFirst), ":\t\tНовый волновой вес : ", weight.ToString("N2")));
#endif
                        if (inNodeTargetMatch)
                        {
                            // стартовая вершина входящего ребра уже имеет "волновой вес"
                            if (inArc.StartNode.WaveWeight.Weight > weight)
                            {
                                // производим переоценку, если "волновой вес" стартовой вершины ребра больше чем weight
                                // это значит что путь через текущую вершину node - короче
                                inArc.StartNode.WaveWeight.Arc = inArc;
#if DETAIL_LOG && WAVE_DEBUG_LOG
                                AStarLogger.WriteLine(LogType.Debug, string.Concat(nameof(WaveSource), '.', nameof(ProcessingFirst), ": \t\t\tИзменяем волновую оценку на ", inArc.StartNode.WaveWeight));
                                AStarLogger.WriteLine(LogType.Debug, string.Concat(nameof(WaveSource), '.', nameof(ProcessingFirst), ": \t\t\tПомещаем в очередь обработки Node", inArc.StartNode));
#endif
                                // добавляем стартовую вершину ребра в очередь, для обработки
                                Enqueue(inArc.StartNode);
                            }
#if DETAIL_LOG && WAVE_DEBUG_LOG
                            else
                            {
                                // В этом случае в из inArc.StartNode есть более короткий путь к endNode
                                AStarLogger.WriteLine(LogType.Debug, string.Concat(nameof(WaveSource), '.', nameof(ProcessingFirst), ": \t\t\tПропускаем Node", inArc.StartNode, " достижимую более коротким путем ", inArc.StartNode.WaveWeight.Weight.ToString("N2")));
                            }
#endif
                        }
                        else
                        {
                            // присваиваем "волновой вес" стартовой вершине ребра
                            inArc.StartNode.WaveWeight = new WaveWeight(this, inArc);
#if DETAIL_LOG && WAVE_DEBUG_LOG
                            AStarLogger.WriteLine(LogType.Debug, string.Concat(nameof(WaveSource), '.', nameof(ProcessingFirst), ": \t\t\tПрисваиваем волновую оценку ", inArc.StartNode.WaveWeight));
                            AStarLogger.WriteLine(LogType.Debug, string.Concat(nameof(WaveSource), '.', nameof(ProcessingFirst), ": \t\t\tПомещаем в очередь обработки Node", inArc.StartNode));
#endif
                            Enqueue(inArc.StartNode);
                        }
                    }
                }
            }

            return node;
        }

        public void ClearWave()
        {
            if (_graph != null)
            {
                foreach (Node n in _graph.Nodes)
                    n.WaveWeight = null;
#if WAVE_DEBUG_LOG
                AStarLogger.WriteLine(LogType.Log, $"{nameof(WaveSource)}.{nameof(ClearWave)}: {_graph.Nodes.Count} nodes processed");
#endif
            }
        }
    }

    /// <summary>
    /// Волновой вес вершины
    /// </summary>
    public class WaveWeight : IComparable<WaveWeight>
    {
        private WaveWeight() { }
        public WaveWeight(WaveSource s, Arc arc)
        {
            _source = s;
            _initializationTime = _source.InitializationTime;
            _arc = arc;
            //_target = _source.Target;

            if (_arc != null)
            {
                var endWW = _arc.EndNode.WaveWeight;
                if (endWW._source != _source)
                    throw new ArgumentException($"Несоответстви источника волны на конце ребра {arc}");
                _weight = endWW._weight + _arc.Length;
            }
            else _weight = 0;
        }

        public WaveSource Source { get => _source; }
        WaveSource _source;

        public double Weight { get => _weight;}
        private double _weight = double.MaxValue;

        public Arc Arc
        {
            get => _arc;
            set
            {
                if (value is null)
                    throw new ArgumentNullException(nameof(value));
                _arc = value;
                var endWW = _arc.EndNode.WaveWeight;
                if (endWW._source != _source)
                    throw new ArgumentException($"Несоответстви источника волны на конце ребра {value}");
                _weight = endWW._weight + _arc.Length;

            }
        }
        private Arc _arc = null;

        ///// <summary>
        ///// Конечная вершина, к которой стремится путь (источник волны)
        ///// </summary>
        //public Node Target => _target;
        //private readonly Node _target;

        public bool IsTargetTo(Node node)
        {
            return _source.IsTargetTo(node) && _source.InitializationTime == _initializationTime;
        }

#if false
        public bool IsValid => _source.Validate(this); 
#endif

        public int InitializationTime { get => _initializationTime;  }
        int _initializationTime = 0;

        public override string ToString()
        {
            if (!_source.Validate(this))
                return "INVALID";
            if (_arc is null)
                return $"{_weight:N2}:\t{_source.Target}";
            return $"{_weight:N2}:\t{_arc} ==> {_source.Target}";
        }

        public int CompareTo(WaveWeight other)
        {
            if (other is null)
                throw new ArgumentException("Недопустимое сравнение с NULL", nameof(other));
            if (ReferenceEquals(_source, other._source))
            {
                double w1 = _weight;
                double w2 = _weight;

                if (w1 < w2)
                    return -1;
                else if (w1 > w2)
                    return +1;
                //return Convert.ToInt32(w1 - w2); 
                return 0;
            }
            else throw new ArgumentException("Не сопадают источники волны");
        }
    }

    /// <summary>
    /// Функтор сравнения вершин по их волновому весу
    /// </summary>
    public class NodesWeightComparer : IComparer<Node>
    {
        /// <summary>
        /// Метод сравнение вершин по их волновому весу
        /// </summary>
        /// <param name="n1"></param>
        /// <param name="n2"></param>
        /// <returns></returns>
        public static int CompareNodesWeight(Node n1, Node n2)
        {
            if (n1 is null)
                throw new ArgumentNullException(nameof(n1));
            if (n2 is null)
                throw new ArgumentNullException(nameof(n2));

            WaveWeight ww1 = n1.WaveWeight;
            WaveWeight ww2 = n2.WaveWeight;

            return ww1.CompareTo(ww2);
        }

        public int Compare(Node n1, Node n2)
        {
            return CompareNodesWeight(n1, n2);
        }
    }
}
