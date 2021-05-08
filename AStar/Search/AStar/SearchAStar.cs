using System;
using System.Collections.Generic;
using AStar.Search.Wave;

namespace AStar.Search.AStar
{
	public class AStarSearch : SearchPathBase
	{
		public static Heuristic EuclideanHeuristic => Node.EuclideanDistance;

        public static Heuristic MaxAlongAxisHeuristic => Node.MaxDistanceAlongAxis;

        public static Heuristic ManhattanHeuristic => Node.ManhattanDistance;

        public Heuristic ChoosenHeuristic
		{
			get => Track.ChoosenHeuristic;
            set => Track.ChoosenHeuristic = value;
        }

		public double DijkstraHeuristicBalance
		{
			get => Track.DijkstraHeuristicBalance;
            set
			{
				if (value < 0.0 || value > 1.0)
				{
					throw new ArgumentException("DijkstraHeuristicBalance value must belong to [0;1].");
				}
				Track.DijkstraHeuristicBalance = value;
			}
		}

		public AStarSearch(Graph G)
		{
			_Graph = G;
			_Open = new SortableList();
			_Closed = new SortableList();
			ChoosenHeuristic = EuclideanHeuristic;
			DijkstraHeuristicBalance = 0.5;
		}

		public override bool SearchPath(Node StartNode, Node EndNode)
		{
			bool pathFound;
            using (_Graph.ReadLock())
			{
				Initialize(StartNode, EndNode);
				while (NextStep()) { }
				pathFound = PathFound;
			}

            return pathFound;
		}

		public Node[][] Open
		{
			get
			{
				Node[][] array = new Node[_Open.Count][];
				for (int i = 0; i < _Open.Count; i++)
				{
					array[i] = GoBackUpNodes((Track)_Open[i]);
				}
				return array;
			}
		}

		public Node[][] Closed
		{
			get
			{
				Node[][] array = new Node[_Closed.Count][];
				for (int i = 0; i < _Closed.Count; i++)
				{
					array[i] = GoBackUpNodes((Track)_Closed[i]);
				}
				return array;
			}
		}

		public void Initialize(Node StartNode, Node EndNode)
		{
			if (StartNode == null || EndNode == null)
			{
				throw new ArgumentNullException();
			}
			_Closed.Clear();
			_Open.Clear();
			Track.Target = EndNode;
			_Open.Add(new Track(StartNode));
			_NbIterations = 0;
			_LeafToGoBackUp = null;
		}

		public bool NextStep()
		{
			if (!Initialized)
			{
				throw new InvalidOperationException("You must initialize AStar before launching the algorithm.");
			}
			if (_Open.Count == 0)
			{
				return false;
			}
			_NbIterations++;
			int index = _Open.IndexOfMin();
			Track track = (Track)_Open[index];
			_Open.RemoveAt(index);
			if (track.Succeed)
			{
				_LeafToGoBackUp = track;
				_Open.Clear();
			}
			else
			{
				Propagate(track);
				_Closed.Add(track);
			}
			return _Open.Count > 0;
		}

		private void Propagate(Track TrackToPropagate)
		{
			foreach (object obj in TrackToPropagate.EndNode.OutgoingArcs)
			{
				Arc arc = (Arc)obj;
				if (arc.Passable && arc.EndNode.Passable)
				{
					Track track = new Track(TrackToPropagate, arc);
					int num = _Closed.IndexOf(track, SameNodesReached);
					int num2 = _Open.IndexOf(track, SameNodesReached);
					if ((num <= 0 || track.Cost < ((Track)_Closed[num]).Cost) && (num2 <= 0 || track.Cost < ((Track)_Open[num2]).Cost))
					{
						if (num > 0)
						{
							_Closed.RemoveAt(num);
						}
						if (num2 > 0)
						{
							_Open.RemoveAt(num2);
						}
						_Open.Add(track);
					}
				}
			}
		}

		public bool Initialized => _NbIterations >= 0;

        public bool SearchStarted => _NbIterations > 0;

