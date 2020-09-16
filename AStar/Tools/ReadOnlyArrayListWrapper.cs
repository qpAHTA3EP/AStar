using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AStar.Tools
{
    public partial class ReadOnlyListWrapper<ListT> where ListT : IList { }

    public partial class ReadOnlyListWrapper<ListT> : ReadOnlyCollectionBase, IList
    {
        private ListT list;
        public ReadOnlyListWrapper(){ }

        public ReadOnlyListWrapper(ListT list)
        {
            this.list = list;
        }

        internal ReadOnlyListWrapper<ListT> Rebase(ListT list)
        {
            this.list = list;
            return this;
        }

        #region IList
        public object this[int index]
        {
            get => list[index];
            set => throw new InvalidOperationException("Нельзя присваивать занчение элементам списка");
        }

        public bool IsReadOnly => true;

        public bool IsFixedSize => true;

        public int Add(object value)
        {
            throw new InvalidOperationException("Нельзя добавлять новые элементы в списка");
        }

        public void Clear()
        {
            throw new InvalidOperationException("Нельзя очищать список");
        }

        public bool Contains(object value)
        {
            return list.Contains(value);
        }

        public int IndexOf(object value)
        {
            return list.IndexOf(value);
        }

        public void Insert(int index, object value)
        {
            throw new InvalidOperationException("Нельзя вставлять новые элементы в список");
        }

        public void Remove(object value)
        {
            throw new InvalidOperationException("Нельзя удалять элементы из списка");
        }

        public void RemoveAt(int index)
        {
            throw new InvalidOperationException("Нельзя удалять элементы из списка");
        }
        #endregion

        #region Перегрузка ReadOnlyCollectionBase
        public override int Count => list.Count;
        public override IEnumerator GetEnumerator() => list.GetEnumerator();
        #endregion
    }
}
