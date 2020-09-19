using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using System.Xml.Serialization;
using System.Linq;
using AStar.Tools;

namespace AStar
{


    [Serializable]
    public class Graph
    {
        // TODO: Попробовать синхронизацию доступа через RWLocker
        // см https://habr.com/ru/post/459514/#ReaderWriterLockSlim
        [XmlIgnore]
        public object SyncRoot => this;//LN.SyncRoot
#if false
        [NonSerialized]
        object _locker = new object();
#elif false
        [NonSerialized]
        public readonly RWLocker locker = new RWLocker();
#endif

#if false
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
        public delegate void NotifyNodeChanged(Node node, NotifyReason reason);
        public event NotifyNodeChanged onNodeChanged; 
        public delegate void NotifyNodesChanged(ArrayList nodes, NotifyReason reason);
        public event NotifyNodesChanged BeforeNodesChanged;

        public delegate void NotifyArcChanged(Arc arc, NotifyReason reason);
        public event NotifyArcChanged onArcChanged; 
        public delegate void NotifyArcsChanged(ArrayList arcs, NotifyReason reason);
        public event NotifyArcsChanged BeforeArcsChanged;
        public delegate void NotifyGraphChanged(object obj, NotifyReason reason);
        public event NotifyGraphChanged BeforeGraphChanged;
#endif

        public Graph() { }

        public IList Nodes => LN;
        private readonly ArrayList LN = new ArrayList();

#if UseListOfArcs
        public IList Arcs => LA;
        private readonly ArrayList LA = new ArrayList(); 
#elif true
        //Заглушка для Astral'a
        [XmlIgnore]
        public IList Arcs => new EnumerableAsReadOnlyListWrapper<IEnumerable<Arc>>(EnumerateArcs);
#endif
        public IEnumerable<Arc> EnumerateArcs
        {
            get
            {
                foreach(Node node in LN)
                {
                    foreach (Arc arc in node.OutgoingArcs)
                        yield return arc;
                }
            }
        }


        public void Clear()
		{
            lock (SyncRoot)
            {
#if BeforeGraphChanged
                BeforeGraphChanged?.Invoke(LN, NotifyReason.RemovingNodes);
#endif
                LN.Clear();
#if BeforeGraphChanged
                BeforeGraphChanged?.Invoke(LN, NotifyReason.RemovingArcs); 
#endif
#if UseListOfArcs
                LA.Clear();  
#endif
            }
        }

		public bool AddNode(Node node)
		{
			if (node is null || LN.Contains(node))
				return false;

            lock (SyncRoot)
            {
#if BeforeGraphChanged
                BeforeGraphChanged?.Invoke(new ArrayList() { NewNode }, NotifyReason.AddingNode); 
#endif
                LN.Add(node); 
            }
			return true;
		}

		public Node AddNode(float x, float y, float z)
		{
			Node node = new Node(x, y, z);
            lock (SyncRoot)
            {
                return AddNode(node) ? node : null;
            }
		}

#if UseListOfArcs
        protected bool lockAddArc(Arc NewArc)
        {
#if BeforeGraphChanged
            BeforeGraphChanged?.Invoke(new ArrayList() { NewArc }, NotifyReason.AddingArc); 
#endif
            lock (Locker)
            {
                return LA.Add(NewArc) > 0;
            }
        } 

        public bool AddArc(Arc NewArc)
		{
			if (NewArc is null || LA.Contains(NewArc))
				return false;

            if (!LN.Contains(NewArc.StartNode) || !LN.Contains(NewArc.EndNode))
				throw new ArgumentException("Cannot add an arc if one of its extremity nodes does not belong to the graph.");

            lockAddArc(NewArc);

            return true;
		}

        public Arc AddArc(Node StartNode, Node EndNode, float Weight)
        {
            Arc arc = Arc.Get(StartNode, EndNode);
            if(arc is null)
            {
                arc = new Arc(StartNode, EndNode, Weight);
                return lockAddArc(arc) ? arc : null;
            }

            return arc;
        }

        public void Add2Arcs(Node Node1, Node Node2, float Weight)
		{
            if (!LN.Contains(Node1) || !LN.Contains(Node2))
                throw new ArgumentException("Cannot add an arc if one of its extremity nodes does not belong to the graph.");

            //AddArc(Node1, Node2, Weight);
            Arc arc = Arc.Get(Node1, Node2);
            if (arc is null)
            {
                arc = new Arc(Node1, Node2, Weight);
                lockAddArc(arc);
            }

            //AddArc(Node2, Node1, Weight);
            arc = Arc.Get(Node2, Node1);
            if (arc is null)
            {
                arc = new Arc(Node2, Node1, Weight);
                lockAddArc(arc);
            }
        }
#else
        public Arc AddArc(Node startNode, Node endNode, float weight)
        {
            return startNode.ConnectTo(endNode, weight);
        }

