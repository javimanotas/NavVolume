using System;

namespace NavVolume.Runtime.Pathfinding
{
    /// <summary>
    /// A generic binary min-heap (priority queue).
    /// </summary>
    /// <remarks>
    /// Backed by a raw array with hole-based bubble-up/sift-down: instead of swapping pairs on every
    /// level, the moving entry is held aside and existing entries are shifted into the hole, halving
    /// the writes per operation. This is the pathfinder's open list, so it is on the hottest path.
    /// </remarks>
    internal sealed class MinHeap<T>
    {
        struct Entry
        {
            public float Priority;
            public T Item;
        }

        Entry[] _heap;
        int _count;

        public MinHeap(int initialCapacity = 64) =>
            _heap = new Entry[initialCapacity < 1 ? 1 : initialCapacity];

        public int Count => _count;

        public bool IsEmpty => _count == 0;

        public void Push(T item, float priority)
        {
            if (_count == _heap.Length)
            {
                Array.Resize(ref _heap, _heap.Length << 1);
            }

            // Bubble the hole up, shifting larger parents down, then drop the entry in once.
            var i = _count++;
            while (i > 0)
            {
                var parent = (i - 1) >> 1;
                if (_heap[parent].Priority <= priority)
                {
                    break;
                }

                _heap[i] = _heap[parent];
                i = parent;
            }

            _heap[i].Priority = priority;
            _heap[i].Item = item;
        }

        public T Pop()
        {
            if (_count == 0)
            {
                throw new InvalidOperationException("Heap is empty");
            }

            var result = _heap[0].Item;
            var last = --_count;

            if (last > 0)
            {
                // Sift the last entry down from the root through a hole, then drop it in once.
                var movedPriority = _heap[last].Priority;
                var movedItem = _heap[last].Item;
                var half = last >> 1; // entries at index >= half are leaves

                var i = 0;
                while (i < half)
                {
                    var child = (i << 1) + 1;
                    var right = child + 1;

                    if (right < last && _heap[right].Priority < _heap[child].Priority)
                    {
                        child = right;
                    }

                    if (_heap[child].Priority >= movedPriority)
                    {
                        break;
                    }

                    _heap[i] = _heap[child];
                    i = child;
                }

                _heap[i].Priority = movedPriority;
                _heap[i].Item = movedItem;
            }

            _heap[last] = default; // release any reference held in the vacated slot
            return result;
        }

        public void Clear()
        {
            Array.Clear(_heap, 0, _count);
            _count = 0;
        }
    }
}
