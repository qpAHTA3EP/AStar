using System;
using System.Collections;
using System.Collections.Generic;
using System.Xml.Serialization;

namespace AStar
{
    [Serializable]
	public class Node
	{
		public Node(double PositionX, double PositionY, double PositionZ)
		{
			this._Position = new Point3D(PositionX, PositionY, PositionZ);
			this._Passable = true;
			this._IncomingArcs = new ArrayList();
			this._OutgoingArcs = new ArrayList();
            this._tags = new Dictionary<object, object>();
        }

		public Node() { }

        public IList IncomingArcs
		{
			get
			{
				return this._IncomingArcs;
			}
		}

		public IList OutgoingArcs
		{
			get
			{
				return this._OutgoingArcs;
			}
		}

		public bool Passable
		{
			get
			{
				return this._Passable;
			}
			set
			{
				foreach (object obj in this._IncomingArcs)
				{
					Arc arc = (Arc)obj;
					arc.Passable = value;
				}
				foreach (object obj2 in this._OutgoingArcs)
				{
					Arc arc2 = (Arc)obj2;
					arc2.Passable = value;
				}
				this._Passable = value;
			}
		}

		public double X
		{
			get
			{
				return this.Position.X;
			}
		}

		public double Y
		{
			get
			{
				return this.Position.Y;
			}
		}

		public double Z
		{
			get
			{
				return this.Position.Z;
			}
		}

		public void ChangeXYZ(double PositionX, double PositionY, double PositionZ)
		{
			this.Position = new Point3D(PositionX, PositionY, PositionZ);
		}

		public Point3D Position
		{
			get
			{
				return this._Position;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException();
				}
				foreach (object obj in this._IncomingArcs)
				{
					Arc arc = (Arc)obj;
					arc.LengthUpdated = false;
				}
				foreach (object obj2 in this._OutgoingArcs)
				{
					Arc arc2 = (Arc)obj2;
					arc2.LengthUpdated = false;
				}
				this._Position = value;
			}
		}

		public Node[] AccessibleNodes
		{
			get
			{
				Node[] array = new Node[this._OutgoingArcs.Count];
				int num = 0;
				foreach (object obj in this.OutgoingArcs)
				{
					Arc arc = (Arc)obj;
					array[num++] = arc.EndNode;
				}
				return array;
			}
		}

		public Node[] AccessingNodes
		{
			get
			{
				Node[] array = new Node[this._IncomingArcs.Count];
				int num = 0;
				foreach (object obj in this.IncomingArcs)
				{
					Arc arc = (Arc)obj;
					array[num++] = arc.StartNode;
				}
				return array;
			}
		}

		public Node[] Molecule
		{
			get
			{
				int num = 1 + this._OutgoingArcs.Count + this._IncomingArcs.Count;
				Node[] array = new Node[num];
				array[0] = this;
				int num2 = 1;
				foreach (object obj in this.OutgoingArcs)
				{
					Arc arc = (Arc)obj;
					array[num2++] = arc.EndNode;
				}
				foreach (object obj2 in this.IncomingArcs)
				{
					Arc arc2 = (Arc)obj2;
					array[num2++] = arc2.StartNode;
				}
				return array;
			}
		}

		public void Isolate()
		{
			this.UntieIncomingArcs();
			this.UntieOutgoingArcs();
		}

		public void UntieIncomingArcs()
		{
			foreach (object obj in this._IncomingArcs)
			{
				Arc arc = (Arc)obj;
				arc.StartNode.OutgoingArcs.Remove(arc);
			}
			this._IncomingArcs.Clear();
		}

