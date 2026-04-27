using NavVolume.Runtime.Pathfinding;
using NUnit.Framework;

namespace NavVolume.Tests.Pathfinding
{
    public class MinHeapTests
    {
        [Test]
        public void Count_OnNewHeap_IsZero()
        {
            var heap = new MinHeap<int>();

            Assert.AreEqual(0, heap.Count);
        }

        [Test]
        public void IsEmpty_OnNewHeap_ReturnsTrue()
        {
            var heap = new MinHeap<int>();

            Assert.IsTrue(heap.IsEmpty);
        }

        [Test]
        public void IsEmpty_AfterPush_ReturnsFalse()
        {
            var heap = new MinHeap<int>();
            heap.Push(42, 1.0f);

            Assert.IsFalse(heap.IsEmpty);
        }

        [Test]
        public void Count_AfterPushes_ReflectsNumberOfItems([Values(1, 5, 10)] int numItems)
        {
            var heap = new MinHeap<int>();

            for (var i = 0; i < numItems; i++)
            {
                heap.Push(i, i);
            }

            Assert.AreEqual(numItems, heap.Count);
        }

        [Test]
        public void Count_AfterPushAndPop_DecrementsByOne()
        {
            var heap = new MinHeap<int>();
            heap.Push(1, 1.0f);
            heap.Push(2, 2.0f);

            var countBefore = heap.Count;
            heap.Pop();

            Assert.AreEqual(countBefore - 1, heap.Count);
        }

        [Test]
        public void Pop_OnSingleItem_ReturnsItAndLeavesHeapEmpty()
        {
            var heap = new MinHeap<int>();
            heap.Push(99, 5.0f);

            var item = heap.Pop();

            Assert.AreEqual(99, item);
            Assert.IsTrue(heap.IsEmpty);
        }

        [Test]
        public void Pop_ReturnsItemWithLowestPriority()
        {
            var heap = new MinHeap<int>();
            heap.Push(100, 10.0f);
            heap.Push(1, 1.0f);
            heap.Push(50, 5.0f);

            Assert.AreEqual(1, heap.Pop());
        }

        [Test]
        public void Pop_RepeatedCalls_ReturnItemsInAscendingPriorityOrder()
        {
            var heap = new MinHeap<int>();
            heap.Push(30, 3.0f);
            heap.Push(10, 1.0f);
            heap.Push(20, 2.0f);
            heap.Push(50, 5.0f);
            heap.Push(40, 4.0f);

            var prev = float.NegativeInfinity;

            while (!heap.IsEmpty)
            {
                var item = heap.Pop();

                Assert.GreaterOrEqual(item, prev);
                prev = item;
            }
        }

        [Test]
        public void Pop_WithDuplicatePriorities_ReturnsAllItems()
        {
            var heap = new MinHeap<int>();
            heap.Push(1, 1.0f);
            heap.Push(2, 1.0f);
            heap.Push(3, 1.0f);

            Assert.AreEqual(3, heap.Count);
            heap.Pop();
            heap.Pop();
            heap.Pop();
            Assert.IsTrue(heap.IsEmpty);
        }

        [Test]
        public void Push_BeyondInitialCapacity_DoesNotThrow()
        {
            var heap = new MinHeap<int>(2);

            Assert.DoesNotThrow(() =>
            {
                for (var i = 0; i < 32; i++)
                {
                    heap.Push(i, i);
                }
            });

            Assert.AreEqual(32, heap.Count);
        }

        [Test]
        public void Push_BeyondInitialCapacity_StillMaintainsMinOrder()
        {
            var heap = new MinHeap<int>(2);

            for (var i = 20; i >= 0; i--)
            {
                heap.Push(i, i);
            }

            Assert.AreEqual(0, heap.Pop());
        }

        [Test]
        public void Clear_AfterPushes_LeavesHeapEmpty()
        {
            var heap = new MinHeap<int>();
            heap.Push(1, 1.0f);
            heap.Push(2, 2.0f);
            heap.Push(3, 3.0f);

            heap.Clear();

            Assert.IsTrue(heap.IsEmpty);
            Assert.AreEqual(0, heap.Count);
        }

        [Test]
        public void Push_AfterClear_WorksCorrectly()
        {
            var heap = new MinHeap<int>();
            heap.Push(10, 10.0f);
            heap.Push(20, 20.0f);
            heap.Clear();

            heap.Push(5, 5.0f);

            Assert.AreEqual(1, heap.Count);
            Assert.AreEqual(5, heap.Pop());
        }
    }
}