        public bool SearchEnded => SearchStarted && _Open.Count == 0;

        public override bool PathFound => _LeafToGoBackUp != null;

        public int StepCounter => _NbIterations;

        private void CheckSearchHasEnded()
		{
			if (!SearchEnded)
			{
				throw new InvalidOperationException("You cannot get a result unless the search has ended.");
			}
		}

		public bool ResultInformation(out int NbArcsOfPath, out double CostOfPath)
		{
			CheckSearchHasEnded();
			if (!PathFound)
			{
				NbArcsOfPath = -1;
				CostOfPath = -1.0;
				return false;
			}
			NbArcsOfPath = _LeafToGoBackUp.NbArcsVisited;
			CostOfPath = _LeafToGoBackUp.Cost;
			return true;
		}

		public override Node[] PathByNodes
		{
			get
			{
				CheckSearchHasEnded();
				if (!PathFound)
				{
					return null;
				}
				return GoBackUpNodes(_LeafToGoBackUp);
			}
		}

        public override IEnumerable<Node> PathNodes
        {
            get
            {
#if true
                return PathByNodes;
#else
                var trackTail = _LeafToGoBackUp;
                while (trackHead != null)
                {
                    yield return trackTail.EndNode;
                    trackTail = trackTail.Queue;
                }
#endif

            }
        }

        public override int PathNodeCount
        {
            get
            {
                if (PathFound)
                    return _LeafToGoBackUp.NbArcsVisited + 1;
                else return 0;
            }
        }

        public override double PathLength
        {
            get
            {
                CheckSearchHasEnded();
                if (!PathFound && _LeafToGoBackUp != null)
                {
                    return _LeafToGoBackUp.EuclideanLength;
                }

                return 0;
            }
        }

		private Node[] GoBackUpNodes(Track T)
		{
			int nbArcsVisited = T.NbArcsVisited;
			Node[] array = new Node[nbArcsVisited + 1];
			int i = nbArcsVisited;
			while (i >= 0)
			{
				array[i] = T.EndNode;
				i--;
				T = T.Queue;
			}
			return array;
		}

        public override void Rebase(Graph g)
        {
            _Graph = g;
            _Open.Clear();
            _Closed.Clear();
            _LeafToGoBackUp = null;
            _NbIterations = -1;

            SameNodesReached = Track.SameEndNode;
        }

        /// <summary>
        /// Сброс результатов последнего поиска
        /// </summary>
        public override void Reset()
        {
            _Open.Clear();
            _Closed.Clear();
            _LeafToGoBackUp = null;
            _NbIterations = -1;

            SameNodesReached = Track.SameEndNode;
        }


        public Arc[] PathByArcs
		{
			get
			{
				CheckSearchHasEnded();
				if (!PathFound)
				{
					return null;
				}
				int nbArcsVisited = _LeafToGoBackUp.NbArcsVisited;
				Arc[] array = new Arc[nbArcsVisited];
				Track track = _LeafToGoBackUp;
				int i = nbArcsVisited - 1;
				while (i >= 0)
				{
					array[i] = track.Queue.EndNode.ArcGoingTo(track.EndNode);
					i--;
					track = track.Queue;
				}
				return array;
			}
		}

		public Point3D[] PathByCoordinates
		{
			get
			{
				CheckSearchHasEnded();
				if (!PathFound)
				{
					return null;
				}
				int nbArcsVisited = _LeafToGoBackUp.NbArcsVisited;
				Point3D[] array = new Point3D[nbArcsVisited + 1];
				Track track = _LeafToGoBackUp;
				int i = nbArcsVisited;
				while (i >= 0)
				{
					array[i] = track.EndNode.Position;
					i--;
					track = track.Queue;
				}
				return array;
			}
		}

        private Graph _Graph;
		private SortableList _Open;
		private SortableList _Closed;
		private Track _LeafToGoBackUp;
		private int _NbIterations = -1;

        private SortableList.Equality SameNodesReached = Track.SameEndNode;
	}
}
