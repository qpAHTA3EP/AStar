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
    public class Graph : IGraph
    {
        public Graph() { }

        #region Synchronization
        /// <summary>
        /// Объект монопольной синхронизации
        /// </summary>
        [XmlIgnore]
        public object SyncRoot => this;

        // Cинхронизация многопоточного доступа через RWLocker
        // см https://habr.com/ru/post/459514/#ReaderWriterLockSlim
        /// <summary>
        /// Объект синхронизации доступа к объекту <see cref="MapperGraphCache"/>
        /// </summary>
        [XmlIgnore]
        [NonSerialized]
        private ReaderWriterLockSlim @lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion);

        /// <summary>
        /// Объект синхронизации для "чтения", допускающий одновременное чтение
        /// </summary>
        /// <returns></returns>
        public RWLocker.ReadLockToken ReadLock() => new RWLocker.ReadLockToken(@lock ?? (@lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion))); //LazyInitializer.EnsureInitialized(ref @lock));
        /// <summary>
        /// Объект синхронизации для "чтения", допускающий ужесточение блокировки до 
        /// </summary>
        /// <returns></returns>
        public RWLocker.UpgradableReadToken UpgradableReadLock() => new RWLocker.UpgradableReadToken(@lock ?? (@lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion))); //LazyInitializer.EnsureInitialized(ref @lock));
        /// <summary>
        /// Объект синхронизации для "записи".
        /// </summary>
        /// <returns></returns>
        public RWLocker.WriteLockToken WriteLock() => new RWLocker.WriteLockToken(@lock ?? (@lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion))); //LazyInitializer.EnsureInitialized(ref @lock));

        public bool IsReadLockHeld =>
            (@lock ?? (@lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion))).IsReadLockHeld;//LazyInitializer.EnsureInitialized(ref @lock)).IsReadLockHeld;
        public bool IsUpgradeableReadLockHeld => (@lock ?? (@lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion))).IsUpgradeableReadLockHeld;//LazyInitializer.EnsureInitialized(ref @lock).IsUpgradeableReadLockHeld;
        public bool IsWriteLockHeld => (@lock ?? (@lock = new ReaderWriterLockSlim(LockRecursionPolicy.SupportsRecursion))).IsWriteLockHeld;//LazyInitializer.EnsureInitialized(ref @lock).IsWriteLockHeld;
        #endregion

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

        #region Данные
        [XmlIgnore]
        public IEnumerable<Node> NodesCollection
        {
            get
            {
                foreach (Node node in LN)
                    yield return node;
            }
        }
        public IList Nodes => LN;
        private readonly ArrayList LN = new ArrayList();

        public int NodesCount => LN.Count;

        //Заглушка для Astral'a
        [XmlIgnore]
        public IList Arcs => new EnumerableAsReadOnlyListWrapper<IEnumerable<Arc>>(ArcsCollection);
        [XmlIgnore]
        public IEnumerable<Arc> ArcsCollection
        {
            get
            {
                foreach (Node node in LN)
                {
                    foreach (Arc arc in node.OutgoingArcs)
                        yield return arc;
                }
            }
        }

        public int Version { get; private set; }
        #endregion

        /// <summary>
        /// Очистка графа (удаление всех вершин)
        /// </summary>
        public void Clear()
		{
#if self_lock
            lock (SyncRoot) 
#endif
            {
#if BeforeGraphChanged
                BeforeGraphChanged?.Invoke(LN, NotifyReason.RemovingNodes);
#endif
                LN.Clear();
#if BeforeGraphChanged
                BeforeGraphChanged?.Invoke(LN, NotifyReason.RemovingArcs); 
#endif
            }
            Version++;
        }

        /// <summary>
        /// Перебор всех вершин и применение к ним дейстия <paramref name="action"/>
        /// </summary>
        public int ForEachNode(Action<Node> action, bool ignorePassableProperty = false)
        {
            int num = 0;
            if (ignorePassableProperty)
                foreach (Node node in LN)
                {
                    action(node);
                    num++;
                }
            else foreach (Node node in LN)
                if(node.Passable)
                {
                    action(node);
                    num++;
                }
            return num;
        }

        /// <summary>
        /// Добавление вершины <paramref name="node"/>
        /// </summary>
        public bool AddNode(Node node)
		{
			if (node is null || LN.Contains(node))
				return false;

#if self_lock
            lock (SyncRoot) 
#endif
            {
#if BeforeGraphChanged
                BeforeGraphChanged?.Invoke(new ArrayList() { NewNode }, NotifyReason.AddingNode); 
#endif
                if(LN.Add(node) >= 0)
                    Version++;
            }
            return true;
		}

        /// <summary>
        /// Добавление вершины с координатами <paramref name="x"/>, <paramref name="y"/>, <paramref name="z"/>
        /// </summary>
        public Node AddNode(float x, float y, float z)
        {
            return AddNode(x, y, z, 0);
        }

        /// <summary>
        /// Добавление вершины с координатами <paramref name="x"/>, <paramref name="y"/>, <paramref name="z"/>
        /// Если в пределах <paramref name="deviation"/> от заданных координат существует вершина, то новая не добавляется
        /// </summary>
        public Node AddNode(float x, float y, float z, double deviation)
		{
            Node node = null;
            if (deviation > 0)
            {
                // Поиск ближайшей вершины к заданным координатам
                node = ClosestNode(x, y, z, out double dist, false);
                if (dist <= deviation)
                    return node;
            }

            if(node is null)
                node = new Node(x, y, z);

            if (AddNode(node))
            {
                Version++;
                return node;
            }
            return null;
		}

        /// <summary>
        /// Добавление односторонней связи между вершинами
        /// </summary>
        public Arc AddArc(Node startNode, Node endNode, float weight)
        {
            if(startNode.ConnectTo(endNode, weight, out Arc arc))
                Version++;
            return arc;
        }

        /// <summary>
        /// Добавление двусторонней связи между вершинами
        /// </summary>
        public void Add2Arcs(Node node1, Node node2, float weight)
        {
            if (!LN.Contains(node1) || !LN.Contains(node2))
                throw new ArgumentException("Cannot add an arc if one of its extremity nodes does not belong to the graph.");

            if(node1.ConnectTo(node2, weight, out Arc arc))
                Version++;

            if(node2.ConnectTo(node1, weight, out arc))
                Version++;
        }

        /// <summary>
        /// Удаление вершины <paramref name="node"/>
        /// </summary>
        public bool RemoveNode(Node node)
		{
			if (node is null)
				return false;

#if self_lock
            lock (SyncRoot) 
#endif
            {
                node.Isolate();
                LN.Remove(node);
                Version++;
            }
            return true;
		}

        /// <summary>
        /// Удаление из графа непроходимых и некорректных вершин и ребер
        /// </summary>
        public int RemoveUnpassable()
        {
            int totalNodes = LN.Count;
            int lastFreeElement = 0;

#if self_lock
            lock (SyncRoot) 
#endif
            {
                //List<Arc> arcsToRemove = new List<Arc>();
                // Уплотнение списка вершин
                for (int i = 0; i < LN.Count; i++)
                {
                    if (LN[i] is Node node)
                        if (node.Passable)
                        {
                            node.RemoveUnpassableAndDublicateArcs(); 
                            LN[lastFreeElement] = node;
                            lastFreeElement++;
                        }
                        // Удаление связей всех прочих вершин с непроходимой вершиной
                        // осуществяется при их обработке путем вызова node.RemoveUnpassableArcs(); 
                        //else node.Isolate();
                }
                // удаление "лишних" элементов в конце списка LN
                if (lastFreeElement < LN.Count)
                {
                    LN.RemoveRange(lastFreeElement, LN.Count - lastFreeElement);
                    Version++;
                }
            }

            // Число удаленных вершин
            return totalNodes - LN.Count;
        }

        public int Validate()
        {
            int errorNum = 0;
            AStarLogger.WriteLine("Start graph verification");
            foreach (Node node in LN)
            {
                foreach (Arc testArc in node.OutgoingArcs)
                {
                    Node testArcEndNode = testArc.EndNode;
                    if (!LN.Contains(testArcEndNode))
                    {
                        // testArc.EndNode отсутствует в общем списке вершин графа
                        errorNum++;
                        AStarLogger.WriteLine($"Arc {testArc} lead to unknown node {testArcEndNode}. Arc disabled.");
                        testArc.Disabled = true;
                    }
                    if(!ReferenceEquals(testArc, testArcEndNode.ArcComingFrom(node)))
                    {
                        // Ребро arc отсутствует в списке IncomingArcs вершины arc.EndNode
                        errorNum++;
                        AStarLogger.WriteLine($"Arc {testArc} not present in IncomingArcs of node {testArcEndNode}. Arc disabled.");
                        testArc.Disabled = true;
                    }
                }

                foreach (Arc testArc in node.IncomingArcs)
                {
                    Node testArcStartNode = testArc.StartNode;
                    if (!LN.Contains(testArcStartNode))
                    {
                        // testArc.StartNode отсутствует в общем списке вершин графа
                        errorNum++;
                        AStarLogger.WriteLine($"Arc {testArc} income from uncnown node {testArcStartNode}. Arc disabled.");
                        testArc.Disabled = true;
                    }
                    if (!ReferenceEquals(testArc, testArcStartNode.ArcGoingTo(node)))
                    {
                        // Ребро arc отсутствует в списке OutgoingArcs вершины arc.EndNode
                        errorNum++;
                        AStarLogger.WriteLine($"Arc {testArc} not present in OutgoingArcs of node {testArcStartNode}. Arc disabled.");
                        testArc.Disabled = true;
                    }
                }
            }
            AStarLogger.WriteLine($"There is {errorNum} errors in the Graph");
            return errorNum;
        }

        /// <summary>
        /// Удаление ребра <paramref name="arcToRemove"/>
        /// </summary>
		public bool RemoveArc(Arc arcToRemove)
		{
			if (arcToRemove is null)
				return false;


#if self_lock
            lock (SyncRoot)  
#endif
            {
#if BeforeGraphChanged
                BeforeGraphChanged?.Invoke(ArcToRemove, NotifyReason.RemovingArc);
#endif
                arcToRemove.StartNode.Remove(arcToRemove);
                arcToRemove.EndNode.Remove(arcToRemove);
                Version++;
            }
            return true;
		}

