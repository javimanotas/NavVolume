using System;
using System.Collections.Generic;

namespace NavVolume.Runtime.Pathfinding
{
    /// <summary>
    /// A generic binary min-heap (priority queue).
    /// </summary>
    internal sealed class MinHeap<T>
    {
        struct Entry
        {
            public float Priority;
            public T Item;
        }

        readonly List<Entry> _heap;

        public MinHeap(int initialCapacity = 64) => _heap = new List<Entry>(initialCapacity);

        public int Count => _heap.Count;

        public bool IsEmpty => _heap.Count == 0;

        public void Push(T item, float priority)
        {
            _heap.Add(new() { Priority = priority, Item = item });
            BubbleUp(_heap.Count - 1);
        }

        public T Pop()
        {
            if (_heap.Count == 0)
            {
                throw new InvalidOperationException("Heap is empty");
            }

            var result = _heap[0].Item;
            var last = _heap.Count - 1;
            _heap[0] = _heap[last];
            _heap.RemoveAt(last);

            if (_heap.Count > 0)
            {
                SiftDown(0);
            }

            return result;
        }

        public void Clear()
        {
            _heap.Clear();
        }

        void BubbleUp(int i)
        {
            while (i > 0)
            {
                var parent = (i - 1) >> 1;

                if (_heap[parent].Priority <= _heap[i].Priority)
                {
                    break;
                }

                Swap(i, parent);
                i = parent;
            }
        }

        void SiftDown(int i)
        {
            while (true)
            {
                var left = (i << 1) + 1;
                var right = left + 1;
                var smallest = i;

                if (left < _heap.Count && _heap[left].Priority < _heap[smallest].Priority)
                {
                    smallest = left;
                }
                if (right < _heap.Count && _heap[right].Priority < _heap[smallest].Priority)
                {
                    smallest = right;
                }
                if (smallest == i)
                {
                    break;
                }

                Swap(i, smallest);
                i = smallest;
            }
        }

        void Swap(int a, int b)
        {
            (_heap[b], _heap[a]) = (_heap[a], _heap[b]);
        }
    }
}
