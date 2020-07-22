using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;

namespace AStar
{
    public enum SearchMode
    {
        AStar,
        WaveFirst,
        WaveRepeated
    }

    public class SearchMetric
    {
        public long TotalTicks = 0;
        public long TotalMillisecond = 0;
        public long TotalCount = 0;
        public long RepeateCount = 0;
        public long CombatCount = 0;
        public long SucceedCount = 0;
        public long TotalLength = 0;
    }

    public class NodeSearchStatistic
    {
        public SearchMetric AStar = new SearchMetric();
        public SearchMetric WaveFirst = new SearchMetric();
        public SearchMetric WaveRepeated = new SearchMetric();
    }

    public static class SearchStatistics
    {
        private static Node targetNode; // конечная точка пути
        private static bool inCombat; // поиск выполняется в бою
        private static bool repeated; // повторный поиск к целевой точке предыдущего поиска
        private static long totalCount; // общее количество поиска путей
        private static long totalCombatCount; // общее количество поиска путей
        private static long totalRepeatCount; // общее количество повторных поисков
        private static long totalCountAStar;  // общее количество повторных поисков алгоритмом AStar
        private static long totalCountWaveFirst;  // общее количество первичным поисков Wave
        private static long totalCountWaveRepeat; // общее количество повторных поисков алгоритмом Wave
        private static long totalTicks; // общее количество повторных поисков
        private static long totalMs;    // общее количество повторных поисков
        private static long totalLen; // общая длина пути
        public static Dictionary<Node, NodeSearchStatistic> Statistics = new Dictionary<Node, NodeSearchStatistic>();
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
                    && !Astral.Quester.API.IgnoreCombat)
                {
                    totalCombatCount++;
                    inCombat = true;
                }

