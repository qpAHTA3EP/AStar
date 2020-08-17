using System;
using AStar.Search.AStar;

namespace AStar
{
	internal class Track : IComparable
	{
		public static Node Target
		{
			get => _Target;
            set => _Target = value;
        }
		private static Node _Target = null;

		public static double DijkstraHeuristicBalance
		{
			get => _Coeff;
            set
			{
				if (value < 0.0 || value > 1.0)
					throw new ArgumentException("The coefficient which balances the respective influences of Dijkstra and the Heuristic must belong to [0; 1].\r\n-> 0 will minimize the number of nodes explored but will not take the real cost into account.\r\n-> 0.5 will minimize the cost without developing more nodes than necessary.\r\n-> 1 will only consider the real cost without estimating the remaining cost.");

                _Coeff = value;
			}
		}
		private static double _Coeff = 0.5;

		public static Heuristic ChoosenHeuristic
		{
			get => _ChoosenHeuristic;
            set => _ChoosenHeuristic = value;
        }
		private static Heuristic _ChoosenHeuristic = AStarSearch.EuclidianHeuristic;

		public int NbArcsVisited => _NbArcsVisited;
		private int _NbArcsVisited;

		public double Cost => _Cost;
		private double _Cost;

		public virtual double Evaluation
		{
			get
			{
				return _Coeff * _Cost + (1.0 - _Coeff) * _ChoosenHeuristic(EndNode, _Target);
			}
		}

		public bool Succeed => Equals(EndNode, _Target);

		public Track(Node GraphNode)
		{
			if (_Target is null)
				throw new InvalidOperationException("You must specify a target Node for the Track class.");

            _Cost = 0.0;
			_NbArcsVisited = 0;
			Queue = null;
			EndNode = GraphNode;
		}

		public Track(Track PreviousTrack, Arc Transition)
		{
			if (_Target is null)
				throw new InvalidOperationException("You must specify a target Node for the Track class.");

            Queue = PreviousTrack;
			_Cost = this.Queue.Cost + Transition.Cost;
			_NbArcsVisited = this.Queue._NbArcsVisited + 1;
			EndNode = Transition.EndNode;
		}

		public int CompareTo(object obj)
		{
            if(obj is Track track)
			    return Evaluation.CompareTo(track.Evaluation);
            return -1;
        }

		public static bool SameEndNode(object O1, object O2)
		{
            if (!(O1 is Track track) || !(O2 is Track track2))
			{
				throw new ArgumentException("Objects must be of 'Track' type.");
			}
			return Equals(track.EndNode, track2.EndNode);
		}

        public Node EndNode;
		public Track Queue;
	}
}
