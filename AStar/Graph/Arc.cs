using System;

namespace AStar
{
	[Serializable]
	public class Arc
#if IEquatable
        : IEquatable<Arc> 
#endif
    {
        private Arc()
        {
            Weight = 1.0;
            LengthUpdated = false;
            Passable = true;
        }

        public Arc(Node start, Node end)
        {
            if (start is null)
                throw new ArgumentNullException(nameof(start));
            if (end is null)
                throw new ArgumentNullException(nameof(end));

            if (start.Equals(end))
                throw new ArgumentException("StartNode and EndNode must be different");
            _StartNode = start;
            _EndNode = end;

#if Arc_AddUnique
        if (_StartNode.OutgoingArcs.AddUnique(this) >= 0 &&
            _EndNode.IncomingArcs.AddUnique(this) >= 0)
                throw new ArgumentException("StartNode and EndNode are already linked by an Arc"); 
#else
            _StartNode.OutgoingArcs.Add(this);
            _EndNode.IncomingArcs.Add(this);
#endif

            Weight = 1;
            LengthUpdated = false;
            Passable = true;
        }

        public Arc(Node start, Node end, double weight = 1)
		{
            if(start is null)
                throw  new ArgumentNullException(nameof(start));
            if (end is null)
                throw new ArgumentNullException(nameof(end));

            if (start.Equals(end))
                throw new ArgumentException("StartNode and EndNode must be different");
			_StartNode = start;
			_EndNode = end;

#if Arc_AddUnique
            if (_StartNode.OutgoingArcs.AddUnique(this) >= 0 &&
                _EndNode.IncomingArcs.AddUnique(this) >= 0)
                throw new ArgumentException("StartNode and EndNode are already linked by an Arc"); 
#else
            _StartNode.OutgoingArcs.Add(this);
            _EndNode.IncomingArcs.Add(this);
#endif

            Weight = weight;
			LengthUpdated = false;
			Passable = true;
		}



        public static Arc Make(Node start, Node end, double weight = 1)
        {
            Arc arc = Get(start, end) ?? new Arc{_StartNode = start, _EndNode = end, _Weight = weight };
            return arc;
        }

        /// <summary>
        /// Проверка наличия ребра, связывающего обе вершины
        /// </summary>
        /// <param name="start"></param>
        /// <param name="end"></param>
        /// <returns></returns>
        public static Arc Get(Node start, Node end)
        {
            if (start is null)
                throw new ArgumentNullException(nameof(start));
            if (end is null)
                throw new ArgumentNullException(nameof(end));

            if (start.Equals(end))
                throw new ArgumentException("StartNode and EndNode must be different");

            Arc arc = start.ArcGoingTo(end);
            if (arc != null)
            {
                Arc arc2 = end.ArcComingFrom(start);
                if (arc2 != null
                    && arc.Equals(arc2))
                    return arc;
            }

            return null;
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
#if Arc_AddUnique
                _StartNode.OutgoingArcs.AddUnique(this); 
#else
                _StartNode.OutgoingArcs.Add(this);
#endif
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
#if Arc_AddUnique
                _EndNode.IncomingArcs.AddUnique(this);
#else
                _EndNode.IncomingArcs.Add(this); 
#endif
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

#if false
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
#else
            if (O is Arc arc
                && arc.StartNode != null
                && arc.EndNode != null)
#endif
                return _StartNode.Equals(arc._StartNode) && _EndNode.Equals(arc._EndNode);
            return false;
        }

#if IEquatable
        public bool Equals(Arc arc)
        {
            return _StartNode.Equals(arc._StartNode) && _EndNode.Equals(arc._EndNode);
        } 
#endif

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
