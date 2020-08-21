using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Xml.Serialization;
using System.Linq;

namespace AStar
{
    /// <summary>
    /// Изменение графа
    /// </summary>
    public enum NotifyReason
    {
        AddingNode,
        RemovingNode,
        RemovingNodes,
        ChangingNode,
        AddingArc,
        RemovingArc,
        RemovingArcs,
        ChangingArc
    }

	[Serializable]
	public class Graph
	{
        [XmlIgnore]
        public object Locker => this;
#if false
        [NonSerialized]
        object _locker = new object(); 
#endif

#if false
        public delegate void NotifyNodeChanged(Node node, NotifyReason reason);
        public event NotifyNodeChanged onNodeChanged; 
        public delegate void NotifyNodesChanged(ArrayList nodes, NotifyReason reason);
        public event NotifyNodesChanged BeforeNodesChanged;

        public delegate void NotifyArcChanged(Arc arc, NotifyReason reason);
        public event NotifyArcChanged onArcChanged; 
        public delegate void NotifyArcsChanged(ArrayList arcs, NotifyReason reason);
        public event NotifyArcsChanged BeforeArcsChanged;
#endif
        public delegate void NotifyGraphChanged(object obj, NotifyReason reason);
        public event NotifyGraphChanged BeforeGraphChanged;

        public Graph()
		{
			LN = new ArrayList();
			LA = new ArrayList();
#if disabled_20200723_1054
            _tags = new Dictionary<object, object>(); 
#endif
        }

        public IList Nodes => LN;

        public IList Arcs => LA;

        public void Clear()
		{
            lock (Locker)
            {
                BeforeGraphChanged?.Invoke(LN, NotifyReason.RemovingNodes);
                LN.Clear();
                BeforeGraphChanged?.Invoke(LN, NotifyReason.RemovingArcs);
                LA.Clear(); 
            }
		}

		public bool AddNode(Node NewNode)
		{
			if (NewNode is null || LN.Contains(NewNode))
				return false;

            lock (Locker)
            {
                BeforeGraphChanged?.Invoke(new ArrayList() { NewNode }, NotifyReason.AddingNode);
                LN.Add(NewNode); 
            }
			return true;
		}

		public Node AddNode(float x, float y, float z)
		{
			Node node = new Node(x, y, z);
            lock (Locker)
            {
                return AddNode(node) ? node : null;
            }
		}

		public bool AddArc(Arc NewArc)
		{
			if (NewArc is null || LA.Contains(NewArc))
				return false;

            if (!LN.Contains(NewArc.StartNode) || !LN.Contains(NewArc.EndNode))
				throw new ArgumentException("Cannot add an arc if one of its extremity nodes does not belong to the graph.");

            lock (Locker)
            {
                BeforeGraphChanged?.Invoke(new ArrayList() { NewArc }, NotifyReason.AddingArc);
                LA.Add(NewArc); 
            }
			return true;
		}

        public Arc AddArc(Node StartNode, Node EndNode, float Weight)
        {
            Arc arc = new Arc(StartNode, EndNode) {Weight = Weight};
            lock (Locker)
            {
                return AddArc(arc) ? arc : null;
            }
        }
    

		public void Add2Arcs(Node Node1, Node Node2, float Weight)
		{
			AddArc(Node1, Node2, Weight);
			AddArc(Node2, Node1, Weight);
		}

		public bool RemoveNode(Node NodeToRemove)
		{
			if (NodeToRemove is null)
				return false;

            try
			{
                lock (Locker)
                {
                    BeforeGraphChanged?.Invoke(new ArrayList() { NodeToRemove.IncomingArcs }, NotifyReason.RemovingArcs);
                    foreach (Arc arc in NodeToRemove.IncomingArcs)
                    {
                        arc.StartNode.OutgoingArcs.Remove(arc);
                        LA.Remove(arc);
                    }
                    BeforeGraphChanged?.Invoke(new ArrayList() { NodeToRemove.OutgoingArcs }, NotifyReason.RemovingArcs);
                    foreach (Arc arc in NodeToRemove.OutgoingArcs)
                    {
                        arc.EndNode.IncomingArcs.Remove(arc);
                        LA.Remove(arc);
                    }
                    BeforeGraphChanged?.Invoke(new ArrayList() { NodeToRemove }, NotifyReason.RemovingNode);
                    LN.Remove(NodeToRemove); 
                }
			}
			catch
			{
				return false;
			}
			return true;
		}

