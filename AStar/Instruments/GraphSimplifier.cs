using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AStar
{
    public partial class Instruments
    {
        /// <summary>
        /// Упрощение графа
        /// путем уменьшения количества вершин
        /// </summary>
        /// <param name="inGraph"></param>
        /// <param name="outGraph"></param>
        /// <param name="distance">минимальное расстояние между вершинами</param>
        /// <param name="saveInpassableNode?>Сохранять вершины</param>
        public static void GraphSimplyfier(Graph inGraph, out Graph outGraph, double distance, bool saveInpassableNode = false)
        {
            if (inGraph != null && inGraph.Nodes.Count > 0)
            {
                outGraph = new Graph();
                Queue<Node> queue = new Queue<Node>();
                
                // Производим поиск концевых вершин (имеющих связь только с одной вершиной)
                foreach(Node node in inGraph.Nodes)
                {
                    if(!saveInpassableNode || node.Passable)
                    {
                        if(node.IncomingArcs.Count == 0)
                        {
                            // вершина не имеет входящих ребер
                            if(node.OutgoingArcs.Count == 0)
                            {
                                // пустая вершина
                                continue;
                            }
                            else if(node.IncomingArcs.Count < 2)
                            {
                                // вершина имеет вединственное исходящее ребро
                                // следовательно вершина является начальной для однонаправленного пути
                                SimplifyOutgoingPath(node, ref outGraph, distance);
                            }
                            else
                            {
                                // исходящее ветвление

                            }
                        }
                        else if (node.IncomingArcs.Count < 2)
                        {
                            if(node.OutgoingArcs.Count == 0)
                            {
                                // вершина не имеет исходящих ребер
                                // следовательно вершина является конченой в однонаправленном пути
                                SimplifyIncomingPath(node, ref outGraph, distance);
                            }
                            else if (node.OutgoingArcs.Count < 2)
                            {
                                if(((Arc)node.IncomingArcs[0]).StartNode == ((Arc)node.OutgoingArcs[0]).EndNode)
                                {
                                    // вершина исходящего ребра совпадает с вершиной входящего
                                    // следовательно вершина node является конечной
                                    queue.Enqueue(node);
                                    SimplifyLinearPath(node, ref outGraph, distance);
                                }
                            }
                            else
                            {
                                // вершина имеет несколько исходящих ребер
                                // и одно входящее
                                // сложное ветвление

                            }
                        }
                        else
                        {

                        }
                    }
                }
            }
            outGraph = null;
        }

        /// <summary>
        /// Упрощение однонаправленного пути, заканчивающего вершиной node
        /// </summary>
        /// <param name="node"></param>
        /// <param name="graph"></param>
        private static void SimplifyIncomingPath(Node node, ref Graph graph, double distance)
        {
            throw new NotImplementedException();

        }

        /// <summary>
        /// Упрощение однонаправленного пути, начинающегося вершиной node
        /// </summary>
        /// <param name="node"></param>
        /// <param name="graph"></param>
        private static void SimplifyOutgoingPath(Node node, ref Graph graph, double distance)
        {
            throw new NotImplementedException();

        }

        /// <summary>
        /// Упрощение линейного двунаправленного пути, начинающегося вершиной node
        /// </summary>
        /// <param name="node"></param>
        /// <param name="graph"></param>
        private static void SimplifyLinearPath(Node node, ref Graph graph, double distance)
        {
            throw new NotImplementedException();

        }

        /// <summary>
        /// Вычисление коссинуса улаг между проэкциями векторов на oXY
        /// </summary>
        /// <param name="o">точка началаве кторов</param>
        /// <param name="n1">точка конец первого вектора</param>
        /// <param name="n2"></param>
        /// <returns></returns>
        public static double CosinusXY(Node o, Node n1, Node n2)
        {
            // вычисляем Cos угла между векторами из формулы скалярного произведения векторов
            // a * b = |a| * |b| * cos (alpha) = xa * xb + ya * yb

            double dx1 = n1.X - o.X;
            double dy1 = n1.Y - o.Y;
            //double dz1 = n1.Z - o.Y;
            double dx2 = n2.X - o.X;
            double dy2 = n2.X - o.Y;
            //double dz2 = n2.Z - o.Z;


            double cos = (dx1 * dx2 + dy1 * dy2)
                          / Math.Sqrt((dx1 * dx1 + dy1 * dy1)
                                      * (dx2 * dx2 + dy2 * dy2));

            return cos;
        }

    }
}