        /// <summary>
        /// Удаление непроходимых ребер
        /// Метод является частью алгоритма сжатия графа
        /// </summary>
        internal void RemoveImpassableArcs()
        {
            int lastFreeElement = 0;
            // уплотнение массива
            for(int i = 0; i< _IncomingArcs.Count; i++)
            {
                if(_IncomingArcs[i] is Arc arc)
                {
                    if (arc.Passable)
                    {
                        _IncomingArcs[lastFreeElement] = arc;
                        lastFreeElement++;
                    }
                }
            }
            // удаление "ненужных" ячеек в конце массива
            if (lastFreeElement < _IncomingArcs.Count)
                _IncomingArcs.RemoveRange(lastFreeElement, _IncomingArcs.Count-lastFreeElement);
            lastFreeElement = 0;
            for (int i = 0; i < _OutgoingArcs.Count; i++)
            {
                if (_OutgoingArcs[i] is Arc arc)
                {
                    if (arc.Passable)
                    {
                        _OutgoingArcs[lastFreeElement] = arc;
                        lastFreeElement++;
                    }
                }
            }
            if (lastFreeElement < _OutgoingArcs.Count)
                _OutgoingArcs.RemoveRange(lastFreeElement, _OutgoingArcs.Count - lastFreeElement);
        }

        public void UntieOutgoingArcs()
		{
			foreach (object obj in this._OutgoingArcs)
			{
				Arc arc = (Arc)obj;
				arc.EndNode.IncomingArcs.Remove(arc);
			}
			this._OutgoingArcs.Clear();
		}

		public Arc ArcGoingTo(Node N)
		{
			if (N == null)
			{
				throw new ArgumentNullException();
			}
			foreach (object obj in this._OutgoingArcs)
			{
				Arc arc = (Arc)obj;
				if (arc.EndNode == N)
				{
					return arc;
				}
			}
			return null;
		}

		public Arc ArcComingFrom(Node N)
		{
			if (N == null)
			{
				throw new ArgumentNullException();
			}
			foreach (object obj in this._IncomingArcs)
			{
				Arc arc = (Arc)obj;
				if (arc.StartNode == N)
				{
					return arc;
				}
			}
			return null;
		}

		private void Invalidate()
		{
			foreach (object obj in this._IncomingArcs)
			{
				Arc arc = (Arc)obj;
				arc.LengthUpdated = false;
			}
			foreach (object obj2 in this._OutgoingArcs)
			{
				Arc arc2 = (Arc)obj2;
				arc2.LengthUpdated = false;
			}
		}

		public override string ToString()
		{
			return this.Position.ToString();
		}

		public override bool Equals(object O)
		{
			Node node = (Node)O;
			if (node == null)
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
			if (N1 == null || N2 == null)
			{
				throw new ArgumentNullException();
			}
			double num = N1.Position.X - N2.Position.X;
			double num2 = N1.Position.Y - N2.Position.Y;
			double num3 = N1.Position.Z - N2.Position.Z;
			return num * num + num2 * num2 + num3 * num3;
		}

		public static double ManhattanDistance(Node N1, Node N2)
		{
			if (N1 == null || N2 == null)
			{
				throw new ArgumentNullException();
			}
			double value = N1.Position.X - N2.Position.X;
			double value2 = N1.Position.Y - N2.Position.Y;
			double value3 = N1.Position.Z - N2.Position.Z;
			return Math.Abs(value) + Math.Abs(value2) + Math.Abs(value3);
		}

		public static double MaxDistanceAlongAxis(Node N1, Node N2)
		{
			if (N1 == null || N2 == null)
			{
				throw new ArgumentNullException();
			}
			double val = Math.Abs(N1.Position.X - N2.Position.X);
			double val2 = Math.Abs(N1.Position.Y - N2.Position.Y);
			double val3 = Math.Abs(N1.Position.Z - N2.Position.Z);
			return Math.Max(val, Math.Max(val2, val3));
		}

		public static void BoundingBox(IList NodesGroup, out double[] MinPoint, out double[] MaxPoint)
		{
			Node node = NodesGroup[0] as Node;
			if (node == null)
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
					{
						MinPoint[j] = node2.Position[j];
					}
					if (MaxPoint[j] < node2.Position[j])
					{
						MaxPoint[j] = node2.Position[j];
					}
				}
			}
		}

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
        private Point3D _Position = new Point3D(0, 0, 0);
		private bool _Passable = false;
		private ArrayList _IncomingArcs = new ArrayList();
		private ArrayList _OutgoingArcs = new ArrayList();
	}
}
