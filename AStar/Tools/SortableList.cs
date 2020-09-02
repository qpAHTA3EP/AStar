using System;
using System.Collections;

namespace AStar
{
	[Serializable]
	public class SortableList : IList, ICollection, IEnumerable, ICloneable
	{
		public SortableList()
		{
			this.InitProperties(null, 0);
		}

		public SortableList(int Capacity)
		{
			this.InitProperties(null, Capacity);
		}

		public SortableList(IComparer Comparer)
		{
			this.InitProperties(Comparer, 0);
		}

		public SortableList(IComparer Comparer, int Capacity)
		{
			this.InitProperties(Comparer, Capacity);
		}

		public bool IsSorted
		{
			get
			{
				return this._IsSorted;
			}
		}

		public bool KeepSorted
		{
			get
			{
				return this._KeepSorted;
			}
			set
			{
				if (value && !this._IsSorted)
				{
					throw new InvalidOperationException("The SortableList can only be kept sorted if it is sorted.");
				}
				this._KeepSorted = value;
			}
		}

		public bool AddDuplicates
		{
			get
			{
				return this._AddDuplicates;
			}
			set
			{
				this._AddDuplicates = value;
			}
		}

		public object this[int Index]
		{
			get
			{
				if (Index >= this._List.Count || Index < 0)
				{
					throw new ArgumentOutOfRangeException("Index is less than zero or Index is greater than Count.");
				}
				return this._List[Index];
			}
			set
			{
				if (this._KeepSorted)
				{
					throw new InvalidOperationException("[] operator cannot be used to set a value if KeepSorted property is set to true.");
				}
				if (Index >= this._List.Count || Index < 0)
				{
					throw new ArgumentOutOfRangeException("Index is less than zero or Index is greater than Count.");
				}
				if (this.ObjectIsCompliant(value))
				{
					object obj = (Index > 0) ? this._List[Index - 1] : null;
					object obj2 = (Index < this.Count - 1) ? this._List[Index + 1] : null;
					if ((obj != null && this._Comparer.Compare(obj, value) > 0) || (obj2 != null && this._Comparer.Compare(value, obj2) > 0))
					{
						this._IsSorted = false;
					}
					this._List[Index] = value;
				}
			}
		}

		public int Add(object O)
		{
			int result = -1;
			if (this.ObjectIsCompliant(O))
			{
				if (this._KeepSorted)
				{
					int num = this.IndexOf(O);
					int num2 = (num >= 0) ? num : (-num - 1);
					if (num2 >= this.Count)
					{
						this._List.Add(O);
					}
					else
					{
						this._List.Insert(num2, O);
					}
					result = num2;
				}
				else
				{
					this._IsSorted = false;
					result = this._List.Add(O);
				}
			}
			return result;
		}

		public bool Contains(object O)
		{
			if (!this._IsSorted)
			{
				return this._List.Contains(O);
			}
			return this._List.BinarySearch(O, this._Comparer) >= 0;
		}

		// Token: 0x0600002E RID: 46 RVA: 0x00002940 File Offset: 0x00000B40
		public int IndexOf(object O)
		{
			int i;
			if (this._IsSorted)
			{
				for (i = this._List.BinarySearch(O, this._Comparer); i > 0; i--)
				{
					if (!this._List[i - 1].Equals(O))
					{
						break;
					}
				}
			}
			else
			{
				i = this._List.IndexOf(O);
			}
			return i;
		}

		public bool IsFixedSize
		{
			get
			{
				return this._List.IsFixedSize;
			}
		}

		public bool IsReadOnly
		{
			get
			{
				return this._List.IsReadOnly;
			}
		}

		public void Clear()
		{
			this._List.Clear();
		}

		public void Insert(int Index, object O)
		{
			if (this._KeepSorted)
			{
				throw new InvalidOperationException("Insert method cannot be called if KeepSorted property is set to true.");
			}
			if (Index >= this._List.Count || Index < 0)
			{
				throw new ArgumentOutOfRangeException("Index is less than zero or Index is greater than Count.");
			}
			if (this.ObjectIsCompliant(O))
			{
				object obj = (Index > 0) ? this._List[Index - 1] : null;
				object obj2 = this._List[Index];
				if ((obj != null && this._Comparer.Compare(obj, O) > 0) || (obj2 != null && this._Comparer.Compare(O, obj2) > 0))
				{
					this._IsSorted = false;
				}
				this._List.Insert(Index, O);
			}
		}

		public void Remove(object Value)
		{
			this._List.Remove(Value);
		}

		public void RemoveAt(int Index)
		{
			this._List.RemoveAt(Index);
		}

		public void CopyTo(Array array, int arrayIndex)
		{
			this._List.CopyTo(array, arrayIndex);
		}

		public int Count
		{
			get
			{
				return this._List.Count;
			}
		}

		public bool IsSynchronized
		{
			get
			{
				return this._List.IsSynchronized;
			}
		}

		public object SyncRoot
		{
			get
			{
				return this._List.SyncRoot;
			}
		}

		public IEnumerator GetEnumerator()
		{
			return this._List.GetEnumerator();
		}

		public object Clone()
		{
			return new SortableList(this._Comparer, this._List.Capacity)
			{
				_List = (ArrayList)this._List.Clone(),
				_AddDuplicates = this._AddDuplicates,
				_IsSorted = this._IsSorted,
				_KeepSorted = this._KeepSorted
			};
		}

		public int IndexOf(object O, int Start)
		{
			int i;
			if (this._IsSorted)
			{
				for (i = this._List.BinarySearch(Start, this._List.Count - Start, O, this._Comparer); i > Start; i--)
				{
					if (!this._List[i - 1].Equals(O))
					{
						break;
					}
				}
			}
			else
			{
				i = this._List.IndexOf(O, Start);
			}
			return i;
		}

