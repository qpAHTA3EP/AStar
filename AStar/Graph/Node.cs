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
    public class Node : IEquatable<Node> 
    {
        public Node()
        {
            _IncomingArcs = new ArrayList();
            _OutgoingArcs = new ArrayList();
        }
        public Node(double PositionX, double PositionY, double PositionZ)
        {
            _Position = new Point3D(PositionX, PositionY, PositionZ);
#if NodeTags
            this._tags = new Dictionary<object, object>();
#endif
            _IncomingArcs = new ArrayList();
            _OutgoingArcs = new ArrayList();
        }

#if true
        public IList IncomingArcs => _incomingArcsWrapper ?? (_incomingArcsWrapper = new ReadOnlyListWrapper<ArrayList>(_IncomingArcs));
        [NonSerialized]
        ReadOnlyListWrapper<ArrayList> _incomingArcsWrapper;
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
        public IList OutgoingArcs => _outgoingArcsWrapper ?? (_outgoingArcsWrapper = new ReadOnlyListWrapper<ArrayList>(_OutgoingArcs));
        [NonSerialized]
        ReadOnlyListWrapper<ArrayList> _outgoingArcsWrapper;
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
        public void Move(double dx, double dy, double dz)
        {
            _Position.X += dx;
            _Position.Y += dy;
            _Position.Z += dz;

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
                    if (arc.Passable)
                        yield return arc.EndNode;

                foreach (Arc arc in IncomingArcs)
                    if(arc.Passable)
                        yield return arc.StartNode;

                yield return this;
            }
        }