        public int RemoveImpassableNodes()
        {
            int num = LN.Count;
            int lastFreeElement = 0;

            lock (Locker)
            {
                // Уплотнение списка вершин
                for (int i = 0; i < LN.Count; i++)
                {
                    if (LN[i] is Node node
                        && node.Passable)
                    {
                        node.RemoveImpassableArcs();
                        LN[lastFreeElement] = node;
                        lastFreeElement++;
                    }
                }
                // удаление "лишних" элементов в конце списка LN
                if (lastFreeElement < LN.Count)
                    LN.RemoveRange(lastFreeElement, LN.Count - lastFreeElement);
                num -= lastFreeElement;

                // Уплотнение списка ребер
                lastFreeElement = 0;
                for (int i = 0; i < LA.Count; i++)
                {
                    if (LA[i] is Arc arc
                        && arc.Passable)
                    {
                        LA[lastFreeElement] = arc;
                        lastFreeElement++;
                    }
                }
                if (lastFreeElement < LA.Count)
                    LA.RemoveRange(lastFreeElement, LA.Count - lastFreeElement); 
            }

            // Число удаленных вершин
            return num;
        }

		public bool RemoveArc(Arc ArcToRemove)
		{
			if (ArcToRemove is null)
				return false;

            try
			{
                lock (Locker)
                {
                    BeforeGraphChanged?.Invoke(new ArrayList() { ArcToRemove }, NotifyReason.RemovingArc);
                    LA.Remove(ArcToRemove);
                    ArcToRemove.StartNode.OutgoingArcs.Remove(ArcToRemove);
                    ArcToRemove.EndNode.IncomingArcs.Remove(ArcToRemove); 
                }
			}
			catch
			{
				return false;
			}
			return true;
		}

		public void BoundingBox(out double[] MinPoint, out double[] MaxPoint)
		{
			try
			{
				Node.BoundingBox(Nodes, out MinPoint, out MaxPoint);
			}
			catch (ArgumentException innerException)
			{
				throw new InvalidOperationException("Impossible to determine the bounding box for this graph.\n", innerException);
			}
		}

		public Node ClosestNode(double PtX, double PtY, double PtZ, out double Distance, bool IgnorePassableProperty)
		{
			Node result = null;
			double num = -1.0;
			Point3D p = new Point3D(PtX, PtY, PtZ);
			foreach (Node node in LN)
			{
				if (!IgnorePassableProperty || node.Passable)
				{
					double num2 = Point3D.DistanceBetween(node.Position, p);
					if (num < 0 || num > num2)
					{
						num = num2;
						result = node;
					}
				}
			}
			Distance = num;
			return result;
		}

		public Arc ClosestArc(double PtX, double PtY, double PtZ, out double Distance, bool IgnorePassableProperty)
		{
			Arc result = null;
			double num = -1.0;
			Point3D point3D = new Point3D(PtX, PtY, PtZ);
			foreach (Arc arc in LA)
			{
				if (!IgnorePassableProperty || arc.Passable)
				{
					Point3D p = Point3D.ProjectOnLine(point3D, arc.StartNode.Position, arc.EndNode.Position);
					double num2 = Point3D.DistanceBetween(point3D, p);
					if (num < 0 || num > num2)
					{
						num = num2;
						result = arc;
					}
				}
			}
			Distance = num;
			return result;
		}

#if disabled_20200723_1054
        /// <summary>
        /// Список меток
        /// </summary>
        [XmlIgnore]
        public Dictionary<object, object> Tags { get => _tags; }

        [NonSerialized]
        private readonly Dictionary<object, object> _tags; 
#endif

        private readonly ArrayList LN;
		private readonly ArrayList LA;
	}
}