        public void Add2Arcs(Node Node1, Node Node2, float Weight)
        {
            if (!LN.Contains(Node1) || !LN.Contains(Node2))
                throw new ArgumentException("Cannot add an arc if one of its extremity nodes does not belong to the graph.");

            //AddArc(Node1, Node2, Weight);
            Node1.ConnectTo(Node2, Weight);

            //AddArc(Node2, Node1, Weight);
            Node2.ConnectTo(Node1, Weight);
        }
#endif

        public bool RemoveNode(Node node)
		{
			if (node is null)
				return false;

            lock (SyncRoot)
            {
                node.Isolate();
                LN.Remove(node);
            }
			return true;
		}

        /// <summary>
        /// Удаление из графа непроходимых и некорректных вершин и ребер
        /// </summary>
        /// <returns></returns>
        public int Compression()
        {
            int deletedNodesNum = LN.Count;
            int lastFreeElement = 0;

            lock (SyncRoot)
            {
                List<Arc> arcsToRemove = new List<Arc>();
                // Уплотнение списка вершин
                for (int i = 0; i < LN.Count; i++)
                {
                    if (LN[i] is Node node)
                        if (node.Passable)
                        {
#if Node_RemoveDublicateArcs
                            // Работает некорректно
                            node.RemoveDublicateArcs(); 
#endif
                            LN[lastFreeElement] = node;
                            lastFreeElement++;
                        }
                        else node.Isolate();
                }
                // удаление "лишних" элементов в конце списка LN
                if (lastFreeElement < LN.Count)
                    LN.RemoveRange(lastFreeElement, LN.Count - lastFreeElement);
                deletedNodesNum -= lastFreeElement;
            }

            // Число удаленных вершин
            return deletedNodesNum;
        }

		public bool RemoveArc(Arc ArcToRemove)
		{
			if (ArcToRemove is null)
				return false;


            lock (SyncRoot)
            {
#if BeforeGraphChanged
                BeforeGraphChanged?.Invoke(ArcToRemove, NotifyReason.RemovingArc);
#endif
                ArcToRemove.StartNode.Remove(ArcToRemove);
                ArcToRemove.EndNode.Remove(ArcToRemove);
#if UseListOfArcs
                LA.Remove(ArcToRemove); 
#endif
            }
			return true;
		}

        public int RemoveArcs(ArrayList ArcsToRemome)
        {
            int deletedArcsNum = 0;
            if(ArcsToRemome?.Count > 0)
            {

                lock (SyncRoot)
                {
#if UseListOfArcs
                    int lastFreeElement = 0;
                    // уплотнение массива LA
                    for (int i = 0; i < LA.Count; i++)
                    {
                        if (LA[i] is Arc arc)
                        {
                            if (ArcsToRemome.Contains(arc))
                            {
                                arc.StartNode.RemoveArcs(ArcsToRemome);
                                arc.EndNode.RemoveArcs(ArcsToRemome);
                            }
                            else
                            {
                                LA[lastFreeElement] = arc;
                                lastFreeElement++;
                            }
                        }
                    }

                    // удаление "ненужных" ячеек в конце массива
                    deletedArcsNum = LA.Count - lastFreeElement;
                    if (deletedArcsNum > 0)
                        LA.RemoveRange(lastFreeElement, deletedArcsNum);  
#else
                    foreach (Arc arc in ArcsToRemome)
                    {
                        arc.StartNode.RemoveArcs(ArcsToRemome);
                        arc.EndNode.RemoveArcs(ArcsToRemome);
                    }
#endif
                }
            }

            return deletedArcsNum;
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
			double closestNodeDist = double.MaxValue;
			Point3D p = new Point3D(PtX, PtY, PtZ);
			foreach (Node node in LN)
			{
				if (!IgnorePassableProperty || node.Passable)
				{
					double dist = Point3D.DistanceBetween(node.Position, p);
					if (closestNodeDist > dist)
					{
						closestNodeDist = dist;
						result = node;
					}
				}
			}
			Distance = closestNodeDist;
			return result;
		}

#if UseListOfArcs
        public Arc ClosestArc(double PtX, double PtY, double PtZ, out double Distance, bool IgnorePassableProperty)
        {
            Arc result = null;
            double closestArcDist = double.MaxValue;
            Point3D point = new Point3D(PtX, PtY, PtZ);
            foreach (Arc arc in LA)
            {
                if (!IgnorePassableProperty || arc.Passable)
                {
                    Point3D pointProjection = Point3D.ProjectOnLine(point, arc.StartNode.Position, arc.EndNode.Position);
                    double dist = Point3D.DistanceBetween(point, pointProjection);
                    if (closestArcDist > dist)
                    {
                        closestArcDist = dist;
                        result = arc;
                    }
                }
            }
            Distance = closestArcDist;
            return result;
        } 
#endif
    }
}
