using MyNW.Classes;
using System.Collections.Generic;

namespace AStar.Search
{
    public abstract class SearchPathBase
    {
        /// <summary>
        /// Поиск пути из узла StartNode к узлу EndNode
        /// Построенный путь доступен через поле PathByNodes
        /// </summary>
        /// <param name="StartNode"></param>
        /// <param name="EndNode"></param>
        /// <returns></returns>
        public abstract bool SearchPath(Node StartNode, Node EndNode);

        /// <summary>
        /// Массив узлов, определяющих найденный уть
        /// </summary>
        public abstract Node[] PathByNodes { get; }

#if false
        /// <summary>
        /// Список узлов, определяющих найденный уть
        /// </summary>
        public abstract IEnumerable<Vector3> PathNodes { get; }  
#endif

        /// <summary>
        /// Длина пути 
        /// </summary>
        public abstract double PathLength { get; }

        /// <summary>
        /// Флаг, указывающие на успешное построение пути
        /// </summary>
        public abstract bool PathFound { get; }

        /// <summary>
        /// Замена графа в 
        /// </summary>
        /// <param name="g"></param>
        public abstract void Rebase(Graph g);
    }
}
