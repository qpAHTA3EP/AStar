using System;
using System.Collections.Generic;
using System.Linq;
using System.Management.Instrumentation;
using System.Text;
using MyNW.Classes;

namespace AStar.Search.Wave
{
    public class WaveSource
    {
        /// <summary>
        /// Максимальное количество "сохраняемых" волн
        /// </summary>
        public static readonly int WavesLimit = 16;

        public WaveSource()
        {
            WaveSources = new WaveSourceSlot[WavesLimit];
        }

        /// <summary>
        /// Индекс текущей волны в кэше
        /// </summary>
        public int CurrentSlotIndex => currentSlotIndex;
        protected int currentSlotIndex = 0;

        /// <summary>
        /// Кэш волн
        /// </summary>
        protected WaveSourceSlot[] WaveSources;

        /// <summary>
        /// Граф, ассоциированный с текущей волной
        /// </summary>
        public Graph Graph => WaveSources[currentSlotIndex]?.Graph;

        /// <summary>
        /// Вершина-источник текущей волны
        /// </summary>
        public Node Target => WaveSources[currentSlotIndex]?.Target;

        /// <summary>
        /// Метка времени инициализации текущей волны
        /// </summary>
        protected int InitTime => WaveSources[currentSlotIndex]?.InitTime ?? 0;

        /// <summary>
        /// Сопоставление вершины с источником текущей волны
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        protected bool IsTargetTo(Node node)
        {
            var source = WaveSources[currentSlotIndex];
            return source != null && source.Target != null && source.Target.Equals(node) && source.InitTime > 0;
        }

