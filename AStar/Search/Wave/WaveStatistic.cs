using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AStar.Search.Wave
{
    public class WaveStatistic
    {
        public bool IsValid { get; set; }
        public Node Start { get; }
        public Node End { get; }

        public LinkedList<Node> ProcessingQueue { get; private set; }
        public uint WaveRadius { get; private set; }
    }
}
