using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace Cajonic.Services
{
    public class ConcurrentSet<T> : ISet<T>
    {
        private readonly ConcurrentDictionary<T, byte> mDictionary = new();
    
        public IEnumerator<T> GetEnumerator()
        {
            return mDictionary.Keys.GetEnumerator();
        }
        
        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
        public bool Remove(T item)
        {
            return TryRemove(item);
        }
        
        public int Count => mDictionary.Count;
        
        public bool IsReadOnly => false;
        
        public bool IsEmpty => mDictionary.IsEmpty;

        public ICollection<T> Values => mDictionary.Keys;

        void ICollection<T>.Add(T item)
        {
            if (!Add(item))
            {
                throw new ArgumentException("Item already exists in set.");
            }
        }

        public void UnionWith(IEnumerable<T> other)
        {
            foreach (T item in other)
            {
                TryAdd(item);
            }
        }

        public void IntersectWith(IEnumerable<T> other)
        {
            IList<T> enumerable = other as IList<T> ?? other.ToArray();
            foreach (T item in this)
            {
                if (!enumerable.Contains(item))
                {
                    TryRemove(item);
                }
            }
        }

       public void ExceptWith(IEnumerable<T> other)
        {
            foreach (T item in other)
                TryRemove(item);
        }
       
        public void SymmetricExceptWith(IEnumerable<T> other)
        {
            throw new NotImplementedException();
        }
        public bool IsSubsetOf(IEnumerable<T> other)
        {
            IList<T> enumerable = other as IList<T> ?? other.ToArray();
            return this.AsParallel().All(enumerable.Contains);
        }
        public bool IsSupersetOf(IEnumerable<T> other)
        {
            return other.AsParallel().All(Contains);
        }
        public bool IsProperSupersetOf(IEnumerable<T> other)
        {
            IList<T> enumerable = other as IList<T> ?? other.ToArray();
            return Count != enumerable.Count && IsSupersetOf(enumerable);
        }
        public bool IsProperSubsetOf(IEnumerable<T> other)
        {
            IList<T> enumerable = other as IList<T> ?? other.ToArray();
            return Count != enumerable.Count && IsSubsetOf(enumerable);
        }
        public bool Overlaps(IEnumerable<T> other)
        {
            return other.AsParallel().Any(Contains);
        }
        public bool SetEquals(IEnumerable<T> other)
        {
            IList<T> enumerable = other as IList<T> ?? other.ToArray();
            return Count == enumerable.Count && enumerable.AsParallel().All(Contains);
        }
        public bool Add(T item)
        {
            return TryAdd(item);
        }

        public void Clear()
        {
            mDictionary.Clear();
        }

        public bool Contains(T item)
        {
            return mDictionary.ContainsKey(item);
        }
        public void CopyTo(T[] array, int arrayIndex)
        {
            Values.CopyTo(array, arrayIndex);
        }

        public T[] ToArray()
        {
            return mDictionary.Keys.ToArray();
        }

        public bool TryAdd(T item)
        {
            return mDictionary.TryAdd(item, default);
        }

        public bool TryRemove(T item)
        {
            return mDictionary.TryRemove(item, out byte _);
        }
    }
}