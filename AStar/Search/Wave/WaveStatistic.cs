using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AStar.Search.Wave
{
    public class WaveStatistic
    {
        public bool IsValid { get; set; }
        public Node Start { get; private set; }
        public Node End { get; private set; }

        public SortedSet<Node> ProcessingQueue { get; private set; } = new SortedSet<Node>();
        public uint WaveRadius { get; private set; }

        public void Reset()
        {
            Start = End = null;
            IsValid = false;
            ProcessingQueue.Clear();
            WaveRadius = 0;
        }
    }
}