        /// <summary>
        /// Зафиксировать (присоединить) источник волны к целевой вершине <paramref name="n"/> в графе <paramref name="g"/>
        /// </summary>
        /// <param name="g"></param>
        /// <param name="n"></param>
        public void AttachTo(Graph g, Node n)
        {
            if (g is null)
                throw new ArgumentNullException(nameof(g));
            if (n is null)
                throw new ArgumentNullException(nameof(n));

#if WAVESOURCE_DEBUG_LOG
            AStarLogger.WriteLine(LogType.Debug, $"{nameof(WaveSource)}.{nameof(AttachTo)}: Привязываем источник волны к вершине {n}");
#endif
            var source = WaveSources[currentSlotIndex];
            if (source is null)
            {
#if WAVESOURCE_DEBUG_LOG
                AStarLogger.WriteLine(LogType.Debug, $"{nameof(WaveSource)}.{nameof(AttachTo)}: Инициализируем источник #{currentSlotIndex}");
#endif
                WaveSources[currentSlotIndex] = new WaveSourceSlot(this, g, n);
                return;
            }

            WaveSourceSlot slot;
            if (!source.IsTargetTo(g, n))
            {
                // Ищем подходящий слот для волны
                // Если нет слота, соответствующего графу и вершине,
                // тогда выбираем слот с минимальным количеством использований 
                int invalidSlotInd = -1;
#if !UsagePerTick
                // выбираем слот с минимальным количеством использований в единицу времени
                // UsageNumber / (TickCount - InitTime)
                double worseUsagePerTick = double.MaxValue;
                int worseSlotInd = -1;
                int nowTicks = Environment.TickCount; 
#else
                uint usageNum = int.MaxValue;
                int minUsageSlotInd = -1;
                int oldestSlotInd = -1;
                int minInitTime = int.MaxValue;
#endif

                for (int i = 0; i < WavesLimit; i++)
                {
                    slot = WaveSources[i];
                    if (slot is null || slot.InitTime == 0)
                    {
                        invalidSlotInd = i;
                        continue;
                    }
                    if (slot.IsTargetTo(g, n))
                        {
                            currentSlotIndex = i;
#if WAVESOURCE_DEBUG_LOG
                        AStarLogger.WriteLine(LogType.Debug, $"{nameof(WaveSource)}.{nameof(AttachTo)}: Обнаружен подходящий источник #{currentSlotIndex}");
                        AStarLogger.WriteLine(LogType.Debug, $"\tTarget{slot.Target}\tUsageNumber: {slot.UsageNumber}\t{nameof(InitTime)}: {slot.InitTime}");
#endif
                        return;
                    }
                    else
                    {

#if !UsagePerTick       
                        int slotTicks = nowTicks - slot.InitTime;
                        double slotUsagePerTick = slotTicks > 0 ?
                            slot.UsageNumber / slotTicks : slot.UsageNumber;
                        if(worseUsagePerTick > slotUsagePerTick)
                        {
                            worseUsagePerTick = slotUsagePerTick;
                            worseSlotInd = i;
                            continue;
                        }
#else
                        if (slot.UsageNumber <= usageNum)
                        {
                            minUsageSlotInd = i;
                            usageNum = slot.UsageNumber;
                        }

                        if (minInitTime > slot.InitTime)
                        {
                            oldestSlotInd = i;
                            minInitTime = slot.InitTime;
                        } 
#endif
                    }
                }

                if (invalidSlotInd >= 0)
                    currentSlotIndex = invalidSlotInd;
                else if (worseSlotInd >= 0)
                    currentSlotIndex = worseSlotInd;
                slot = WaveSources[currentSlotIndex];
                if (slot is null)
                {
#if WAVESOURCE_DEBUG_LOG
                    AStarLogger.WriteLine(LogType.Debug, $"{nameof(WaveSource)}.{nameof(AttachTo)}: Создаем новый источник #{currentSlotIndex}");
#endif
                    WaveSources[currentSlotIndex] = new WaveSourceSlot(this, g, n);
                }
                else
                {
#if WAVESOURCE_DEBUG_LOG
                    AStarLogger.WriteLine(LogType.Debug, $"{nameof(WaveSource)}.{nameof(AttachTo)}: Меняем привязку источника #{currentSlotIndex}:");
                    AStarLogger.WriteLine(LogType.Debug, $"\tСтарая\tTarget{slot.Target}\tUsageNumber: {slot.UsageNumber}\t{nameof(InitTime)}: {slot.InitTime}");
#endif
                    slot.AttachTo(this, g, n);
#if WAVESOURCE_DEBUG_LOG
                    AStarLogger.WriteLine(LogType.Debug, $"\tНовая\tTarget{slot.Target}\tUsageNumber: {slot.UsageNumber}\t{nameof(InitTime)}: {slot.InitTime}");
#endif
                }
            }
#if WAVESOURCE_DEBUG_LOG
            else
            {
                slot = WaveSources[currentSlotIndex];
                AStarLogger.WriteLine(LogType.Debug,
                    $"{nameof(WaveSource)}.{nameof(AttachTo)}: Используем источник #{currentSlotIndex}:");
                AStarLogger.WriteLine(LogType.Debug,
                    $"\tTarget{slot.Target}\tUsageNumber: {slot.UsageNumber}\t{nameof(InitTime)}: {slot.InitTime}");
            }
#endif
        }

#if WaveSource_Validate
        /// <summary>
        /// Сопоставление волнового веса <paramref name="ww"/> с текущей волной
        /// </summary>
        /// <param name="ww"></param>
        /// <returns></returns>
        internal bool Validate(WaveWeight ww)
        {
            var source = WaveSources[currentSlotIndex];
            return ww != null && source != null && ww.Source == this && source.InitTime > 0 && ww.InitTime == source.InitTime;
        } 
#endif

