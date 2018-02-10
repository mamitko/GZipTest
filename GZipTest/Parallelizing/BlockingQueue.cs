using System;
using System.Collections.Generic;
using System.Diagnostics;
using GZipTest.Threading;

namespace GZipTest.Parallelizing
{
    public class BlockingQueue<T>
    {
        private readonly int boundedCapacity;
        private readonly MonitorSimple sync = new MonitorSimple();
        private readonly Queue<T> queue = new Queue<T>();

        public bool IsAddingCompleted { get; private set; }

        public BlockingQueue(int boundedCapacity = -1)
        {
            this.boundedCapacity = boundedCapacity;
        }

        public void CompleteAdding()
        {
            using (sync.GetLocked())
            {
                IsAddingCompleted = true;
                sync.PulseAll();
            }
        }

        public bool AddIfNotCompleted(T item)
        {
            if (IsAddingCompleted)
                return false;
            
            using (sync.GetLocked())
            {
                if (IsAddingCompleted)
                    return false;

                Add(item);
                return true;
            }
        }

        public IEnumerable<T> GetConsumingEnumerable()
        {
            T item;
            while (TakeOrTryWait(out item))
            {
                yield return item;
            }
        }

        private void Add(T item)
        {
            using (sync.GetLocked())
            {
                if (IsAddingCompleted)
                    throw new InvalidOperationException();

                while (boundedCapacity >= 0 && queue.Count >= boundedCapacity && !IsAddingCompleted)
                {
                    sync.Wait();
                }

                queue.Enqueue(item);
                sync.Pulse();
            }
        }

        /// <summary>
        /// Takes an item away from collection. If collection is empty, blocks control flow and waits until new item comes or CompleteAdding is invoked.
        /// </summary>
        /// <returns>Whether the item was taken or not</returns>
        private bool TakeOrTryWait(out T item)
        {
            using (sync.GetLocked())
            {
                while (queue.Count == 0 && !IsAddingCompleted)
                {
                    sync.Wait();
                }

                if (queue.Count <= 0)
                {
                    Debug.Assert(IsAddingCompleted);
                    item = default(T);
                    return false;
                }

                item = queue.Dequeue();
                sync.PulseAll();
                return true;
            }
        }
    }
}
