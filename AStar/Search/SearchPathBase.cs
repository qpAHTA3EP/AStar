using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AStar
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
        /// Список узлов, определяющих найденный уть
        /// </summary>
        public abstract Node[] PathByNodes { get; }

        /// <summary>
        /// Флаг, указыающий на завершение поиска
        /// </summary>
        public abstract bool SearchEnded { get; }

        /// <summary>
        /// Флаг, указывающие на успешное построение пути
        /// </summary>
        public abstract bool PathFound { get; }
    }
}