        /// <summary>
        /// Формирование волны из <see cref="Target"/> к <paramref name="startNode"/>
        /// </summary>
        /// <param name="startNode"></param>
        /// <returns></returns>
        public bool GenerateWave(Node startNode)
        {
            if (Target is null)
                throw new Exception($"Не задана целевая вершина {nameof(Target)} (используйте метод {nameof(WaveSource)}.{nameof(AttachTo)})");

            if (startNode is null)
            {
#if WAVESOURCE_DEBUG_LOG
                AStarLogger.WriteLine(LogType.Debug, $"{nameof(WaveSource)}.{nameof(GenerateWave)}: Некорректные исходные данные");
#endif
                return false;
            }

#if WaveSource_Validate
            if (Validate(startNode.WaveWeight))  
#else
            if (startNode.WaveWeight?.IsTargetTo(Target) == true)
#endif
            {
#if WAVESOURCE_DEBUG_LOG
                AStarLogger.WriteLine(LogType.Debug, $"{nameof(WaveSource)}.{nameof(GenerateWave)}: Start{startNode} имеет волновой вес {startNode.WaveWeight}... Прерываем волну");
#endif
                return true;
            }

            bool pathFound = false;

            var source = WaveSources[currentSlotIndex];
#if WAVESOURCE_DEBUG_LOG
            AStarLogger.WriteLine(LogType.Debug, $"{nameof(WaveSource)}.{nameof(GenerateWave)}: Используется источник #{currentSlotIndex}, имеющий параметры привязки:");
            AStarLogger.WriteLine(LogType.Debug, $"\tTarget{source.Target}\tUsageNumber: {source.UsageNumber}\t{nameof(InitTime)}: {source.InitTime}");
#endif

            if (source.FrontCount == 0)
            {
#if WAVESOURCE_DEBUG_LOG
                AStarLogger.WriteLine(LogType.Debug, $"{nameof(WaveSource)}.{nameof(GenerateWave)}: Инициализируем новую волну из End{source.Target}");
#endif
                // Помещаем в очередь обработки конечный узел
#if processingQueue_SortedSet
                    processingQueue.Add(EndNode); 
#else
                source.Initialize();
#endif
            }
#if WAVESOURCE_DEBUG_LOG
            else
            {
#if DETAIL_LOG
                AStarLogger.WriteLine(LogType.Debug, $"{nameof(WaveSource)}.{nameof(GenerateWave)}: Продолжаем кэшированну волну");
                AStarLogger.WriteLine(LogType.Debug, $"{nameof(WaveSource)}.{nameof(GenerateWave)}: Кэшированный фронт волны ({nameof(source.WaveFront)})");
                foreach (var node in source.WaveFront)
                {
                    AStarLogger.WriteLine(LogType.Debug, $"\t\t{node}\t|\t{node.WaveWeight}");
                }
#else
                    AStarLogger.WriteLine(LogType.Debug, $"{nameof(WaveSource)}.{nameof(GenerateWave)}: Продолжаем кэшированный волновой фронт, содержащий {source.FrontCount} вершин");
#endif
            }
#endif

            // обсчитываем волну
            while (source.FrontCount > 0)
            {
                Node node = ProcessingFirst(source);

                if (Equals(node, startNode))
                {
                    pathFound = true;
#if !continue_waveSearch_after_reaching_startNode
#if WAVESOURCE_DEBUG_LOG
                    AStarLogger.WriteLine(LogType.Debug, $"{nameof(WaveSource)}.{nameof(GenerateWave)}: Путь найден. Достигнута вершина Start{startNode} | {startNode.WaveWeight} ... Завершаем поиск");
#if DETAIL_LOG
                    AStarLogger.WriteLine(LogType.Debug, $"{nameof(WaveSource)}.{nameof(GenerateWave)}: Фронт волны (WaveFront):");
                    foreach (var n in source.WaveFront)
                    {
                        AStarLogger.WriteLine(LogType.Debug, $"\t\t{n}\t|\t{n.WaveWeight}");
                    }
#endif
#endif
                    break;
#else
#if WAVESOURCE_DEBUG_LOG
                        AStarLogger.WriteLine(LogType.Log, $"{nameof(WaveSource)}.{nameof(GenerateWave)}: Путь найден. Достигнута вершина Start{StartNode}... Продолжаем волну");
#endif
#endif
                }
            }
            return pathFound;
        }

