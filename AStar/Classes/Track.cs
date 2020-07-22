using System;

namespace AStar
{
	// Token: 0x0200000A RID: 10
	internal class Track : IComparable
	{
		// Token: 0x1700002C RID: 44
		// (get) Token: 0x0600008E RID: 142 RVA: 0x00004024 File Offset: 0x00002224
		// (set) Token: 0x0600008D RID: 141 RVA: 0x0000401C File Offset: 0x0000221C
		public static Node Target
		{
			get
			{
				return Track._Target;
			}
			set
			{
				Track._Target = value;
			}
		}

		// Token: 0x1700002D RID: 45
		// (get) Token: 0x0600008F RID: 143 RVA: 0x0000402B File Offset: 0x0000222B
		// (set) Token: 0x06000090 RID: 144 RVA: 0x00004032 File Offset: 0x00002232
		public static double DijkstraHeuristicBalance
		{
			get
			{
				return Track._Coeff;
			}
			set
			{
				if (value < 0.0 || value > 1.0)
				{
					throw new ArgumentException("The coefficient which balances the respective influences of Dijkstra and the Heuristic must belong to [0; 1].\r\n-> 0 will minimize the number of nodes explored but will not take the real cost into account.\r\n-> 0.5 will minimize the cost without developing more nodes than necessary.\r\n-> 1 will only consider the real cost without estimating the remaining cost.");
				}
				Track._Coeff = value;
			}
		}

		// Token: 0x1700002E RID: 46
		// (get) Token: 0x06000092 RID: 146 RVA: 0x00004065 File Offset: 0x00002265
		// (set) Token: 0x06000091 RID: 145 RVA: 0x0000405D File Offset: 0x0000225D
		public static Heuristic ChoosenHeuristic
		{
			get
			{
				return Track._ChoosenHeuristic;
			}
			set
			{
				Track._ChoosenHeuristic = value;
			}
		}

		// Token: 0x1700002F RID: 47
		// (get) Token: 0x06000093 RID: 147 RVA: 0x0000406C File Offset: 0x0000226C
		public int NbArcsVisited
		{
			get
			{
				return this._NbArcsVisited;
			}
		}

		// Token: 0x17000030 RID: 48
		// (get) Token: 0x06000094 RID: 148 RVA: 0x00004074 File Offset: 0x00002274
		public double Cost
		{
			get
			{
				return this._Cost;
			}
		}

		// Token: 0x17000031 RID: 49
		// (get) Token: 0x06000095 RID: 149 RVA: 0x0000407C File Offset: 0x0000227C
		public virtual double Evaluation
		{
			get
			{
				return Track._Coeff * this._Cost + (1.0 - Track._Coeff) * Track._ChoosenHeuristic(this.EndNode, Track._Target);
			}
		}

		// Token: 0x17000032 RID: 50
		// (get) Token: 0x06000096 RID: 150 RVA: 0x000040B0 File Offset: 0x000022B0
		public bool Succeed
		{
			get
			{
				return this.EndNode == Track._Target;
			}
		}

		// Token: 0x06000097 RID: 151 RVA: 0x000040BF File Offset: 0x000022BF
		public Track(Node GraphNode)
		{
			if (Track._Target == null)
			{
				throw new InvalidOperationException("You must specify a target Node for the Track class.");
			}
			this._Cost = 0.0;
			this._NbArcsVisited = 0;
			this.Queue = null;
			this.EndNode = GraphNode;
		}

		// Token: 0x06000098 RID: 152 RVA: 0x00004100 File Offset: 0x00002300
		public Track(Track PreviousTrack, Arc Transition)
		{
			if (Track._Target == null)
			{
				throw new InvalidOperationException("You must specify a target Node for the Track class.");
			}
			this.Queue = PreviousTrack;
			this._Cost = this.Queue.Cost + Transition.Cost;
			this._NbArcsVisited = this.Queue._NbArcsVisited + 1;
			this.EndNode = Transition.EndNode;
		}

		// Token: 0x06000099 RID: 153 RVA: 0x00004164 File Offset: 0x00002364
		public int CompareTo(object Objet)
		{
			Track track = (Track)Objet;
			return this.Evaluation.CompareTo(track.Evaluation);
		}

		// Token: 0x0600009A RID: 154 RVA: 0x0000418C File Offset: 0x0000238C
		public static bool SameEndNode(object O1, object O2)
		{
			Track track = O1 as Track;
			Track track2 = O2 as Track;
			if (track == null || track2 == null)
			{
				throw new ArgumentException("Objects must be of 'Track' type.");
			}
			return track.EndNode == track2.EndNode;
		}

		// Token: 0x04000014 RID: 20
		private static Node _Target = null;

		// Token: 0x04000015 RID: 21
		private static double _Coeff = 0.5;

		// Token: 0x04000016 RID: 22
		private static Heuristic _ChoosenHeuristic = AStarSearch.EuclidianHeuristic;

		// Token: 0x04000017 RID: 23
		public Node EndNode;

		// Token: 0x04000018 RID: 24
		public Track Queue;

		// Token: 0x04000019 RID: 25
		private int _NbArcsVisited;

		// Token: 0x0400001A RID: 26
		private double _Cost;
	}
}
