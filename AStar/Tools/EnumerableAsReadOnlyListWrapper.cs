using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace AStar.Tools
{
    public partial class EnumerableAsReadOnlyListWrapper<TEnumerable> where TEnumerable : IEnumerable { }

    public partial class EnumerableAsReadOnlyListWrapper<TEnumerable> : ReadOnlyCollectionBase, IList
    {
        private TEnumerable collection;
        public EnumerableAsReadOnlyListWrapper(){ }

        public EnumerableAsReadOnlyListWrapper(TEnumerable collection)
        {
            this.collection = collection;
        }

        internal EnumerableAsReadOnlyListWrapper<TEnumerable> Rebase(TEnumerable collection)
        {
            this.collection = collection;
            return this;
        }

        #region IList
        public object this[int index]
        {
            get => throw new NotImplementedException("Операция индексирования не поддерживается над перечислимым контейнером");
            set => throw new InvalidOperationException("Нельзя присваивать значение элементам списка");
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
            foreach (var item in collection)
                if (item.Equals(value))
                    return true;
            return false;
        }

        public int IndexOf(object value)
        {
            throw new NotImplementedException("Операция индексирования не поддерживается над перечислимым контейнером");
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
        public override int Count
        {
            get
            {
                int num = 0;
                foreach (var item in collection)
                    num++;
                return num;
            }
        }

        public override IEnumerator GetEnumerator() => collection.GetEnumerator();
        #endregion
    }
}
