using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using AStar.Tools;

namespace AStar
{
    /// <summary>
    /// Интерфейс графа
    /// </summary>
    public interface IGraph
    {
        /// <summary>
        /// Объект монопольной синхронизации (lock)
        /// </summary>
        object SyncRoot { get; }

        RWLocker.ReadLockToken ReadLock();
        RWLocker.UpgradableReadToken UpgradableReadLock();
        RWLocker.WriteLockToken WriteLock(); 

        bool IsReadLockHeld { get; }
        bool IsUpgradeableReadLockHeld { get; }
        bool IsWriteLockHeld { get; }

        IEnumerable<Node> NodesCollection { get; }
        int NodesCount { get; }

        int ForEachNode(Action<Node> action, bool ignorePassableProperty = false);

        IEnumerable<Arc> ArcsCollection { get; }

        void Clear();
        bool AddNode(Node node);
        Node AddNode(float x, float y, float z);
        bool RemoveNode(Node node);

        Arc AddArc(Node startNode, Node endNode, float weight);
        void Add2Arcs(Node Node1, Node Node2, float Weight);
        bool RemoveArc(Arc ArcToRemove);
        int RemoveArcs(ArrayList ArcsToRemome);

        Node ClosestNode(double x, double y, double z, out double distance, bool ignorePassableProperty);
    }
}