        /// <summary>
        /// Проверка первой вершины из очереди обработки (фронта волны), 
        /// просмотр её входящих ребер, присвоение "волновой оценки" вершине и 
        /// добавление в очередь обработки "входящих" вершин
        /// </summary>
        private Node ProcessingFirst(WaveSourceSlot source)
        {
            Node node = source.Dequeue();

            if (node != null)
            {
                if (node.WaveWeight is null)
                    throw new Exception($"Node{node} не имеет волновой оценки");

#if DETAIL_LOG && WAVESOURCE_DEBUG_LOG
                AStarLogger.WriteLine(LogType.Debug, string.Concat(nameof(WaveSource), '.', nameof(ProcessingFirst), ": Обрабатываем Node", node));
#endif
                // Просмотр входящих ребер
                foreach (Arc inArc in node.IncomingArcs)
                {
                    if (inArc.Passable && inArc.StartNode.Passable)
                    {
#if DETAIL_LOG && WAVESOURCE_DEBUG_LOG
                        AStarLogger.WriteLine(LogType.Debug, string.Concat(nameof(WaveSource), '.', nameof(ProcessingFirst), ": \tАнализируем ребро ", inArc));
#endif
                        double weight = node.WaveWeight.Weight + inArc.Length;

                        bool inNodeTargetMatch = inArc.StartNode.WaveWeight != null && inArc.StartNode.WaveWeight.IsTargetTo(Target);
#if DETAIL_LOG && WAVESOURCE_DEBUG_LOG
                        AStarLogger.WriteLine(LogType.Debug, string.Concat(nameof(WaveSource), '.', nameof(ProcessingFirst), ": \t\t", inNodeTargetMatch ? "MATCH " : "MISMATCH ", inArc.StartNode.WaveWeight));
#endif
                        if (inNodeTargetMatch)
                        {
                            // стартовая вершина входящего ребра уже имеет "волновой вес"
                            if (inArc.StartNode.WaveWeight.Weight > weight)
                            {
                                // производим переоценку, если "волновой вес" стартовой вершины ребра больше чем weight
                                // это значит что путь через текущую вершину node - короче
                                inArc.StartNode.WaveWeight.Arc = inArc;
#if DETAIL_LOG && WAVESOURCE_DEBUG_LOG
                                AStarLogger.WriteLine(LogType.Debug, string.Concat(nameof(WaveSource), '.', nameof(ProcessingFirst), ": \t\t\tИзменяем волновую оценку на ", inArc.StartNode.WaveWeight));
                                AStarLogger.WriteLine(LogType.Debug, string.Concat(nameof(WaveSource), '.', nameof(ProcessingFirst), ": \t\t\tПомещаем в очередь обработки Node", inArc.StartNode));
#endif
                                // добавляем стартовую вершину ребра в очередь, для обработки
                                source.Enqueue(inArc.StartNode);
                            }
#if DETAIL_LOG && WAVESOURCE_DEBUG_LOG
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
                            inArc.StartNode.AttachTo(this).Arc = inArc;
#if DETAIL_LOG && WAVESOURCE_DEBUG_LOG
                            AStarLogger.WriteLine(LogType.Debug, string.Concat(nameof(WaveSource), '.', nameof(ProcessingFirst), ": \t\t\tПрисваиваем волновую оценку ", inArc.StartNode.WaveWeight));
                            AStarLogger.WriteLine(LogType.Debug, string.Concat(nameof(WaveSource), '.', nameof(ProcessingFirst), ": \t\t\tПомещаем в очередь обработки Node", inArc.StartNode));
#endif
                            source.Enqueue(inArc.StartNode);
                        }
                    }
                }
            }

            return node;
        }

        /// <summary>
        /// Удаление текущей волны
        /// </summary>
        public void ClearWave()
        {
            WaveSources[currentSlotIndex]?.ClearWave();
        }

        public uint IncreaseUsage()
        {
            var source = WaveSources[currentSlotIndex];
            return source is null ? 0 : ++source.UsageNumber;
        }

        /// <summary>
        /// Идентификатор волны, связывающие кэш с вершиной-источником <see cref="Target"/>
        /// </summary>
        protected class WaveSourceSlot
        {
            public WaveSourceSlot(WaveSource s, Graph g, Node n)
            {
                _source = s;
                _graph = g;
                _target = n;
                _initTime = 0;
            }

            public WaveSource Source  => _source;
            private readonly WaveSource _source;

            /// <summary>
            /// Граф в котором производится поиск пути
            /// </summary>
            public Graph Graph => _graph;
            Graph _graph;

            /// <summary>
            /// Вершина-источник волны
            /// </summary>
            public Node Target
            {
                get =>_target;
                private set
                {
                    if(!Equals(value, _target) || !Validate(value.WaveWeight))
                    {
                        _initTime = Environment.TickCount;
                        _target = value;
                        _target.AttachTo(_source).Arc = null;
                        _waveFront.Clear();
                        _usageNumber = 0;
                    }
                }
            }
            Node _target;

