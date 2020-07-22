using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;


namespace AStar
{
	public class AStarSearch : SearchPathBase
	{
		public static Heuristic EuclidianHeuristic
		{
			get
			{
				return new Heuristic(Node.EuclidianDistance);
			}
		}

		public static Heuristic MaxAlongAxisHeuristic
		{
			get
			{
				return new Heuristic(Node.MaxDistanceAlongAxis);
			}
		}

		public static Heuristic ManhattanHeuristic
		{
			get
			{
				return new Heuristic(Node.ManhattanDistance);
			}
		}

		public Heuristic ChoosenHeuristic
		{
			get
			{
				return Track.ChoosenHeuristic;
			}
			set
			{
				Track.ChoosenHeuristic = value;
			}
		}

		public double DijkstraHeuristicBalance
		{
			get
			{
				return Track.DijkstraHeuristicBalance;
			}
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
			this._Graph = G;
			this._Open = new SortableList();
			this._Closed = new SortableList();
			this.ChoosenHeuristic = AStarSearch.EuclidianHeuristic;
			this.DijkstraHeuristicBalance = 0.5;
		}

		public override bool SearchPath(Node StartNode, Node EndNode)
		{
			bool pathFound;
            SearchStatistics.Start(EndNode);

            lock (this._Graph)
			{
				this.Initialize(StartNode, EndNode);
				while (this.NextStep())
				{
				}
				pathFound = this.PathFound;
			}
            SearchStatistics.Finish(SearchMode.AStar, EndNode, _LeafToGoBackUp?.NbArcsVisited ?? 0);

			return pathFound;
		}

		public Node[][] Open
		{
			get
			{
				Node[][] array = new Node[this._Open.Count][];
				for (int i = 0; i < this._Open.Count; i++)
				{
					array[i] = this.GoBackUpNodes((Track)this._Open[i]);
				}
				return array;
			}
		}

		public Node[][] Closed
		{
			get
			{
				Node[][] array = new Node[this._Closed.Count][];
				for (int i = 0; i < this._Closed.Count; i++)
				{
					array[i] = this.GoBackUpNodes((Track)this._Closed[i]);
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
			this._Closed.Clear();
			this._Open.Clear();
			Track.Target = EndNode;
			this._Open.Add(new Track(StartNode));
			this._NbIterations = 0;
			this._LeafToGoBackUp = null;
		}

		public bool NextStep()
		{
			if (!this.Initialized)
			{
				throw new InvalidOperationException("You must initialize AStar before launching the algorithm.");
			}
			if (this._Open.Count == 0)
			{
				return false;
			}
			this._NbIterations++;
			int index = this._Open.IndexOfMin();
			Track track = (Track)this._Open[index];
			this._Open.RemoveAt(index);
			if (track.Succeed)
			{
				this._LeafToGoBackUp = track;
				this._Open.Clear();
			}
			else
			{
				this.Propagate(track);
				this._Closed.Add(track);
			}
			return this._Open.Count > 0;
		}

		private void Propagate(Track TrackToPropagate)
		{
			foreach (object obj in TrackToPropagate.EndNode.OutgoingArcs)
			{
				Arc arc = (Arc)obj;
				if (arc.Passable && arc.EndNode.Passable)
				{
					Track track = new Track(TrackToPropagate, arc);
					int num = this._Closed.IndexOf(track, this.SameNodesReached);
					int num2 = this._Open.IndexOf(track, this.SameNodesReached);
					if ((num <= 0 || track.Cost < ((Track)this._Closed[num]).Cost) && (num2 <= 0 || track.Cost < ((Track)this._Open[num2]).Cost))
					{
						if (num > 0)
						{
							this._Closed.RemoveAt(num);
						}
						if (num2 > 0)
						{
							this._Open.RemoveAt(num2);
						}
						this._Open.Add(track);
					}
				}
			}
		}

		public bool Initialized
		{
			get
			{
				return this._NbIterations >= 0;
			}
		}

		public bool SearchStarted
		{
			get
			{
				return this._NbIterations > 0;
			}
		}

		public override bool SearchEnded
		{
			get
			{
				return this.SearchStarted && this._Open.Count == 0;
			}
		}

		public override bool PathFound
		{
			get
			{
				return this._LeafToGoBackUp != null;
			}
		}

		public int StepCounter
		{
			get
			{
				return this._NbIterations;
			}
		}

		private void CheckSearchHasEnded()
		{
			if (!this.SearchEnded)
			{
				throw new InvalidOperationException("You cannot get a result unless the search has ended.");
			}
		}

		public bool ResultInformation(out int NbArcsOfPath, out double CostOfPath)
		{
			this.CheckSearchHasEnded();
			if (!this.PathFound)
			{
				NbArcsOfPath = -1;
				CostOfPath = -1.0;
				return false;
			}
			NbArcsOfPath = this._LeafToGoBackUp.NbArcsVisited;
			CostOfPath = this._LeafToGoBackUp.Cost;
			return true;
		}

		public override Node[] PathByNodes
		{
			get
			{
				this.CheckSearchHasEnded();
				if (!this.PathFound)
				{
					return null;
				}
				return this.GoBackUpNodes(this._LeafToGoBackUp);
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

		public Arc[] PathByArcs
		{
			get
			{
				this.CheckSearchHasEnded();
				if (!this.PathFound)
				{
					return null;
				}
				int nbArcsVisited = this._LeafToGoBackUp.NbArcsVisited;
				Arc[] array = new Arc[nbArcsVisited];
				Track track = this._LeafToGoBackUp;
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
				this.CheckSearchHasEnded();
				if (!this.PathFound)
				{
					return null;
				}
				int nbArcsVisited = this._LeafToGoBackUp.NbArcsVisited;
				Point3D[] array = new Point3D[nbArcsVisited + 1];
				Track track = this._LeafToGoBackUp;
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

        private SortableList.Equality SameNodesReached = new SortableList.Equality(Track.SameEndNode);
	}
}
