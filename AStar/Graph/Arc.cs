using System;

namespace AStar
{
	[Serializable]
	public class Arc
	{
		public Arc(Node Start, Node End)
		{
			this.StartNode = Start;
			this.EndNode = End;
			this.Weight = 1.0;
			this.LengthUpdated = false;
			this.Passable = true;
		}

		public Node StartNode
		{
			get
			{
				return this._StartNode;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("StartNode");
				}
				if (this.EndNode != null && value.Equals(this.EndNode))
				{
					throw new ArgumentException("StartNode and EndNode must be different");
				}
				if (this._StartNode != null)
				{
					this._StartNode.OutgoingArcs.Remove(this);
				}
				this._StartNode = value;
				this._StartNode.OutgoingArcs.Add(this);
			}
		}

		public Node EndNode
		{
			get
			{
				return this._EndNode;
			}
			set
			{
				if (value == null)
				{
					throw new ArgumentNullException("EndNode");
				}
				if (this.StartNode != null && value.Equals(this.StartNode))
				{
					throw new ArgumentException("StartNode and EndNode must be different");
				}
				if (this._EndNode != null)
				{
					this._EndNode.IncomingArcs.Remove(this);
				}
				this._EndNode = value;
				this._EndNode.IncomingArcs.Add(this);
			}
		}

		public double Weight
		{
			get
			{
				return this._Weight;
			}
			set
			{
				this._Weight = value;
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
				this._Passable = value;
			}
		}

		internal bool LengthUpdated
		{
			get
			{
				return this._LengthUpdated;
			}
			set
			{
				this._LengthUpdated = value;
			}
		}

		public double Length
		{
			get
			{
				if (!this.LengthUpdated)
				{
					this._Length = this.CalculateLength();
					this.LengthUpdated = true;
				}
				return this._Length;
			}
		}

		protected virtual double CalculateLength()
		{
			return Point3D.DistanceBetween(this._StartNode.Position, this._EndNode.Position);
		}

		public virtual double Cost
		{
			get
			{
				return this.Weight * this.Length;
			}
		}

		public override string ToString()
		{
			return this._StartNode.ToString() + "-->" + this._EndNode.ToString();
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
			return (int)this.Length;
		}

		private Node _StartNode;
		private Node _EndNode;
		private double _Weight;
		private bool _Passable;
		private double _Length;
		private bool _LengthUpdated;
	}
}