#endif
        /// <summary>
        /// Поиска ребра, соединяющего с <paramref name="node"/>, а при его отсутствии - добавление нужного ребра
        /// </summary>
        public bool ConnectTo(Node node, double weight, out Arc arc)
        {
            bool added;
            arc = ArcGoingTo(node);
            if (arc is null)
            {
                arc = new Arc() { StartNode = this, EndNode = node, Weight = weight };
                added = _OutgoingArcs.Add(arc) >= 0;
            }
            else
            {
                added = arc.Disabled;
                arc.Disabled = false;
            }
            return added;
        }

        /// <summary>
        /// Добавление ребра <paramref name="arc"/>
        /// </summary>
        public bool Add(Arc arc)
        {
            if (arc.StartNode.Equals(this))
                return _OutgoingArcs.AddUnique(arc) > 0;
            else if (arc.EndNode.Equals(this))
               return  _IncomingArcs.AddUnique(arc) > 0;
            return false;
        }

        /// <summary>
        /// Удаление ребра <paramref name="arc"/>
        /// </summary>
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

        /// <summary>
        /// Удаление списка ребер <paramref name="arcsToRemome"/>
        /// Метод является частью алгоритма сжатия графа
        /// </summary>
        internal void RemoveArcs(ArrayList arcsToRemome)
        {
            int lastFreeElement = 0;
            // уплотнение массива
            for (int i = 0; i < _IncomingArcs.Count; i++)
            {
                if (!arcsToRemome.Contains(_IncomingArcs[i]))
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
                if (!arcsToRemome.Contains(_OutgoingArcs[i]))
                {
                    _OutgoingArcs[lastFreeElement] = _OutgoingArcs[i];
                    lastFreeElement++;
                }
            }
            if (lastFreeElement < _OutgoingArcs.Count)
                _OutgoingArcs.RemoveRange(lastFreeElement, _OutgoingArcs.Count - lastFreeElement);
        }

        /// <summary>
        /// Поиск исходящего ребра к вершине <paramref name="node"/>
        /// </summary>
        public Arc ArcGoingTo(Node node)
        {
            if (node is null)
                throw new ArgumentNullException(nameof(node));

            foreach (Arc arc in _OutgoingArcs)
                if (node.Equals(arc.EndNode))
                    return arc;
            
            return null;
        }

        /// <summary>
        /// Поиск входящего ребра из вершины <paramref name="node"/>
        /// </summary>
        public Arc ArcComingFrom(Node node)
        {
            if (node is null)
                throw new ArgumentNullException(nameof(node));

            foreach (Arc arc in _IncomingArcs)
                if (Equals(arc.StartNode, node))
                    return arc;

            return null;
        }

        /// <summary>
        /// Сброс предварительно рассчитанной длины всем связанным ребрам
        /// При считывании длина ребра будет вычислена заново
        /// </summary>
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
        public bool Equals(Node n)
        {
            if (ReferenceEquals(this, n))
                return true;
            return n != null && _Position.Equals(n._Position);
        } 

        /// <summary>
        /// Создание копии вершины (без ребер)
        /// </summary>
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

        /// <summary>
        /// Вычисление Евклидового расстояния между вершинами <paramref name="N1"/> и <paramref name="N2"/>
        /// </summary>
        public static double EuclideanDistance(Node N1, Node N2)
        {
            return Math.Sqrt(SquareEuclideanDistance(N1, N2));
        }

        /// <summary>
        /// Вычисление квадрата Евклидового расстояния между вершинами <paramref name="N1"/> и <paramref name="N2"/>
        /// </summary>
        public static double SquareEuclideanDistance(Node N1, Node N2)
        {
            if (N1 is null || N2 is null)
                throw new ArgumentNullException();

            double num = N1.Position.X - N2.Position.X;
            double num2 = N1.Position.Y - N2.Position.Y;
            double num3 = N1.Position.Z - N2.Position.Z;
            return num * num + num2 * num2 + num3 * num3;
        }

        /// <summary>
        /// Вычисление Манхетоннового расстояния между вершинами <paramref name="N1"/> и <paramref name="N2"/>
        /// Сумма расстояний между точками вдоль осей Ox, Oy, Oz
        /// </summary>
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

        /// <summary>
        /// Вычисление параллелипипеда, вмещающего все вершины <paramref name="nodesGroup"/>
        /// Парраллелипипед ограничен задается точками <paramref name="minPoint"/> и <paramref name="maxPoint"/>
        /// </summary>
        public static void BoundingBox(IList nodesGroup, out double[] minPoint, out double[] maxPoint)
        {
            if (nodesGroup.Count == 0)
                throw new ArgumentException("The list of nodes is empty.");

            if (!(nodesGroup[0] is Node))
                throw new ArgumentException("The list must only contain elements of type Node.");

            minPoint = new double[3] {double.MaxValue, double.MaxValue, double.MaxValue};
            maxPoint = new double[3] {double.MinValue, double.MinValue, double.MinValue};
            
            foreach (Node node2 in nodesGroup)
            {
                if (minPoint[0] > node2._Position[0])
                    minPoint[0] = node2._Position[0];
                if (minPoint[1] > node2._Position[1])
                    minPoint[1] = node2._Position[1];
                if (minPoint[2] > node2._Position[2])
                    minPoint[2] = node2._Position[2];

                if (maxPoint[0] < node2._Position[0])
                    maxPoint[0] = node2._Position[0];
                if (maxPoint[1] < node2._Position[1])
                    maxPoint[1] = node2._Position[1];
                if (maxPoint[2] < node2._Position[2])
                    maxPoint[2] = node2._Position[2];
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

        /// <summary>
        /// Волновой вес вершины
        /// </summary>
        [XmlIgnore]
        public WaveSource.WaveWeight WaveWeight => _waveWeight;
        [NonSerialized]
        private WaveSource.WaveWeight _waveWeight = null;

        /// <summary>
        /// Привязка вершины к источнику волны <paramref name="source"/>
        /// </summary>
        public WaveSource.WaveWeight AttachTo(WaveSource source)
        {
            if (_waveWeight is null || _waveWeight.Source != source)
                _waveWeight = WaveSource.WaveWeight.Make(source);
            
            return _waveWeight;
        }
    }
}
