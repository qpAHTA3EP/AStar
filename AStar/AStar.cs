#define PROFILING

using System;
using System.Linq;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;


namespace AStar
{
#if DEBUG && PROFILING
    public class AStarStatistic
    {
        public long Ticks = 0;
        public long Millisecond = 0;
        public long Count = 0;
        public long RepeateCount = 0;
        public long CombatCount = 0;
        public long Length = 0;
    }

    public static class AStarDiagnostic
    {
        private static Node targetNode; // конечная точка пути
        private static bool inCombat; // поиск выполняется в бою
        private static bool repeated; // повторный поиск к целевой точке предыдущего поиска
        private static long totalCount; // общее количество поиска путей
        private static long totalCombatCount; // общее количество поиска путей
        private static long totalRepeatCount; // общее количество повторных поисков
        private static long totalTicks; // общее количество повторных поисков
        private static long totalMs; // общее количество повторных поисков
        private static long totalLen; // общее количество повторных поисков
        public static Dictionary<Node, AStarStatistic> Metrics = new Dictionary<Node, AStarStatistic>();
        private static Stopwatch sw = new Stopwatch();

        public static void Start(Node tarNode)
        {
            if (tarNode != null)
            {
                totalCount++;
                if (targetNode == tarNode)
                {
                    totalRepeatCount++;
                    repeated = true;
                }
                if (MyNW.Internals.EntityManager.LocalPlayer.InCombat
                    /*&& !Astral.Quester.API.IgnoreCombat*/)
                {
                    totalCombatCount++;
                    inCombat = true;
                }

                targetNode = tarNode;
                sw.Restart();
            }
        }
        public static void Finish(Node tarNode, long len)
        {
            sw.Stop();
            if (tarNode != null && targetNode == tarNode)
            {
                if(Metrics.ContainsKey(targetNode))
                {
                    AStarStatistic stat = Metrics[targetNode];
                    stat.Ticks += sw.ElapsedTicks;
                    stat.Millisecond += sw.ElapsedMilliseconds;
                    stat.Count += 1;
                    if (inCombat)
                        stat.CombatCount += 1;
                    if (repeated)
                        stat.RepeateCount += 1;
                    stat.Length += len;
                }
                else
                {
                    AStarStatistic stat = new AStarStatistic() {
                                                Ticks = sw.ElapsedTicks,
                                                Millisecond = sw.ElapsedMilliseconds,
                                                Count = 1,
                                                CombatCount = inCombat ? 1 : 0,
                                                Length = len
                                            };

                    Metrics.Add(targetNode, stat);
                }
                totalTicks += sw.ElapsedTicks;
                totalMs += sw.ElapsedMilliseconds;
                totalLen += len;
            }

            inCombat = false;
            repeated = false;
        }

        public static string SaveLog(string fileName = "")
        {
            if (string.IsNullOrEmpty(fileName)
                || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) != -1
                || fileName.IndexOfAny(Path.GetInvalidPathChars()) != -1)
                fileName = $".\\Logs\\AStarDiagnostic_{DateTime.Now.Year}-{DateTime.Now.Month}-{DateTime.Now.Day}_{DateTime.Now.Hour}-{DateTime.Now.Minute}-{DateTime.Now.Second}.log";

            bool exportOk = false;
            if(Metrics.Count > 0)
                using (StreamWriter file = new StreamWriter(fileName))
                {
                    file.WriteLine($"TotalCount: {totalCount}");
                    file.WriteLine($"TotalRepeatCount: {totalRepeatCount}");
                    file.WriteLine($"TotalCombatCount: {totalCombatCount}");
                    file.WriteLine($"TotalTicks: {totalTicks}");
                    file.WriteLine($"TotalMillisecond: {totalMs}");
                    file.WriteLine($"TotalLength: {totalLen}");
                    file.WriteLine($"----------------------------------------------------------------------------------------------------------");
                    foreach (var stat in Metrics)
                    {
                        file.WriteLine($"Path to Node<{stat.Key.X.ToString("N3")}, {stat.Key.Y.ToString("N3")}, {stat.Key.Y.ToString("N3")}>;\tTotalCount: {stat.Value.Count};\tCombatCount: {stat.Value.CombatCount};\tRepeatCount: {stat.Value.RepeateCount};\tTotalLength: {stat.Value.Length};\tTotalMs: {stat.Value.Millisecond};\tTotalTicks: {stat.Value.Ticks}");
                    }
                    exportOk = true;
                }
            if (exportOk)
            {
                totalCount = 0;
                totalRepeatCount = 0;
                totalCombatCount = 0;
                totalTicks = 0;
                totalMs = 0;
                totalLen = 0;

                Metrics.Clear();
                return fileName;
            }
            else return string.Empty;
        }
    }
#endif

	public class AStar
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

		public AStar(Graph G)
		{
			this._Graph = G;
			this._Open = new SortableList();
			this._Closed = new SortableList();
			this.ChoosenHeuristic = AStar.EuclidianHeuristic;
			this.DijkstraHeuristicBalance = 0.5;
		}

		public bool SearchPath(Node StartNode, Node EndNode)
		{
			bool pathFound;
#if DEBUG && PROFILING
            AStarDiagnostic.Start(EndNode);
#endif
            lock (this._Graph)
			{
				this.Initialize(StartNode, EndNode);
				while (this.NextStep())
				{
				}
				pathFound = this.PathFound;
			}
#if DEBUG && PROFILING
            AStarDiagnostic.Finish(EndNode, _LeafToGoBackUp?.NbArcsVisited ?? 0);
#endif
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

		public bool SearchEnded
		{
			get
			{
				return this.SearchStarted && this._Open.Count == 0;
			}
		}

		public bool PathFound
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

		public Node[] PathByNodes
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
