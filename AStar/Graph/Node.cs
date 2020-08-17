﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;
using AStar.Search;
using AStar.Search.Wave;

namespace AStar
{
    [Serializable]
    public class Node
    {
        public Node(double PositionX, double PositionY, double PositionZ)
        {
            _Position = new Point3D(PositionX, PositionY, PositionZ);
            _Passable = true;
            _IncomingArcs = new ArrayList();
            _OutgoingArcs = new ArrayList();
#if NodeTags
            this._tags = new Dictionary<object, object>();
#endif
            _waveWeight = new WaveWeight();
        }

        public Node() { }

        public IList IncomingArcs => _IncomingArcs;

        public IList OutgoingArcs => _OutgoingArcs;

        public bool Passable
        {
            get => _Passable;
            set
            {
                foreach (Arc arc in _IncomingArcs)
                    arc.Passable = value;

                foreach (Arc arc2 in _OutgoingArcs)
                    arc2.Passable = value;

                _Passable = value;
            }
        }

        public double X => Position.X;

        public double Y => Position.Y;

        public double Z => Position.Z;

        public void ChangeXYZ(double PositionX, double PositionY, double PositionZ)
        {
            Position.X = PositionX;
            Position.Y = PositionY;
            Position.Z = PositionZ;
        }

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
        }

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

        public void Isolate()
        {
            UntieIncomingArcs();
            UntieOutgoingArcs();
        }

        public void UntieIncomingArcs()
        {
            foreach (Arc arc in _IncomingArcs)
                arc.StartNode.OutgoingArcs.Remove(arc);

            _IncomingArcs.Clear();
        }

        /// <summary>
        /// Удаление непроходимых ребер
        /// Метод является частью алгоритма сжатия графа
        /// </summary>
        internal void RemoveImpassableArcs()
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

        public void UntieOutgoingArcs()
        {
            foreach (Arc arc in _OutgoingArcs)
                arc.EndNode.IncomingArcs.Remove(arc);

            _OutgoingArcs.Clear();
        }

        public Arc ArcGoingTo(Node N)
        {
            if (N is null)
                throw new ArgumentNullException();

            foreach (Arc arc in _OutgoingArcs)
                if (Equals(arc.EndNode, N))
                    return arc;

            return null;
        }

        public Arc ArcComingFrom(Node N)
        {
            if (N == null)
            {
                throw new ArgumentNullException();
            }
            foreach (Arc arc in _IncomingArcs)
                if (Equals(arc.StartNode, N))
                    return arc;

            return null;
        }

        private void Invalidate()
        {
            foreach (Arc arc in _IncomingArcs)
                arc.LengthUpdated = false;
            foreach (Arc arc2 in _OutgoingArcs)
                arc2.LengthUpdated = false;
        }

        public override string ToString()
        {
            return this.Position.ToString();
        }

        public override bool Equals(object O)
        {
            Node node = (Node)O;
            if (node is null)
            {
                throw new ArgumentException(string.Concat(new object[]
                {
                    "Type ",
                    O.GetType(),
                    " cannot be compared with type ",
                    base.GetType(),
                    " !"
                }));
            }
            return this.Position.Equals(node.Position);
        }

        public object Clone()
        {
            return new Node(this.X, this.Y, this.Z)
            {
                _Passable = this._Passable
            };
        }

        public override int GetHashCode()
        {
            return this.Position.GetHashCode();
        }

        public static double EuclidianDistance(Node N1, Node N2)
        {
            return Math.Sqrt(Node.SquareEuclidianDistance(N1, N2));
        }

        public static double SquareEuclidianDistance(Node N1, Node N2)
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
            if (!(NodesGroup[0] is Node node))
            {
                throw new ArgumentException("The list must only contain elements of type Node.");
            }
            if (NodesGroup.Count == 0)
            {
                throw new ArgumentException("The list of nodes is empty.");
            }
            int num = 3;
            MinPoint = new double[num];
            MaxPoint = new double[num];
            for (int i = 0; i < num; i++)
            {
                MinPoint[i] = (MaxPoint[i] = node.Position[i]);
            }
            foreach (object obj in NodesGroup)
            {
                Node node2 = (Node)obj;
                for (int j = 0; j < num; j++)
                {
                    if (MinPoint[j] > node2.Position[j])
                        MinPoint[j] = node2.Position[j];

                    if (MaxPoint[j] < node2.Position[j])
                        MaxPoint[j] = node2.Position[j];
                }
            }
        }

        /// <summary>
        /// Список меток
        /// </summary>
#if NodeTags
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
        public WaveWeight WaveWeight
        {
            get
            {
                if (_waveWeight is null)
                    _waveWeight = new WaveWeight();
                return _waveWeight;
            }

            set => _waveWeight = value;
        }
        [NonSerialized]
        private WaveWeight _waveWeight;

        private Point3D _Position = new Point3D(0, 0, 0);
        private bool _Passable = false;
        private ArrayList _IncomingArcs = new ArrayList();
        private ArrayList _OutgoingArcs = new ArrayList();
    }
}
