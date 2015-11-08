using System;
using System.Collections.Generic;
using GZipTest.Threading;

namespace GZipTest.Parallelizing
{
    public class BlockingQueue<T>
    {
        private readonly int _boundedCapacity;
        private readonly MonitorSimple _sync = new MonitorSimple();
        private readonly Queue<T> _queue = new Queue<T>();

        public bool IsAddingCompleted { get; private set; }

        public BlockingQueue(int boundedCapacity = -1)
        {
            _boundedCapacity = boundedCapacity;
        }

        public void CompleteAdding()
        {
            using (_sync.GetLocked())
            {
                IsAddingCompleted = true;
                _sync.PulseAll();
            }
        }

        public void Add(T item)
        {
            using(_sync.GetLocked())
            {
                if (IsAddingCompleted)
                    throw new InvalidOperationException();

                while (_boundedCapacity >= 0 && _queue.Count >= _boundedCapacity && !IsAddingCompleted)
                    _sync.Wait();
         
                _queue.Enqueue(item);
                _sync.Pulse();
            }
        }

        public bool AddIfNotCompleted(T item)
        {
            if (IsAddingCompleted)
                return false;
            
            using (_sync.GetLocked())
            {
                if (IsAddingCompleted)
                    return false;

                Add(item);
                return true;
            }
        }

        /// <summary>
        /// Takes an item away from collection. If collection is empty, blocks control flow and waits until new item come or CompleteAdding is invoked.
        /// </summary>
        /// <returns>Whether an item is taken or not.</returns>
        public bool TakeOrTryWait(out T item)
        // имя метода странноватое, но вроде бы точно отражает то, что метод делает
        {
            using (_sync.GetLocked())
            {
                while (_queue.Count == 0 && !IsAddingCompleted)
                {
                    _sync.Wait();
                }

                if (_queue.Count <= 0)
                {
                    item = default(T);
                    return false;
                }

                item = _queue.Dequeue();
                _sync.Pulse();
                return true;
            }
        }
    }
}