using System;
using System.Linq;
using System.Xml.Serialization;

namespace AStar
{
	[Serializable]
	public class Arc : IEquatable<Arc> 
    {
        internal Arc()
        {
            Weight = 1.0;
            LengthUpdated = false;
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

            _StartNode.Add(this);
            _EndNode.Add(this);

            Weight = 1;
            LengthUpdated = false;
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

            _StartNode.Add(this);
            _EndNode.Add(this);

            Weight = weight;
			LengthUpdated = false;
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

                _StartNode?.Remove(this);
                _StartNode = value;
                _StartNode.Add(this);
            }
        }
		private Node _StartNode;

		public Node EndNode
		{
			get => _EndNode;
            set
			{
				if (value is null)
					throw new ArgumentNullException(nameof(EndNode));
				if (StartNode != null && value.Equals(StartNode))
					throw new ArgumentException("StartNode and EndNode must be different");
                _EndNode?.Remove(this);
                _EndNode = value;
                _EndNode.Add(this); 
            }
        }
		private Node _EndNode;

		public double Weight
		{
			get => _Weight;
            set => _Weight = value;
        }
		private double _Weight;

		public bool Passable
        {
            get => !_Disabled && _StartNode != null && _StartNode.Passable && _EndNode!= null && _EndNode.Passable;
        }
        public bool Disabled
        {
            get => _Disabled;
            set => _Disabled = value;
        }
        private bool _Disabled = false; 

#if false
        /// <summary>
        /// Флаг, при установки которого ребро помечается некорректным и подлежит удалению
        /// </summary>
        [XmlIgnore]
        public bool Invalid
        {
            get => !_StartNode.Passable || !_EndNode.Passable;
        }
#endif

        internal bool LengthUpdated
		{
			get => _LengthUpdated;
            set => _LengthUpdated = value;
        }
		private bool _LengthUpdated;

		public double Length
		{
			get
			{
                if (LengthUpdated) return _Length;
                _Length = CalculateLength();
                _LengthUpdated = true;
                return _Length;
			}
		}
		private double _Length;

		protected virtual double CalculateLength()
		{
			return Point3D.DistanceBetween(_StartNode.Position, _EndNode.Position);
		}

		public virtual double Cost => _Weight * Length;

        public override string ToString()
		{
			return string.Concat(_StartNode, "-->", _EndNode);
		}

		public override bool Equals(object O)
		{
            if (ReferenceEquals(this, O))
                return true;
            if (O is Arc arc
                && arc.StartNode != null
                && arc.EndNode != null)
                return _StartNode != null && _StartNode.Equals(arc._StartNode) && _EndNode != null && _EndNode.Equals(arc._EndNode);
            return false;
        }

        public bool Equals(Arc arc)
        {
            if (ReferenceEquals(this, arc))
                return true;
            return _StartNode != null && _StartNode.Equals(arc._StartNode) && _EndNode!= null && _EndNode.Equals(arc._EndNode);
        } 

        public override int GetHashCode()
		{
			return (int)Length;
		}

	}
}