            /// <summary>
            /// Количество использований источника волны,
            /// то есть сколько раз производился поиск пути к вершине <see cref="Target"/>
            /// </summary>
            public uint UsageNumber
            {
                get => _initTime > 0 ? _usageNumber : 0;
                set => _usageNumber = value;
            }
            private uint _usageNumber;

            public bool IsTargetTo(Graph g, Node n)
            {
                return _graph != null && _graph == g && _target != null && _target.Equals(n) && _initTime > 0;
            }

            internal void AttachTo(WaveSource s, Graph g, Node n)
            {
                if (_source != s || _graph != g || _target != n)
                {
                    _initTime = Environment.TickCount;
                    _usageNumber = 0;
                    _waveFront.Clear();
                    _graph = g;
                    _target = n;
                    _target.AttachTo(_source).Arc = null;
                }
            }
#if false
            internal void Detach()
            {
                _graph = null;
                _target = null;
                _usageNumber = 0;
                _initTime = 0;
                _waveFront.Clear();
            } 
#endif

            public int InitTime => _initTime;
            private int _initTime;

            public IEnumerable<Node> WaveFront => _waveFront;
            private readonly LinkedList<Node> _waveFront = new LinkedList<Node>();

            public int FrontCount => _waveFront.Count;

            public void Initialize()
            {
                if (_target is null)
                    throw new Exception($"{nameof(Target)} не может быть NULL. (используйте метод {nameof(WaveSourceSlot)}.{nameof(AttachTo)})");
                _waveFront.Clear();
                _initTime = Environment.TickCount;
                _usageNumber = 1;
                _target.AttachTo(_source).Arc = null;
                Enqueue(_target);
            }

            /// <summary>
            /// Добавление вершины в очередь обработки (волновой фронт)
            /// в порядке возрастания WaveWeight
            /// </summary>
            /// <param name="n"></param>
            public void Enqueue(Node n)
            {
                var pos =_waveFront.First;

                while (pos != null)
                {
                    // Нужна ли эта проверка, ибо она не гарантирует что n будет представлена в очереди в единственном экземпляре
                    if (Equals(n, pos.Value))
                        return;

                    if (NodesWeightComparer.CompareNodesWeight(n, pos.Value) < 0)
                    {
                        _waveFront.AddBefore(pos, n);
                        return;
                    }
                    pos = pos.Next;
                }

                _waveFront.AddLast(n);
            }

            /// <summary>
            /// Извлечение узла из очереди обработки (волнового фронта)
            /// </summary>
            /// <returns></returns>
            public Node Dequeue()
            {
                var n = _waveFront.First;
                _waveFront.RemoveFirst();
                return n.Value;
            }

            public bool Validate(WaveWeight ww)
            {
                return ww != null && ww.Source == _source && ww.InitTime == _initTime;
            }

            public void ClearWave()
            {
#if false
                //TODO: Попробовать без ClearWave()
                if (graph != null)
                {
                    foreach (Node n in graph.Nodes)
                        n.WaveWeight?.Reset();
#if WAVESOURCE_DEBUG_LOG
                    AStarLogger.WriteLine(LogType.Log, $"{nameof(WaveSource)}.{nameof(ClearWave)}: {graph.Nodes.Count} nodes processed");
#endif
                } 
#endif
                _waveFront.Clear();
                _usageNumber = 0;
                _initTime = 0;
            }

            public override string ToString()
            {
                return $"Target{_target}\tUsage: {_usageNumber}\tInitTime: {_initTime}";
            }
        }

