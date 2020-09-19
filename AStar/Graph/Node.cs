using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml.Serialization;
using AStar.Search;
using AStar.Search.Wave;
using AStar.Tools;

namespace AStar
{
    [Serializable]
    public class Node
#if IEquatable
        : IEquatable<Node> 
#endif
    {
        public Node()
        {
            _IncomingArcs = new ArrayList();
            //incomingArcsWrapper = new ReadOnlyListWrapper<ArrayList>(_IncomingArcs);
            _OutgoingArcs = new ArrayList();
            //outgoingArcsWrapper = new ReadOnlyListWrapper<ArrayList>(_OutgoingArcs);
        }
        public Node(double PositionX, double PositionY, double PositionZ)
        {
            _Position = new Point3D(PositionX, PositionY, PositionZ);
#if NodeTags
            this._tags = new Dictionary<object, object>();
#endif
            _IncomingArcs = new ArrayList();
            //incomingArcsWrapper = new ReadOnlyListWrapper<ArrayList>(_IncomingArcs);
            _OutgoingArcs = new ArrayList();
            //outgoingArcsWrapper = new ReadOnlyListWrapper<ArrayList>(_OutgoingArcs);
        }

#if true
        public IList IncomingArcs => new ReadOnlyListWrapper<ArrayList>(_IncomingArcs);//incomingArcsWrapper.Rebase(_IncomingArcs);
        //[NonSerialized]
        //private ReadOnlyListWrapper<ArrayList> incomingArcsWrapper = new ReadOnlyListWrapper<ArrayList>();
#else
        public IEnumerable<Arc> IncomingArcs
        {
            get
            {
                foreach (Arc arc in _IncomingArcs)
                    yield return arc;
            }
        } 
#endif
        private ArrayList _IncomingArcs;

        public int IncomingArcsCount => _IncomingArcs.Count;

#if true
        public IList OutgoingArcs => new ReadOnlyListWrapper<ArrayList>(_OutgoingArcs); //outgoingArcsWrapper.Rebase(_OutgoingArcs);
        //[NonSerialized]
        //private ReadOnlyListWrapper<ArrayList> outgoingArcsWrapper = new ReadOnlyListWrapper<ArrayList>();
#else
        public IEnumerable<Arc> OutgoingArcs
        {
            get
            {
                foreach (Arc arc in _OutgoingArcs)
                    yield return arc;
            }
        } 
#endif
        private ArrayList _OutgoingArcs;

        public int OutgoingArcsCount => _OutgoingArcs.Count;

        public bool Passable
        {
            get => _Passable;
            set
            {
#if false
                foreach (Arc arc in _IncomingArcs)
                    arc.Passable = value;

                foreach (Arc arc2 in _OutgoingArcs)
                    arc2.Passable = value; 
#endif

                _Passable = value;
            }
        }
        private bool _Passable = true;

#if false
        /// <summary>
        /// Флаг, при установки которого вершина помечается некорректной и подлежит удалению
        /// </summary>
        [XmlIgnore]
        public bool Invalid
        {
            get => _Invalid;
            set => _Invalid = value;
        }
        [NonSerialized]
        private bool _Invalid = false; 
#endif

        #region Координаты
        public Point3D Position
        {
            get => _Position;
            set
            {
                _Position = value ?? throw new ArgumentNullException();

                foreach (Arc arc in _IncomingArcs)
                    arc.LengthUpdated = false;

                foreach (Arc arc2 in _OutgoingArcs)
                    arc2.LengthUpdated = false;
            }
        }
        private Point3D _Position = new Point3D(0, 0, 0);

        public double X => Position.X;

        public double Y => Position.Y;

        public double Z => Position.Z;

        public void ChangeXYZ(double x, double y, double z)
        {
            _Position.X = x;
            _Position.Y = y;
            _Position.Z = z;

            foreach (Arc arc in _IncomingArcs)
                arc.LengthUpdated = false;

            foreach (Arc arc2 in _OutgoingArcs)
                arc2.LengthUpdated = false;
        }
        #endregion


#if Original_AStar
        public Node[] AccessibleNodes
        {            
            get
            {
                Node[] array = new Node[_OutgoingArcs.Count];
                int num = 0;
                foreach (Arc arc in OutgoingArcs)
                    array[num++] = arc.EndNode;

                return array;
            } 
        }
#else
        public IEnumerable<Node> AccessibleNodes
        {
            get
            {
                foreach (Arc arc in _OutgoingArcs)
                    yield return arc.EndNode;
            }
        }
#endif

#if Original_AStar
        public Node[] AccessingNodes
        {
            get
            {
                Node[] array = new Node[_IncomingArcs.Count];
                int num = 0;
                foreach (Arc arc in this.IncomingArcs)
                    array[num++] = arc.StartNode;

                return array;
            } 
#else
        public IEnumerable<Node> AccessingNodes
        {
            get
            {
                foreach (Arc arc in _IncomingArcs)
                    yield return arc.StartNode;
            }
        }
#endif

#if Original_AStar
        public Node[] Molecule
        {
            get
            {
                int num = 1 + _OutgoingArcs.Count + _IncomingArcs.Count;
                Node[] array = new Node[num];
                array[0] = this;
                int num2 = 1;
                foreach (Arc arc in OutgoingArcs)
                    array[num2++] = arc.EndNode;

                foreach (Arc arc2 in IncomingArcs)
                    array[num2++] = arc2.StartNode;

                return array;
            }
        } 
#else
        public IEnumerable<Node> Molecule
        {
            get
            {
                foreach (Arc arc in _OutgoingArcs)
                    yield return arc.EndNode;

                foreach (Arc arc in IncomingArcs)
                    yield return arc.StartNode;

                yield return this;
            }
        }
#endif
        /// <summary>
        /// Поиска ребра, соединяющего с <paramref name="node"/>, а при его отсутствии - добавление нужного ребра
        /// </summary>
        /// <param name="node"></param>
        /// <returns></returns>
        public Arc ConnectTo(Node node, double weight = 1)
        {
            Arc arc = ArcGoingTo(node);
            if(arc is null)
                arc = new Arc() { StartNode = this, EndNode = node, Weight = weight };
            return arc;
        }

        /// <summary>
        /// Добавление ребра
        /// </summary>
        /// <param name="arc"></param>
        public bool Add(Arc arc)
        {
            if (arc.StartNode.Equals(this))
            {
                return _OutgoingArcs.AddUnique(arc) > 0;
            }
            else if (arc.EndNode.Equals(this))
               return  _IncomingArcs.AddUnique(arc) > 0;
            return false;
        }

        /// <summary>
        /// Удаление ребра
        /// </summary>
        /// <param name="arc"></param>
        public void Remove(Arc arc)
        {
            if (arc.StartNode.Equals(this))
                _IncomingArcs.Remove(arc);
            else _OutgoingArcs.Remove(this);
        }

        /// <summary>
        /// Изоляция вершины (удаление всех ребер)
        /// </summary>
        public void Isolate()
        {
            UntieIncomingArcs();
            UntieOutgoingArcs();
        }

        /// <summary>
        /// Удаление всех входящих ребер
        /// </summary>
        public void UntieIncomingArcs()
        {
            foreach (Arc arc in _IncomingArcs)
                arc.StartNode.Remove(arc);

            _IncomingArcs.Clear();
        }

        /// <summary>
        /// Удаление всех исходящих ребер
        /// </summary>
        public void UntieOutgoingArcs()
        {
            foreach (Arc arc in _OutgoingArcs)
                arc.EndNode.Remove(arc);

            _OutgoingArcs.Clear();
        }

#if false
        /// <summary>
        /// Удаление непроходимых ребер
        /// Метод является частью алгоритма сжатия графа
        /// </summary>
        internal void RemoveUnpassableArcs()
        {
            int lastFreeElement = 0;
            // уплотнение массива
            for (int i = 0; i < _IncomingArcs.Count; i++)
            {
                if (_IncomingArcs[i] is Arc arc
                    && arc.Passable)
                {
                    _IncomingArcs[lastFreeElement] = arc;
                    lastFreeElement++;
                }
            }
            // удаление "ненужных" ячеек в конце массива
            if (lastFreeElement < _IncomingArcs.Count)
                _IncomingArcs.RemoveRange(lastFreeElement, _IncomingArcs.Count - lastFreeElement);

            lastFreeElement = 0;
            for (int i = 0; i < _OutgoingArcs.Count; i++)
            {
                if (_OutgoingArcs[i] is Arc arc
                    && arc.Passable)
                {
                    _OutgoingArcs[lastFreeElement] = arc;
                    lastFreeElement++;
                }
            }
            if (lastFreeElement < _OutgoingArcs.Count)
                _OutgoingArcs.RemoveRange(lastFreeElement, _OutgoingArcs.Count - lastFreeElement);
        } 
#elif false
        internal ArrayList RemoveUnpassableAndDublicateArcs()
        {
            ArrayList arcsToRemove = new ArrayList();
            int lastFreeElement = 0;
            // уплотнение массива
            for (int i = 0; i < _IncomingArcs.Count; i++)
            {
                if (_IncomingArcs[i] is Arc arc)
                {
                    if (arc.Passable)
                    {
                        int dubleInd = -1;
                        for (int j = 0; j < lastFreeElement; j++)
                        {
                            if (_IncomingArcs[j] is Arc correctArc
                                && (ReferenceEquals(correctArc, arc) 
                                    || correctArc.StartNode.Equals(arc.StartNode)))
                            {
                                dubleInd = j;
                                break;
                            }
                        }
                        if (dubleInd < 0)
                        {
                            _IncomingArcs[lastFreeElement] = arc;
                            lastFreeElement++;
                            continue;
                        }
                    }
                    arcsToRemove.Add(arc);
                }
            }
            // удаление "ненужных" ячеек в конце массива
            if (lastFreeElement < _IncomingArcs.Count)
                _IncomingArcs.RemoveRange(lastFreeElement, _IncomingArcs.Count - lastFreeElement);

            lastFreeElement = 0;
            for (int i = 0; i < _OutgoingArcs.Count; i++)
            {
                if (_OutgoingArcs[i] is Arc arc)
                {
                    if (arc.Passable)
                    {
                        int dubleInd = -1;
                        for (int j = 0; j < lastFreeElement; j++)
                        {
                            if (_OutgoingArcs[j] is Arc correctArc
                                && (ReferenceEquals(correctArc, arc)
                                    || correctArc.EndNode.Equals(arc.StartNode)))
                            {
                                dubleInd = j;
                                break;
                            }
                        }
                        if (dubleInd < 0)
                        {
                            _OutgoingArcs[lastFreeElement] = arc;
                            lastFreeElement++;
                            continue;
                        }
                    }
                    arcsToRemove.Add(arc);
                }
            }
            if (lastFreeElement < _OutgoingArcs.Count)
                _OutgoingArcs.RemoveRange(lastFreeElement, _OutgoingArcs.Count - lastFreeElement);

            return arcsToRemove;
        }
#elif false
        internal int RemoveDublicateArcs()
        {
            int removedArcNum = 0;
            if (_Passable)
            {
                HashSet<Arc> arcSet = new HashSet<Arc>();
                foreach (Arc arc in _IncomingArcs)
                    if (arc.StartNode.Passable)
                        arcSet.Add(arc);

                if (arcSet.Count < _IncomingArcs.Count)
                {
                    int i = 0;
                    int num = _IncomingArcs.Count - arcSet.Count;
                    foreach (Arc arc in arcSet)
                    {
                        _IncomingArcs[i] = arc;
                    }
                    _IncomingArcs.RemoveRange(arcSet.Count, num);
                    removedArcNum = num;
                }

                arcSet.Clear();
                foreach (Arc arc in _OutgoingArcs)
                    if (arc.EndNode.Passable)
                        arcSet.Add(arc);

                if (arcSet.Count < _OutgoingArcs.Count)
                {
                    int i = 0;
                    int num = _OutgoingArcs.Count - arcSet.Count;
                    foreach (Arc arc in arcSet)
                    {
                        _OutgoingArcs[i] = arc;
                    }
                    _OutgoingArcs.RemoveRange(arcSet.Count, num);
                    removedArcNum += num;
                }
            }
            return removedArcNum;
        }
#endif

        /// <summary>
        /// Удаление списка ребер
        /// Метод является частью алгоритма сжатия графа
        /// </summary>
        internal void RemoveArcs(ArrayList ArcsToRemome)
        {
            int lastFreeElement = 0;
            // уплотнение массива
            for (int i = 0; i < _IncomingArcs.Count; i++)
            {
                if (!ArcsToRemome.Contains(_IncomingArcs[i]))
                {
                    _IncomingArcs[lastFreeElement] = _IncomingArcs[i];
                    lastFreeElement++;
                }
            }
            // удаление "ненужных" ячеек в конце массива
            if (lastFreeElement < _IncomingArcs.Count)
                _IncomingArcs.RemoveRange(lastFreeElement, _IncomingArcs.Count - lastFreeElement);

            lastFreeElement = 0;
            for (int i = 0; i < _OutgoingArcs.Count; i++)
            {
                if (!ArcsToRemome.Contains(_OutgoingArcs[i]))
                {
                    _OutgoingArcs[lastFreeElement] = _OutgoingArcs[i];
                    lastFreeElement++;
                }
            }
            if (lastFreeElement < _OutgoingArcs.Count)
                _OutgoingArcs.RemoveRange(lastFreeElement, _OutgoingArcs.Count - lastFreeElement);
        }

        public Arc ArcGoingTo(Node node)
        {
            if (node is null)
                throw new ArgumentNullException(nameof(node));

            foreach (Arc arc in _OutgoingArcs)
                if (node.Equals(arc.EndNode))
                    return arc;

            return null;
        }

        public Arc ArcComingFrom(Node node)
        {
            if (node is null)
                throw new ArgumentNullException(nameof(node));

            foreach (Arc arc in _IncomingArcs)
                if (Equals(arc.StartNode, node))
                    return arc;

            return null;
        }

        private void Invalidate()
        {
            foreach (Arc arc in _IncomingArcs)
                arc.LengthUpdated = false;
            foreach (Arc arc in _OutgoingArcs)
                arc.LengthUpdated = false;
        }

        public override string ToString()
        {
            return Position.ToString();
        }

        public override bool Equals(object O)
        {
            if (ReferenceEquals(this, O))
                return true;
            if (O is Node node)
                return _Position.Equals(node._Position);
            return false;
        }
#if IEquatable
        public bool Equals(Node n)
        {
            if (ReferenceEquals(this, n))
                return true;
            return n != null && _Position.Equals(n._Position);
        } 
#endif

        public object Clone()
        {
            return new Node(X, Y, Z)
            {
                _Passable = _Passable
            };
        }

        public override int GetHashCode()
        {
            return _Position.GetHashCode();
        }

        public static double EuclideanDistance(Node N1, Node N2)
        {
            return Math.Sqrt(SquareEuclideanDistance(N1, N2));
        }

        public static double SquareEuclideanDistance(Node N1, Node N2)
        {
            if (N1 is null || N2 is null)
                throw new ArgumentNullException();

            double num = N1.Position.X - N2.Position.X;
            double num2 = N1.Position.Y - N2.Position.Y;
            double num3 = N1.Position.Z - N2.Position.Z;
            return num * num + num2 * num2 + num3 * num3;
        }

        public static double ManhattanDistance(Node N1, Node N2)
        {
            if (N1 is null || N2 is null)
                throw new ArgumentNullException();

            double value = N1.Position.X - N2.Position.X;
            double value2 = N1.Position.Y - N2.Position.Y;
            double value3 = N1.Position.Z - N2.Position.Z;
            return Math.Abs(value) + Math.Abs(value2) + Math.Abs(value3);
        }

        public static double MaxDistanceAlongAxis(Node N1, Node N2)
        {
            if (N1 is null || N2 is null)
                throw new ArgumentNullException();

            double val = Math.Abs(N1.Position.X - N2.Position.X);
            double val2 = Math.Abs(N1.Position.Y - N2.Position.Y);
            double val3 = Math.Abs(N1.Position.Z - N2.Position.Z);
            return Math.Max(val, Math.Max(val2, val3));
        }

        public static void BoundingBox(IList NodesGroup, out double[] MinPoint, out double[] MaxPoint)
        {
            if (NodesGroup.Count == 0)
                throw new ArgumentException("The list of nodes is empty.");

            if (!(NodesGroup[0] is Node))
                throw new ArgumentException("The list must only contain elements of type Node.");

            MinPoint = new double[3] {double.MaxValue, double.MaxValue, double.MaxValue};
            MaxPoint = new double[3] {double.MinValue, double.MinValue, double.MinValue};
            
            foreach (Node node2 in NodesGroup)
            {
                if (MinPoint[0] > node2._Position[0])
                    MinPoint[0] = node2._Position[0];
                if (MinPoint[1] > node2._Position[1])
                    MinPoint[1] = node2._Position[1];
                if (MinPoint[2] > node2._Position[2])
                    MinPoint[2] = node2._Position[2];

                if (MaxPoint[0] < node2._Position[0])
                    MaxPoint[0] = node2._Position[0];
                if (MaxPoint[1] < node2._Position[1])
                    MaxPoint[1] = node2._Position[1];
                if (MaxPoint[2] < node2._Position[2])
                    MaxPoint[2] = node2._Position[2];
            }
        }

#if NodeTags
        /// <summary>
        /// Список меток
        /// </summary>
        [XmlIgnore]
        public Dictionary<object, object> Tags
        {
            get
            {
                if (_tags == null)
                    _tags = new Dictionary<object, object>();
                return _tags;
            }
        }
        [NonSerialized]
        private Dictionary<object, object> _tags = new Dictionary<object, object>(); 
#endif

        [XmlIgnore]
        public WaveSource.WaveWeight WaveWeight => _waveWeight;
        [NonSerialized]
        private WaveSource.WaveWeight _waveWeight = null;

        public WaveSource.WaveWeight AttachTo(WaveSource source)
        {
            if (_waveWeight is null || _waveWeight.Source != source)
                _waveWeight = WaveSource.WaveWeight.Make(source);
            
            return _waveWeight;
        }
    }
}