#if false
        /// <summary>
        /// Удаление ребер <paramref name="arcsToRemove"/>
        /// </summary>
        public int RemoveArcs(ArrayList arcsToRemome)
        {
            int deletedArcsNum = 0;
            if (arcsToRemome?.Count > 0)
            {
#if self_lock
                lock (SyncRoot) 
#endif
                {
                    foreach (Arc arc in arcsToRemome)
                    {
                        arc.StartNode.RemoveArcs(arcsToRemome);
                        arc.EndNode.RemoveArcs(arcsToRemome);
                    }
                }
                Version++;
            }

            return deletedArcsNum;
        } 
#endif

        /// <summary>
        /// Вычисление параллелипипеда, вмещающего все вершины графа
        /// Параллелипипед задается точками <paramref name="minPoint"/> и <paramref name="maxPoint"/>
        /// </summary>
        public void BoundingBox(out double[] minPoint, out double[] maxPoint)
		{
			try
			{
				Node.BoundingBox(LN, out minPoint, out maxPoint);
			}
			catch (ArgumentException innerException)
			{
				throw new InvalidOperationException("Impossible to determine the bounding box for this graph.\n", innerException);
			}
		}

        /// <summary>
        /// Поиск вершины. ближайшей к точке с координатами <paramref name="x"/>, <paramref name="y"/>, <paramref name="z"/>
        /// </summary>
		public Node ClosestNode(double x, double y, double z, out double distance, bool ignorePassableProperty)
		{
			Node result = null;
            distance = double.MaxValue;
            if(ignorePassableProperty)
                foreach (Node node in LN)
			    {
                    var nodePos = node.Position;
                    double squaredDist = Point3D.SquaredDistanceBetween(nodePos.X, nodePos.Y, nodePos.Z, x, y, z);
                    if (distance > squaredDist)
					{
                        distance = squaredDist;
						result = node;
					}
			    }
            else foreach (Node node in LN)
            {
                if (node.Passable)
                {
                    var nodePos = node.Position;
                    double squaredDist = Point3D.SquaredDistanceBetween(nodePos.X, nodePos.Y, nodePos.Z, x, y, z);
                    if (distance > squaredDist)
                    {
                        distance = squaredDist;
                        result = node;
                    }
                }
            }
            if (result != null)
                distance = Math.Sqrt(distance);
            return result;
		}

        /// <summary>
        /// Поиск вершины <paramref name="node1"/>, ближайшей к точке с координатами <paramref name="x1"/>, <paramref name="y1"/>, <paramref name="z1"/>
        /// и вершины <paramref name="node2"/>, ближайшей к точке с координатами <paramref name="x2"/>, <paramref name="y2"/>, <paramref name="z2"/>
        /// </summary>
        public void ClosestNodes(double x1, double y1, double z1, out double distance1, out Node node1,
                                 double x2, double y2, double z2, out double distance2, out Node node2,
                                 bool ignorePassableProperty = false)
        {
            node1 = null;
            node2 = null;
            distance1 = double.MaxValue;
            distance2 = double.MaxValue;
            if(ignorePassableProperty)
                foreach (Node node in LN)
                {
                    var nodePos = node.Position;
                    double squaredDist = Point3D.SquaredDistanceBetween(nodePos.X, nodePos.Y, nodePos.Z, x1, y1, z1);
                    if (distance1 > squaredDist)
                    {
                        distance1 = squaredDist;
                        node1 = node;
                    }

                    squaredDist = Point3D.SquaredDistanceBetween(nodePos.X, nodePos.Y, nodePos.Z, x2, y2, z2);
                    if (distance2 > squaredDist)
                    {
                        distance2 = squaredDist;
                        node2 = node;
                    }
                }
            else foreach (Node node in LN)
            {
                if (node.Passable)
                {
                    var nodePos = node.Position;
                    double squaredDist = Point3D.SquaredDistanceBetween(nodePos.X, nodePos.Y, nodePos.Z, x1, y1, z1);
                    if (distance1 > squaredDist)
                    {
                        distance1 = squaredDist;
                        node1 = node;
                    }

                    squaredDist = Point3D.SquaredDistanceBetween(nodePos.X, nodePos.Y, nodePos.Z, x2, y2, z2);
                    if (distance2 > squaredDist)
                    {
                        distance2 = squaredDist;
                        node2 = node;
                    }
                }
            }
            if (node1 != null)
                distance1 = Math.Sqrt(distance1);
            if (node2 != null)
                distance2 = Math.Sqrt(distance2);
        }
    }
}
