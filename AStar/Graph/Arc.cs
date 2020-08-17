using System;

namespace AStar
{
	[Serializable]
	public class Arc
	{
		public Arc(Node Start, Node End)
		{
			StartNode = Start;
			EndNode = End;
			Weight = 1.0;
			LengthUpdated = false;
			Passable = true;
		}

		public Node StartNode
		{
			get => _StartNode;
            set
			{
				if (value is null)
					throw new ArgumentNullException(nameof(StartNode));
				if (EndNode != null && value.Equals(EndNode))
					throw new ArgumentException("StartNode and EndNode must be different");

                _StartNode?.OutgoingArcs.Remove(this);
                _StartNode = value;
				_StartNode.OutgoingArcs.Add(this);
			}
		}

		public Node EndNode
		{
			get => _EndNode;
            set
			{
				if (value is null)
					throw new ArgumentNullException(nameof(EndNode));
				if (StartNode != null && value.Equals(StartNode))
					throw new ArgumentException("StartNode and EndNode must be different");
                _EndNode?.IncomingArcs.Remove(this);
                _EndNode = value;
				_EndNode.IncomingArcs.Add(this);
			}
		}

		public double Weight
		{
			get => _Weight;
            set => _Weight = value;
        }

		public bool Passable
		{
			get => _Passable;
            set => _Passable = value;
        }

		internal bool LengthUpdated
		{
			get => _LengthUpdated;
            set => _LengthUpdated = value;
        }

		public double Length
		{
			get
			{
                if (LengthUpdated) return _Length;
                _Length = CalculateLength();
                LengthUpdated = true;
                return _Length;
			}
		}

		protected virtual double CalculateLength()
		{
			return Point3D.DistanceBetween(_StartNode.Position, _EndNode.Position);
		}

		public virtual double Cost => Weight * Length;

        public override string ToString()
		{
			return string.Concat(_StartNode, "-->", _EndNode);
		}

		public override bool Equals(object O)
		{
			Arc arc = (Arc)O;
			if (arc == null)
			{
				throw new ArgumentException(string.Concat(new object[]
				{
					"Cannot compare type ",
					base.GetType(),
					" with type ",
					O.GetType(),
					" !"
				}));
			}
			return this._StartNode.Equals(arc._StartNode) && this._EndNode.Equals(arc._EndNode);
		}

		public override int GetHashCode()
		{
			return (int)Length;
		}

		private Node _StartNode;
		private Node _EndNode;
		private double _Weight;
		private bool _Passable;
		private double _Length;
		private bool _LengthUpdated;
	}
}