		public int IndexOf(object O, SortableList.Equality AreEqual)
		{
			for (int i = 0; i < this._List.Count; i++)
			{
				if (AreEqual(this._List[i], O))
				{
					return i;
				}
			}
			return -1;
		}

		public int IndexOf(object O, int Start, SortableList.Equality AreEqual)
		{
			if (Start < 0 || Start >= this._List.Count)
			{
				throw new ArgumentException("Start index must belong to [0; Count-1].");
			}
			for (int i = Start; i < this._List.Count; i++)
			{
				if (AreEqual(this._List[i], O))
				{
					return i;
				}
			}
			return -1;
		}

		public int Capacity
		{
			get
			{
				return this._List.Capacity;
			}
			set
			{
				this._List.Capacity = value;
			}
		}

		public override string ToString()
		{
			string text = "{";
			for (int i = 0; i < this._List.Count; i++)
			{
				text = text + this._List[i].ToString() + ((i != this._List.Count - 1) ? "; " : "}");
			}
			return text;
		}

		public override bool Equals(object O)
		{
			SortableList sortableList = (SortableList)O;
			if (sortableList.Count != this.Count)
			{
				return false;
			}
			for (int i = 0; i < this.Count; i++)
			{
				if (!sortableList[i].Equals(this[i]))
				{
					return false;
				}
			}
			return true;
		}

		public override int GetHashCode()
		{
			return this._List.GetHashCode();
		}

		public void Sort()
		{
			if (this._IsSorted)
			{
				return;
			}
			this._List.Sort(this._Comparer);
			this._IsSorted = true;
		}

		public void AddRange(ICollection C)
		{
			if (this._KeepSorted)
			{
                foreach (object o in C)
                {
                    this.Add(o);
                }
                /*using (IEnumerator enumerator = C.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						object o = enumerator.Current;
						this.Add(o);
					}
					return;
				}*/
            }
			this._List.AddRange(C);
		}

		public void InsertRange(int Index, ICollection C)
		{
			if (this._KeepSorted)
			{
                foreach(object o in C)
                {
                    this.Insert(Index++, o);
                }
				/*using (IEnumerator enumerator = C.GetEnumerator())
				{
					while (enumerator.MoveNext())
					{
						object o = enumerator.Current;
						this.Insert(Index++, o);
					}
					return;
				}*/
			}
			this._List.InsertRange(Index, C);
		}

		public void LimitNbOccurrences(object Value, int NbValuesToKeep)
		{
			if (Value == null)
			{
				throw new ArgumentNullException("Value");
			}
			int num = 0;
			while ((num = this.IndexOf(Value, num)) >= 0)
			{
				if (NbValuesToKeep <= 0)
				{
					this._List.RemoveAt(num);
				}
				else
				{
					num++;
					NbValuesToKeep--;
				}
				if (this._IsSorted && this._Comparer.Compare(this._List[num], Value) > 0)
				{
					return;
				}
			}
		}

		public void RemoveDuplicates()
		{
			if (this._IsSorted)
			{
				int i = 0;
				while (i < this.Count - 1)
				{
					if (this._Comparer.Compare(this[i], this[i + 1]) == 0)
					{
						this.RemoveAt(i);
					}
					else
					{
						i++;
					}
				}
				return;
			}
			for (int j = 0; j >= 0; j++)
			{
				int i = j + 1;
				while (i > 0)
				{
					if (j != i && this._Comparer.Compare(this[j], this[i]) == 0)
					{
						this.RemoveAt(i);
					}
					else
					{
						i++;
					}
				}
			}
		}

		public int IndexOfMin()
		{
			int result = -1;
			if (this._List.Count > 0)
			{
				result = 0;
				object x = this._List[0];
				if (!this._IsSorted)
				{
					for (int i = 1; i < this._List.Count; i++)
					{
						if (this._Comparer.Compare(x, this._List[i]) > 0)
						{
							x = this._List[i];
							result = i;
						}
					}
				}
			}
			return result;
		}

		public int IndexOfMax()
		{
			int result = -1;
			if (this._List.Count > 0)
			{
				result = this._List.Count - 1;
				object x = this._List[this._List.Count - 1];
				if (!this._IsSorted)
				{
					for (int i = this._List.Count - 2; i >= 0; i--)
					{
						if (this._Comparer.Compare(x, this._List[i]) < 0)
						{
							x = this._List[i];
							result = i;
						}
					}
				}
			}
			return result;
		}

		private bool ObjectIsCompliant(object O)
		{
			if (this._UseObjectsComparison && !(O is IComparable))
			{
				throw new ArgumentException("The SortableList is set to use the IComparable interface of objects, and the object to add does not implement the IComparable interface.");
			}
			return this._AddDuplicates || !this.Contains(O);
		}

		private void InitProperties(IComparer Comparer, int Capacity)
		{
			if (Comparer != null)
			{
				this._Comparer = Comparer;
				this._UseObjectsComparison = false;
			}
			else
			{
				this._Comparer = new SortableList.Comparison();
				this._UseObjectsComparison = true;
			}
			this._List = ((Capacity > 0) ? new ArrayList(Capacity) : new ArrayList());
			this._IsSorted = true;
			this._KeepSorted = true;
			this._AddDuplicates = true;
		}

		private ArrayList _List;
		private IComparer _Comparer;
		private bool _UseObjectsComparison;
		private bool _IsSorted;
		private bool _KeepSorted;
		private bool _AddDuplicates;
		public delegate bool Equality(object O1, object O2);

        private class Comparison : IComparer
		{
			public int Compare(object O1, object O2)
			{
				IComparable comparable = O1 as IComparable;
				return comparable.CompareTo(O2);
			}
		}
	}
}