                targetNode = tarNode;
                sw.Restart();
            }
        }
        public static void Finish(SearchMode mode, Node tarNode, long len)
        {
            if (tarNode != null && targetNode == tarNode)
            {
            sw.Stop();
            switch(mode)
            {
                case SearchMode.AStar:
                        if (Statistics.ContainsKey(targetNode))
                        {
                            NodeSearchStatistic stat = Statistics[targetNode];
                            stat.AStar.TotalTicks += sw.ElapsedTicks;
                            stat.AStar.TotalMillisecond += sw.ElapsedMilliseconds;
                            stat.AStar.TotalCount += 1;
                            if (inCombat)
                                stat.AStar.CombatCount += 1;
                            if (repeated)
                                stat.AStar.RepeateCount += 1;
                            stat.AStar.TotalLength += len;
                            if (len > 0)
                                stat.AStar.SucceedCount += 1;
                        }
                        else
                        {
                            NodeSearchStatistic stat = new NodeSearchStatistic() {
                                                            AStar = new SearchMetric() {
                                                                TotalTicks = sw.ElapsedTicks,
                                                                TotalMillisecond = sw.ElapsedMilliseconds,
                                                                TotalCount = 1,
                                                                CombatCount = inCombat ? 1 : 0,
                                                                TotalLength = len,
                                                                SucceedCount = (len > 0) ? 1 : 0
                                                            }
                                                        };

                            Statistics.Add(targetNode, stat);
                        }
                        totalCountAStar++;
                        break;
                    case SearchMode.WaveFirst:
                        if (Statistics.ContainsKey(targetNode))
                        {
                            NodeSearchStatistic stat = Statistics[targetNode];
                            stat.WaveFirst.TotalTicks += sw.ElapsedTicks;
                            stat.WaveFirst.TotalMillisecond += sw.ElapsedMilliseconds;
                            stat.WaveFirst.TotalCount += 1;
                            if (inCombat)
                                stat.WaveFirst.CombatCount += 1;
                            if (repeated)
                                stat.WaveFirst.RepeateCount += 1;
                            stat.WaveFirst.TotalLength += len;
                            if (len > 0)
                                stat.WaveFirst.SucceedCount += 1;
                        }
                        else
                        {
                            NodeSearchStatistic stat = new NodeSearchStatistic() {
                                                            WaveFirst = new SearchMetric() {
                                                                TotalTicks = sw.ElapsedTicks,
                                                                TotalMillisecond = sw.ElapsedMilliseconds,
                                                                TotalCount = 1,
                                                                CombatCount = inCombat ? 1 : 0,
                                                                TotalLength = len,
                                                                SucceedCount = (len > 0) ? 1 : 0
                                                            }
                                                        };

                            Statistics.Add(targetNode, stat);
                        }
                        totalCountWaveFirst++;
                        break;
                    case SearchMode.WaveRepeated:
                        if (Statistics.ContainsKey(targetNode))
                        {
                            NodeSearchStatistic stat = Statistics[targetNode];
                            stat.WaveRepeated.TotalTicks += sw.ElapsedTicks;
                            stat.WaveRepeated.TotalMillisecond += sw.ElapsedMilliseconds;
                            stat.WaveRepeated.TotalCount += 1;
                            if (inCombat)
                                stat.WaveRepeated.CombatCount += 1;
                            if (repeated)
                                stat.WaveRepeated.RepeateCount += 1;
                            stat.WaveFirst.TotalLength += len;
                            if (len > 0)
                                stat.WaveRepeated.SucceedCount += 1;
                        }
                        else
                        {
                            NodeSearchStatistic stat = new NodeSearchStatistic() {
                                                            WaveRepeated = new SearchMetric() {
                                                                    TotalTicks = sw.ElapsedTicks,
                                                                    TotalMillisecond = sw.ElapsedMilliseconds,
                                                                    TotalCount = 1,
                                                                    CombatCount = inCombat ? 1 : 0,
                                                                    TotalLength = len,
                                                                    SucceedCount = (len > 0) ? 1 : 0
                                                                }
                                                            };

                            Statistics.Add(targetNode, stat);
                        }
                        totalCountWaveRepeat++;
                        break;
                }

                totalTicks += sw.ElapsedTicks;
                totalMs += sw.ElapsedMilliseconds;
                //otalLen += len;
            }

            inCombat = false;
            repeated = false;
        }

        public static string SaveLog(string fileName = "")
        {
            if (string.IsNullOrEmpty(fileName)
                || fileName.IndexOfAny(Path.GetInvalidFileNameChars()) != -1
                || fileName.IndexOfAny(Path.GetInvalidPathChars()) != -1)
                fileName = $".\\Logs\\PathfindDiagnostic_{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")}.log";

            
            if (Statistics.Count > 0)
                using (StreamWriter file = new StreamWriter(fileName))
                {
                    file.WriteLine($"TotalCount: {totalCount}");
                    file.WriteLine($"AStar TotalCount: {totalCountAStar}");
                    file.WriteLine($"WaveFirst TotalCount: {totalCountWaveFirst}");
                    file.WriteLine($"WaveRepeat TotalCount: {totalCountWaveRepeat}");
                    file.WriteLine($"TotalRepeatCount: {totalRepeatCount}");
                    file.WriteLine($"TotalCombatCount: {totalCombatCount}");
                    file.WriteLine($"TotalTicks: {totalTicks}");
                    file.WriteLine($"TotalMillisecond: {totalMs}");
                    file.WriteLine($"TotalLength: {totalLen}");
                    file.WriteLine($"----------------------------------------------------------------------------------------------------------");
                    foreach (var stat in Statistics)
                    {
                        file.WriteLine($"AStar to Node<{stat.Key.X.ToString("N3")}, {stat.Key.Y.ToString("N3")}, {stat.Key.Y.ToString("N3")}>;\t\tTotalCount: {stat.Value.AStar.TotalCount};\tSucceedCount: {stat.Value.AStar.SucceedCount};\tCombatCount: {stat.Value.AStar.CombatCount};\tRepeatCount: {stat.Value.AStar.RepeateCount};\tTotalLength: {stat.Value.AStar.TotalLength};\tTotalMs: {stat.Value.AStar.TotalMillisecond};\tTotalTicks: {stat.Value.AStar.TotalTicks}");
                        file.WriteLine($"WaveFirst to Node<{stat.Key.X.ToString("N3")}, {stat.Key.Y.ToString("N3")}, {stat.Key.Y.ToString("N3")}>;\t\tTotalCount: {stat.Value.WaveFirst.TotalCount};\tSucceedCount: {stat.Value.WaveFirst.SucceedCount};\tCombatCount: {stat.Value.WaveFirst.CombatCount};\tRepeatCount: {stat.Value.WaveFirst.RepeateCount};\tTotalLength: {stat.Value.WaveFirst.TotalLength};\tTotalMs: {stat.Value.WaveFirst.TotalMillisecond};\tTotalTicks: {stat.Value.WaveFirst.TotalTicks}");
                        file.WriteLine($"WaveSecond to Node<{stat.Key.X.ToString("N3")}, {stat.Key.Y.ToString("N3")}, {stat.Key.Y.ToString("N3")}>;\t\tTotalCount: {stat.Value.WaveRepeated.TotalCount};\tSucceedCount: {stat.Value.WaveRepeated.SucceedCount};\tCombatCount: {stat.Value.WaveRepeated.CombatCount};\tRepeatCount: {stat.Value.WaveRepeated.RepeateCount};\tTotalLength: {stat.Value.WaveRepeated.TotalLength};\tTotalMs: {stat.Value.WaveRepeated.TotalMillisecond};\tTotalTicks: {stat.Value.WaveRepeated.TotalTicks}");
                    }
                }

            totalCount = 0;
            totalCountAStar = 0;
            totalCountWaveFirst = 0;
            totalCountWaveRepeat = 0;
            totalRepeatCount = 0;
            totalCombatCount = 0;
            totalTicks = 0;
            totalMs = 0;
            totalLen = 0;

            Statistics.Clear();

            return fileName;
        }
    }
}