        /// <summary>
        /// Волновой вес вершины
        /// </summary>
        public class WaveWeight : IComparable<WaveWeight>
        {
            private WaveWeight(WaveSource s)
            {
                _source = s;

                _weights = new double[WavesLimit];
                for(int i = 0; i < WavesLimit; i++)
                    _weights[i] = double.MaxValue;
                _arcs = new Arc[WavesLimit];
                _initTimes = new int[WavesLimit];
                //_initTimes[_source.currentSlotIndex] = _source.InitTime;
            }
#if false
            private WaveWeight(WaveSource s, Arc arc)
            {
                _source = s;

                _initTimes = new int[WavesLimit];
                _initTimes[_source.currentSlotIndex] = _source.InitTime;

                _weights = new double[WavesLimit];
                for (int i = 0; i < WavesLimit; i++)
                    _weights[i] = double.MaxValue;

                _arcs = new Arc[WavesLimit];
                _arcs[_source.currentSlotIndex] = arc;

                if (arc != null)
                {
                    var endWW = arc.EndNode.AttachTo(_source);
                    if (endWW._source != _source)
                        throw new ArgumentException($"Несоответствие источника волны на конце ребра {arc}");
                    _weights[_source.currentSlotIndex] = endWW.Weight + _arcs.Length;
                }
                else _weights[_source.currentSlotIndex] = 0;
            } 
#endif

            public static WaveWeight Make(WaveSource source)
            {
                return new WaveWeight(source);
            }
            /// <summary>
            /// Источник волн
            /// </summary>
            public WaveSource Source => _source; 
            private readonly WaveSource _source;

            /// <summary>
            /// Вес текущей волны
            /// </summary>
            public double Weight => _weights[_source.currentSlotIndex];
            private readonly double[] _weights;

            /// <summary>
            /// Ребро по которому "пришла волна"
            /// </summary>
            public Arc Arc
            {
                get => _arcs[_source.currentSlotIndex];
                set
                {
                    _arcs[_source.currentSlotIndex] = value;
                    _initTimes[_source.currentSlotIndex] = _source.InitTime;
                    if (value is null)
                        _weights[_source.currentSlotIndex] = 0;
                    else if (value.EndNode.WaveWeight is null)
                        _weights[_source.currentSlotIndex] = double.MaxValue;
                    else _weights[_source.currentSlotIndex] = value.EndNode.WaveWeight.Weight + _arcs.Length;
                }
            }
            private readonly Arc[] _arcs = null;

            /// <summary>
            /// Сопоставление вершины с вершиной-источником волны
            /// </summary>
            /// <param name="node"></param>
            /// <returns></returns>
            public bool IsTargetTo(Node node)
            {
                return _source.IsTargetTo(node) && _source.InitTime == _initTimes[_source.currentSlotIndex];
            }

            public int InitTime  => _initTimes[_source.currentSlotIndex];
            private readonly int[] _initTimes;

            public override string ToString()
            {
                return string.Concat(
#if WaveSource_Validate
                                     !_source.Validate(this) ? "INVALID\t|\t" : string.Empty,  
#endif
                                     Weight.ToString("N2"), "\t|\t",
                                     InitTime, "\t|\t", 
                                     _arcs is null ? string.Empty : string.Concat(_arcs[_source.currentSlotIndex], " ==> "),
                                     _source.Target);
            }

            public int CompareTo(WaveWeight other)
            {
                if (other is null)
                    throw new ArgumentException("Недопустимое сравнение с NULL", nameof(other));
                if (ReferenceEquals(_source, other._source))
                {
                    double w1 = Weight;
                    double w2 = other.Weight;

                    if (w1 < w2)
                        return -1;
                    if (w1 > w2)
                        return +1;

                    return 0;
                }
                throw new ArgumentException("Не совпадают источники волны");
            }

            public void Reset()
            {
                _arcs[_source.currentSlotIndex] = null;
                _weights[_source.currentSlotIndex] = double.MaxValue;
                _initTimes[_source.currentSlotIndex] = 0;
            }

#if false
            public WaveWeight AttachTo(WaveSource s)
            {
                if (s != this.s)
                {
                    this.s = s;
                    for (int i = 0; i < WavesLimit; i++)
                    {
                        _arcs[i] = null;
                        _weights[i] = double.MaxValue;
                        _initTimes[i] = 0;
                    }
                }

                return this;
            } 
#endif
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

            var ww1 = n1.WaveWeight;
            var ww2 = n2.WaveWeight;

            return ww1.CompareTo(ww2);
        }

        public int Compare(Node n1, Node n2)
        {
            return CompareNodesWeight(n1, n2);
        }
    }
}
