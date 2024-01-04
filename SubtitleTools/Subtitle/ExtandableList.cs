using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace S16.Collections
{
    /// <summary>
    /// Implements a variable-size List that uses an array of objects to store the
    /// elements. A List has a capacity, which is the allocated length
    /// of the internal array. As elements are added to a List, the capacity
    /// of the List is automatically increased as required by reallocating the
    /// internal array.
    /// </summary>
    [DebuggerDisplay("Count = {Count}")]
    [Serializable]
    public class ExtandableList<T> : IList<T>, IList, IReadOnlyList<T>
    {
        #region Variables
        protected const uint MaxArrayLength = 0x7FEFFFFF;
        protected const int DefaultCapacity = 4;

        protected T[] _items; // Do not rename (binary serialization)
        protected int _size; // Do not rename (binary serialization)
        protected int _version; // Do not rename (binary serialization)

#pragma warning disable CA1825 // avoid the extra generic instantiation for Array.Empty<T>()
        protected static readonly T[] s_emptyArray = new T[0];
#pragma warning restore CA1825
        #endregion

        #region Constructors
        /// <summary>
        /// Constructs a List. The list is initially empty and has a capacity
        /// of zero. Upon adding the first element to the list the capacity is
        /// increased to DefaultCapacity, and then increased in multiples of two
        /// as required.
        /// </summary>
        public ExtandableList()
        {
            _items = s_emptyArray;
        }

        /// <summary>
        /// Constructs a List with a given initial capacity. The list is
        /// initially empty, but will have room for the given number of elements
        /// before any reallocations are required.
        /// </summary>
        public ExtandableList(int capacity)
        {
            if (capacity < 0)
                throw new ArgumentOutOfRangeException("capacity");

            if (capacity == 0)
                _items = s_emptyArray;
            else
                _items = new T[capacity];
        }

        /// <summary>
        /// Constructs a List, copying the contents of the given collection. The
        /// size and capacity of the new list will both be equal to the size of the
        /// given collection.
        /// </summary>
        public ExtandableList(IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");

            if (collection is ICollection<T> c)
            {
                int count = c.Count;
                if (count == 0)
                {
                    _items = s_emptyArray;
                }
                else
                {
                    _items = new T[count];
                    c.CopyTo(_items, 0);
                    _size = count;
                }
            }
            else
            {
                _size = 0;
                _items = s_emptyArray;
                using (IEnumerator<T> en = collection.GetEnumerator())
                {
                    while (en.MoveNext())
                    {
                        Add(en.Current);
                    }
                }
            }
        }
        #endregion

        #region Properties
        /// <summary>
        /// Sets or Gets the element at the given index.
        /// </summary>
        public T this[int index]
        {
            get
            {
                // Following trick can reduce the range check by one
                if ((uint)index >= (uint)_size)
                    throw new ArgumentOutOfRangeException();
                return _items[index];
            }

            set
            {
                if ((uint)index >= (uint)_size)
                    throw new ArgumentOutOfRangeException();
                _items[index] = value;
                _version++;
            }
        }

        /// <summary>
        /// Gets and sets the capacity of this list.  The capacity is the size of
        /// the internal array used to hold items.  When set, the internal
        /// array of the list is reallocated to the given capacity.
        /// </summary>
        public int Capacity
        {
            get => _items.Length;
            set
            {
                if (value < _size)
                    throw new ArgumentOutOfRangeException("value");

                if (value != _items.Length)
                {
                    if (value > 0)
                    {
                        T[] newItems = new T[value];
                        if (_size > 0)
                            Array.Copy(_items, newItems, _size);
                        _items = newItems;
                    }
                    else
                        _items = s_emptyArray;
                }
            }
        }

        /// <summary>
        /// Read-only property describing how many elements are in the List. 
        /// </summary>
        public int Count => _size;
        #endregion

        #region Implemented Properties
        object IList.this[int index]
        {
            get => this[index];
            set
            {
                VerifyValueType(value);

                try
                {
                    this[index] = (T)value;
                }
                catch (InvalidCastException)
                {
                    throw new ArgumentException("value");
                }
            }
        }

        bool IList.IsFixedSize => false;

        /// <summary>
        /// Is this List read-only? 
        /// </summary>
        bool ICollection<T>.IsReadOnly => false;

        bool IList.IsReadOnly => false;

        /// <summary>
        /// Is this List synchronized (thread-safe)?
        /// </summary>
        bool ICollection.IsSynchronized => false;

        /// <summary>
        /// Synchronization root for this object.
        /// </summary>
        object ICollection.SyncRoot => this;
        #endregion

        #region Static Methods
        protected static bool IsReferenceOrContainsReferences<TRef>()
        {
#if NET
            return RuntimeHelpers.IsReferenceOrContainsReferences<TRef>();
#else
            try
            {
                var method = typeof(RuntimeHelpers).GetMethod("IsReferenceOrContainsReferences", BindingFlags.Static | BindingFlags.Public);
                if (method != null)
                {
                    var generic = method.MakeGenericMethod(typeof(TRef));
                    return generic.Invoke(null, null) == (object)true;
                }
            }
            catch (Exception e)
            {
                Debug.WriteLine(e.Message);
            }

            var t = typeof(TRef);
            return t.IsValueType || t.IsClass || t.IsAnsiClass || t.IsPointer;
#endif
        }

        protected static bool IsCompatibleObject(object value)
        {
            // Non-null values are fine.  Only accept nulls if T is a class or Nullable<U>.
            // Note that default(T) is not equal to null for value types except when T is Nullable<U>.
            return (value is T) || (value == null && default(T) == null);
        }

        protected static void VerifyValueType(object value)
        {
            if (!IsCompatibleObject(value))
            {
                throw new ArgumentException(value.ToString());
            }
        }
        #endregion

        #region Protected Methods
        /// <summary>
        /// Ensures that the capacity of this list is at least the given minimum
        /// value. If the current capacity of the list is less than min, the
        /// capacity is increased to twice the current capacity or to min,
        /// whichever is larger.
        /// </summary>
        protected virtual void EnsureCapacity(int min)
        {
            if (_items.Length < min)
            {
                int newCapacity = _items.Length == 0 ? DefaultCapacity : _items.Length * 2;
                // Allow the list to grow to maximum possible capacity (~2G elements) before encountering overflow.
                // Note that this check works even when _items.Length overflowed thanks to the (uint) cast
                if ((uint)newCapacity > MaxArrayLength) newCapacity = (int)MaxArrayLength;
                if (newCapacity < min) newCapacity = min;
                Capacity = newCapacity;
            }
        }

        /// <summary>
        /// Non-inline from List.Add to improve its code quality as uncommon path
        /// </summary>
        protected virtual void AddWithResize(T item)
        {
            int size = _size;
            EnsureCapacity(size + 1);
            _size = size + 1;
            _items[size] = item;
        }
        #endregion

        #region Public Methods
        /// <summary>
        /// Adds the given object to the end of this list. The size of the list is
        /// increased by one. If required, the capacity of the list is doubled
        /// before adding the new element.
        /// </summary>
        public virtual void Add(T item)
        {
            _version++;
            T[] array = _items;
            int size = _size;
            if ((uint)size < (uint)array.Length)
            {
                _size = size + 1;
                array[size] = item;
            }
            else
            {
                AddWithResize(item);
            }
        }

        /// <summary>
        /// Adds the elements of the given collection to the end of this list. If
        /// required, the capacity of the list is increased to twice the previous
        /// capacity or the new size, whichever is larger.
        /// </summary>
        public virtual void AddRange(IEnumerable<T> collection)
            => InsertRange(_size, collection);

        public virtual ReadOnlyCollection<T> AsReadOnly()
            => new ReadOnlyCollection<T>(this);

        /// <summary>
        /// Searches a section of the list for a given element using a binary search
        /// algorithm. Elements of the list are compared to the search value using
        /// the given IComparer interface. If comparer is null, elements of
        /// the list are compared to the search value using the IComparable
        /// interface, which in that case must be implemented by all elements of the
        /// list and the given search value. This method assumes that the given
        /// section of the list is already sorted; if this is not the case, the
        /// result will be incorrect.
        ///
        /// The method returns the index of the given value in the list. If the
        /// list does not contain the given value, the method returns a negative
        /// integer. The bitwise complement operator (~) can be applied to a
        /// negative result to produce the index of the first element (if any) that
        /// is larger than the given search value. This is also the index at which
        /// the search value should be inserted into the list in order for the list
        /// to remain sorted.
        ///
        /// The method uses the Array.BinarySearch method to perform the
        /// search.
        /// </summary>
        public virtual int BinarySearch(int index, int count, T item, IComparer<T> comparer)
        {
            if (index < 0)
                throw new ArgumentOutOfRangeException("index");
            if (count < 0)
                throw new ArgumentOutOfRangeException("count");
            if (_size - index < count)
                throw new ArgumentException();

            return Array.BinarySearch<T>(_items, index, count, item, comparer);
        }

        public virtual int BinarySearch(T item)
            => BinarySearch(0, Count, item, null);

        public virtual int BinarySearch(T item, IComparer<T> comparer)
            => BinarySearch(0, Count, item, comparer);

        /// <summary>
        /// Clears the contents of List.
        /// </summary>
        public virtual void Clear()
        {
            _version++;
            if (IsReferenceOrContainsReferences<T>())
            {
                int size = _size;
                _size = 0;
                if (size > 0)
                {
                    Array.Clear(_items, 0, size); // Clear the elements so that the gc can reclaim the references.
                }
            }
            else
            {
                _size = 0;
            }
        }

        /// <summary>
        /// Contains returns true if the specified element is in the List.
        /// It does a linear, O(n) search.  Equality is determined by calling
        /// EqualityComparer<T>.Default.Equals().
        /// </summary>
        public virtual bool Contains(T item)
        {
            // PERF: IndexOf calls Array.IndexOf, which internally
            // calls EqualityComparer<T>.Default.IndexOf, which
            // is specialized for different types. This
            // boosts performance since instead of making a
            // virtual method call each iteration of the loop,
            // via EqualityComparer<T>.Default.Equals, we
            // only make one virtual call to EqualityComparer.IndexOf.

            return _size != 0 && IndexOf(item) != -1;
        }

        public virtual ExtandableList<TOutput> ConvertAll<TOutput>(Converter<T, TOutput> converter)
        {
            if (converter == null)
                throw new ArgumentNullException("converter");

            ExtandableList<TOutput> list = new ExtandableList<TOutput>(_size);
            for (int i = 0; i < _size; i++)
            {
                list._items[i] = converter(_items[i]);
            }
            list._size = _size;
            return list;
        }

        public virtual void CopyTo(T[] array)
            => CopyTo(array, 0);

        /// <summary>
        /// Copies a section of this list to the given array at the given index.
        ///
        /// The method uses the Array.Copy method to copy the elements.
        /// </summary>
        public virtual void CopyTo(int index, T[] array, int arrayIndex, int count)
        {
            if (_size - index < count)
            {
                throw new ArgumentException();
            }

            // Delegate rest of error checking to Array.Copy.
            Array.Copy(_items, index, array, arrayIndex, count);
        }

        public virtual void CopyTo(T[] array, int arrayIndex)
        {
            // Delegate rest of error checking to Array.Copy.
            Array.Copy(_items, 0, array, arrayIndex, _size);
        }

        public virtual bool Exists(Predicate<T> match)
            => FindIndex(match) != -1;

        public virtual T Find(Predicate<T> match)
        {
            if (match == null)
                throw new ArgumentNullException("match");

            for (int i = 0; i < _size; i++)
            {
                if (match(_items[i]))
                {
                    return _items[i];
                }
            }
            return default(T);
        }

        public virtual ExtandableList<T> FindAll(Predicate<T> match)
        {
            if (match == null)
                throw new ArgumentNullException("match");

            ExtandableList<T> list = new ExtandableList<T>();
            for (int i = 0; i < _size; i++)
            {
                if (match(_items[i]))
                {
                    list.Add(_items[i]);
                }
            }
            return list;
        }

        public virtual int FindIndex(Predicate<T> match)
            => FindIndex(0, _size, match);

        public virtual int FindIndex(int startIndex, Predicate<T> match)
            => FindIndex(startIndex, _size - startIndex, match);

        public virtual int FindIndex(int startIndex, int count, Predicate<T> match)
        {
            if ((uint)startIndex > (uint)_size)
                throw new IndexOutOfRangeException("startIndex");

            if (count < 0 || startIndex > _size - count)
                throw new IndexOutOfRangeException("count");

            if (match == null)
                throw new ArgumentNullException("match");

            int endIndex = startIndex + count;
            for (int i = startIndex; i < endIndex; i++)
            {
                if (match(_items[i])) return i;
            }
            return -1;
        }

        public virtual T FindLast(Predicate<T> match)
        {
            if (match == null)
                throw new ArgumentNullException("match");

            for (int i = _size - 1; i >= 0; i--)
            {
                if (match(_items[i]))
                {
                    return _items[i];
                }
            }
            return default(T);
        }

        public virtual int FindLastIndex(Predicate<T> match)
            => FindLastIndex(_size - 1, _size, match);

        public virtual int FindLastIndex(int startIndex, Predicate<T> match)
            => FindLastIndex(startIndex, startIndex + 1, match);

        public virtual int FindLastIndex(int startIndex, int count, Predicate<T> match)
        {
            if (match == null)
                throw new ArgumentNullException("match");

            if (_size == 0)
            {
                // Special case for 0 length List
                if (startIndex != -1)
                    throw new IndexOutOfRangeException("startIndex");
            }
            else
            {
                // Make sure we're not out of range
                if ((uint)startIndex >= (uint)_size)
                    throw new IndexOutOfRangeException("startIndex");
            }

            // 2nd have of this also catches when startIndex == MAXINT, so MAXINT - 0 + 1 == -1, which is < 0.
            if (count < 0 || startIndex - count + 1 < 0)
                throw new IndexOutOfRangeException("count");

            int endIndex = startIndex - count;
            for (int i = startIndex; i > endIndex; i--)
            {
                if (match(_items[i]))
                {
                    return i;
                }
            }
            return -1;
        }

        public virtual void ForEach(Action<T> action)
        {
            if (action == null)
                throw new ArgumentNullException("action");

            int version = _version;

            for (int i = 0; i < _size; i++)
            {
                if (version != _version) break;
                action(_items[i]);
            }

            if (version != _version)
                throw new InvalidOperationException();
        }

        /// <summary>
        /// Returns an enumerator for this list with the given
        /// permission for removal of elements. If modifications made to the list
        /// while an enumeration is in progress, the MoveNext and
        /// GetObject methods of the enumerator will throw an exception.
        /// </summary>
        public virtual Enumerator GetEnumerator()
            => new Enumerator(this);

        public virtual ExtandableList<T> GetRange(int index, int count)
        {
            if (index < 0)
                throw new IndexOutOfRangeException("index");

            if (count < 0)
                throw new ArgumentOutOfRangeException("count");
        
            if (_size - index < count)
                throw new ArgumentException();

            ExtandableList<T> list = new ExtandableList<T>(count);
            Array.Copy(_items, index, list._items, 0, count);
            list._size = count;
            return list;
        }

        /// <summary>
        /// Returns the index of the first occurrence of a given value in a range of
        /// this list. The list is searched forwards from beginning to end.
        /// The elements of the list are compared to the given value using the
        /// Object.Equals method.
        ///
        /// This method uses the Array.IndexOf method to perform the
        /// search.
        /// </summary>
        public virtual int IndexOf(T item)
            => Array.IndexOf(_items, item, 0, _size);

        public virtual int IndexOf(T item, int index)
        {
            if (index > _size)
                throw new ArgumentOutOfRangeException("index");
            return Array.IndexOf(_items, item, index, _size - index);
        }

        /// <summary>
        /// Returns the index of the first occurrence of a given value in a range of
        /// this list. The list is searched forwards, starting at index
        /// index and upto count number of elements. The
        /// elements of the list are compared to the given value using the
        /// Object.Equals method.
        ///
        /// This method uses the Array.IndexOf method to perform the
        /// search.
        /// </summary>
        public virtual int IndexOf(T item, int index, int count)
        {
            if (index > _size)
                throw new ArgumentOutOfRangeException("index");

            if (count < 0 || index > _size - count)
                throw new ArgumentOutOfRangeException("count");

            return Array.IndexOf(_items, item, index, count);
        }

        /// <summary>
        /// Inserts an element into this list at a given index. The size of the list
        /// is increased by one. If required, the capacity of the list is doubled
        /// before inserting the new element.
        /// </summary>
        public virtual void Insert(int index, T item)
        {
            // Note that insertions at the end are legal.
            if ((uint)index > (uint)_size)
            {
                throw new ArgumentOutOfRangeException("index");
            }

            if (_size == _items.Length) EnsureCapacity(_size + 1);
            if (index < _size)
            {
                Array.Copy(_items, index, _items, index + 1, _size - index);
            }
            _items[index] = item;
            _size++;
            _version++;
        }

        /// <summary>
        /// Inserts the elements of the given collection at a given index. If
        /// required, the capacity of the list is increased to twice the previous
        /// capacity or the new size, whichever is larger.  Ranges may be added
        /// to the end of the list by setting index to the List's size.
        /// </summary>
        public virtual void InsertRange(int index, IEnumerable<T> collection)
        {
            if (collection == null)
                throw new ArgumentNullException("collection");

            if ((uint)index > (uint)_size)
                throw new ArgumentOutOfRangeException("index");

            if (collection is ICollection<T> c)
            {
                int count = c.Count;
                if (count > 0)
                {
                    EnsureCapacity(_size + count);
                    if (index < _size)
                    {
                        Array.Copy(_items, index, _items, index + count, _size - index);
                    }

                    // If we're inserting a List into itself, we want to be able to deal with that.
                    if (this == c)
                    {
                        // Copy first part of _items to insert location
                        Array.Copy(_items, 0, _items, index, index);
                        // Copy last part of _items back to inserted location
                        Array.Copy(_items, index + count, _items, index * 2, _size - index);
                    }
                    else
                    {
                        c.CopyTo(_items, index);
                    }
                    _size += count;
                }
            }
            else
            {
                using (IEnumerator<T> en = collection.GetEnumerator())
                {
                    while (en.MoveNext())
                    {
                        Insert(index++, en.Current);
                    }
                }
            }
            _version++;
        }

        /// <summary>
        /// Returns the index of the last occurrence of a given value in a range of
        /// this list. The list is searched backwards, starting at the end
        /// and ending at the first element in the list. The elements of the list
        /// are compared to the given value using the Object.Equals method.
        ///
        /// This method uses the Array.LastIndexOf method to perform the
        /// search.
        /// </summary>
        public virtual int LastIndexOf(T item)
        {
            if (_size == 0)
            {  // Special case for empty list
                return -1;
            }
            else
            {
                return LastIndexOf(item, _size - 1, _size);
            }
        }

        /// <summary>
        /// Returns the index of the last occurrence of a given value in a range of
        /// this list. The list is searched backwards, starting at index
        /// index and ending at the first element in the list. The
        /// elements of the list are compared to the given value using the
        /// Object.Equals method.
        ///
        /// This method uses the Array.LastIndexOf method to perform the
        /// search.
        /// </summary>
        public virtual int LastIndexOf(T item, int index)
        {
            if (index >= _size)
                throw new ArgumentOutOfRangeException("index");
            return LastIndexOf(item, index, index + 1);
        }

        /// <summary>
        /// Returns the index of the last occurrence of a given value in a range of
        /// this list. The list is searched backwards, starting at index
        /// index and upto count elements. The elements of
        /// the list are compared to the given value using the Object.Equals
        /// method.
        ///
        /// This method uses the Array.LastIndexOf method to perform the
        /// search.
        /// </summary>
        public virtual int LastIndexOf(T item, int index, int count)
        {
            if ((Count != 0) && (index < 0))
                throw new IndexOutOfRangeException("index");

            if ((Count != 0) && (count < 0))
                throw new ArgumentOutOfRangeException("count");

            if (_size == 0)
            {  // Special case for empty list
                return -1;
            }

            if (index >= _size)
                throw new ArgumentOutOfRangeException("index");

            if (count > index + 1)
                throw new ArgumentOutOfRangeException("count");

            return Array.LastIndexOf(_items, item, index, count);
        }

        /// <summary>
        /// Removes the element at the given index. The size of the list is
        /// decreased by one.
        /// </summary>
        public virtual bool Remove(T item)
        {
            int index = IndexOf(item);
            if (index >= 0)
            {
                RemoveAt(index);
                return true;
            }

            return false;
        }

        /// <summary>
        /// This method removes all items which matches the predicate.
        /// The complexity is O(n).
        /// </summary>
        public virtual int RemoveAll(Predicate<T> match)
        {
            if (match == null)
                throw new ArgumentNullException("match");

            int freeIndex = 0;   // the first free slot in items array

            // Find the first item which needs to be removed.
            while (freeIndex < _size && !match(_items[freeIndex])) freeIndex++;
            if (freeIndex >= _size) return 0;

            int current = freeIndex + 1;
            while (current < _size)
            {
                // Find the first item which needs to be kept.
                while (current < _size && match(_items[current])) current++;

                if (current < _size)
                {
                    // copy item to the free slot.
                    _items[freeIndex++] = _items[current++];
                }
            }

            if (IsReferenceOrContainsReferences<T>())
            {
                Array.Clear(_items, freeIndex, _size - freeIndex); // Clear the elements so that the gc can reclaim the references.
            }

            int result = _size - freeIndex;
            _size = freeIndex;
            _version++;
            return result;
        }

        /// <summary>
        /// Removes the element at the given index. The size of the list is
        /// decreased by one.
        /// </summary>
        public virtual void RemoveAt(int index)
        {
            if ((uint)index >= (uint)_size)
            {
                throw new ArgumentOutOfRangeException("index");
            }
            _size--;
            if (index < _size)
            {
                Array.Copy(_items, index + 1, _items, index, _size - index);
            }
            if (IsReferenceOrContainsReferences<T>())
            {
                _items[_size] = default(T);
            }
            _version++;
        }

        /// <summary>
        /// Removes a range of elements from this list.
        /// </summary>
        public virtual void RemoveRange(int index, int count)
        {
            if (index < 0)
                throw new IndexOutOfRangeException("index");

            if (count < 0)
                throw new ArgumentOutOfRangeException("count");

            if (_size - index < count)
                throw new ArgumentException();

            if (count > 0)
            {
                _size -= count;
                if (index < _size)
                {
                    Array.Copy(_items, index + count, _items, index, _size - index);
                }

                _version++;
                if (IsReferenceOrContainsReferences<T>())
                {
                    Array.Clear(_items, _size, count);
                }
            }
        }

        /// <summary>
        /// Reverses the elements in this list.
        /// </summary>
        public virtual void Reverse()
            => Reverse(0, Count);

        /// <summary>
        /// Reverses the elements in a range of this list. Following a call to this
        /// method, an element in the range given by index and count
        /// which was previously located at index i will now be located at
        /// index index + (index + count - i - 1).
        /// </summary>
        public virtual void Reverse(int index, int count)
        {
            if (index < 0)
                throw new IndexOutOfRangeException("index");

            if (count < 0)
                throw new ArgumentOutOfRangeException("count");

            if (_size - index < count)
                throw new ArgumentException();

            if (count > 1)
            {
                Array.Reverse(_items, index, count);
            }
            _version++;
        }

        /// <summary>
        /// Sorts the elements in this list.  Uses the default comparer and
        /// Array.Sort.
        /// </summary>
        public virtual void Sort()
            => Sort(0, Count, null);

        /// <summary>
        /// Sorts the elements in this list.  Uses Array.Sort with the
        /// provided comparer.
        /// </summary>
        public virtual void Sort(IComparer<T> comparer)
            => Sort(0, Count, comparer);

        /// <summary>
        /// Sorts the elements in a section of this list. The sort compares the
        /// elements to each other using the given IComparer interface. If
        /// comparer is null, the elements are compared to each other using
        /// the IComparable interface, which in that case must be implemented by all
        /// elements of the list.
        ///
        /// This method uses the Array.Sort method to sort the elements.
        /// </summary>
        public virtual void Sort(int index, int count, IComparer<T> comparer)
        {
            if (index < 0)
                throw new IndexOutOfRangeException("index");

            if (count < 0)
                throw new ArgumentOutOfRangeException("count");

            if (_size - index < count)
                throw new ArgumentException();

            if (count > 1)
            {
                Array.Sort<T>(_items, index, count, comparer);
            }
            _version++;
        }

        public virtual void Sort(Comparison<T> comparison)
        {
            if (comparison == null)
                throw new ArgumentNullException("comparison");

            if (_size > 1)
            {
                IComparer<T> comparer = new FunctorComparer<T>(comparison);
                Array.Sort<T>(_items, 0, _size, comparer);
            }
            _version++;
        }

        /// <summary>
        /// ToArray returns an array containing the contents of the List.
        /// This requires copying the List, which is an O(n) operation.
        /// </summary>
        public virtual T[] ToArray()
        {
            if (_size == 0)
                return s_emptyArray;

            T[] array = new T[_size];
            Array.Copy(_items, array, _size);
            return array;
        }

        /// <summary>
        /// Sets the capacity of this list to the size of the list. This method can
        /// be used to minimize a list's memory overhead once it is known that no
        /// new elements will be added to the list. To completely clear a list and
        /// release all memory referenced by the list, execute the following
        /// statements:
        ///
        /// list.Clear();
        /// list.TrimExcess();
        /// </summary>
        public virtual void TrimExcess()
        {
            int threshold = (int)(((double)_items.Length) * 0.9);
            if (_size < threshold)
            {
                Capacity = _size;
            }
        }

        public virtual bool TrueForAll(Predicate<T> match)
        {
            if (match == null)
                throw new ArgumentNullException("match");

            for (int i = 0; i < _size; i++)
            {
                if (!match(_items[i]))
                    return false;
            }
            return true;
        }
        #endregion

        #region Implemented Methods
        int IList.Add(object item)
        {
            VerifyValueType(item);

            try
            {
                Add((T)item);
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException("item");
            }

            return Count - 1;
        }

        bool IList.Contains(object item)
        {
            if (IsCompatibleObject(item))
            {
                return Contains((T)item);
            }
            return false;
        }

        /// <summary>
        /// Copies this List into array, which must be of a
        /// compatible array type.
        /// </summary>
        void ICollection.CopyTo(Array array, int arrayIndex)
        {
            if ((array != null) && (array.Rank != 1))
            {
                throw new ArgumentException();
            }

            try
            {
                // Array.Copy will check for NULL.
                Array.Copy(_items, 0, array, arrayIndex, _size);
            }
            catch (ArrayTypeMismatchException)
            {
                throw new ArgumentException();
            }
        }

        IEnumerator<T> IEnumerable<T>.GetEnumerator()
            => new Enumerator(this);

        IEnumerator IEnumerable.GetEnumerator()
            => new Enumerator(this);

        int IList.IndexOf(object item)
        {
            if (IsCompatibleObject(item))
            {
                return IndexOf((T)item);
            }
            return -1;
        }

        void IList.Insert(int index, object item)
        {
            VerifyValueType(item);

            try
            {
                Insert(index, (T)item);
            }
            catch (InvalidCastException)
            {
                throw new ArgumentException("item");
            }
        }

        void IList.Remove(object item)
        {
            if (IsCompatibleObject(item))
            {
                Remove((T)item);
            }
        }
        #endregion

        #region Nested Types
        public struct Enumerator : IEnumerator<T>, IEnumerator
        {
            #region Variables
            private readonly ExtandableList<T> _list;
            private int _index;
            private readonly int _version;
            private T _current;
            #endregion

            #region Constructor
            public Enumerator(ExtandableList<T> list)
            {
                _list = list;
                _index = 0;
                _version = list._version;
                _current = default;
            }
            #endregion

            #region Properties
            public T Current => _current;

            object IEnumerator.Current
            {
                get
                {
                    if (_index == 0 || _index == _list._size + 1)
                        throw new InvalidOperationException();
                    return Current;
                }
            }
            #endregion

            #region Methods
            public void Dispose()
            {
            }

            public bool MoveNext()
            {
                ExtandableList<T> localList = _list;

                if (_version == localList._version && ((uint)_index < (uint)localList._size))
                {
                    _current = localList._items[_index];
                    _index++;
                    return true;
                }
                return MoveNextRare();
            }

            private bool MoveNextRare()
            {
                if (_version != _list._version)
                    throw new InvalidOperationException();

                _index = _list._size + 1;
                _current = default;
                return false;
            }

            void IEnumerator.Reset()
            {
                if (_version != _list._version)
                    throw new InvalidOperationException();

                _index = 0;
                _current = default;
            }
            #endregion
        }

        public class FunctorComparer<Tfc> : IComparer<Tfc>
        {
            #region Variables
            private Comparer<Tfc> c;
            private Comparison<Tfc> comparison;
            #endregion

            #region Constructor
            public FunctorComparer(Comparison<Tfc> comparison)
            {
                c = Comparer<Tfc>.Default;
                this.comparison = comparison;
            }
            #endregion

            #region Methods
            public int Compare(Tfc x, Tfc y)
            {
                return comparison(x, y);
            }
            #endregion
        }
        #endregion
    }
}
