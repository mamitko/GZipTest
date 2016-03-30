using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GZipTest.Parallelizing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestGZipTest
{
    class MyCustomException : Exception { };

    [TestClass]
    public class TestParallellizing
    {
        [TestMethod]
        public void TestInGeneral()
        {
            var sum = Enumerable.Range(0, 10).Select(i => i*2).Sum();
            var sum1 = 0;
            var sum2 = Enumerable.Range(0, 10).SelectParallely(i =>
            {
                Interlocked.Add(ref sum1, i*2);
                return i*2;
            }).AsEnumerable().Sum();

            Assert.AreEqual(sum, sum1);
            Assert.AreEqual(sum, sum2);
        }

        [TestMethod]
        public void TestChainOfSelects()
        {
            var sum = Enumerable.Range(0, 1000).Select(i => i*2*3*4).Sum();
            
            var sum2 = Enumerable.Range(0, 1000)
                .SelectParallely(i => i*2)
                .SelectParallely(i => i*3)
                .SelectParallely(i => i*4)
                .Sum();

            Assert.AreEqual(sum, sum2);
        }

        [TestMethod]
        public void TestSelectParallelException()
        {
            var select = Enumerable.Range(0, 100).SelectParallely(i =>
            {
                if (i > 3)
                    throw new MyCustomException();
                return i*2;
            });

            AssertEx.Throws<CrossThreadTransferredException>(() => select.ToList(), 
                e => e.InnerException is MyCustomException);
        }

        [TestMethod]
        public void TestSelectParallelCancellation()
        {
            var cancellation = new Cancellation();
            var maxNumber = 0;
            
            var selectResult = Enumerable.Range(0, int.MaxValue).SelectParallely(i =>
            {
                maxNumber = i;
                Thread.Sleep(1);
                return i;
            }, cancellation);

            cancellation.Cancel();

            AssertEx.Throws<OperationCanceledException>(() => selectResult.AsEnumerable().ToList());
            Assert.IsTrue(maxNumber < int.MaxValue-1);
        }

        [TestMethod]
        public void TestCancelStuckEnumerable()
        {
            var lastValue = -1;
            var cancellation = new Cancellation();
            var stickWhileEvent = new ManualResetEvent(false);
            var ints = new DisposableEnumerator<int>(StuckDownEnumerable(stickWhileEvent));

            var select = ints
                .SelectParallely(i => Interlocked.Exchange(ref lastValue, i), cancellation);

            new Thread(() =>
            {
                Thread.Sleep(100);
                cancellation.Cancel();
            }).Start();


            AssertEx.Throws<OperationCanceledException>(() => select.AsEnumerable().ToList());
            Assert.AreEqual(1, lastValue);

            stickWhileEvent.Set();
            Thread.Sleep(100);
            Assert.IsTrue(ints.IsDisposed);
        }

        [TestMethod]
        public void TestDisposingEnumerable()
        {
            var obj = new DisposableEnumerator<int>(Enumerable.Range(0, 11));
            Assert.IsFalse(obj.IsDisposed);
            var list = obj.SelectParallely(i => i).AsEnumerable().ToList();
            var sum = list.Sum();
            Assert.AreEqual(55, sum);
            Thread.Sleep(100);
            Assert.IsTrue(obj.IsDisposed);
        }

        [TestMethod]
        public void TestEnumerableThreadSafeWrapper()
        {
            var stickWhileEvent = new ManualResetEvent(false);
            var enumerator = new DisposableEnumerator<int>(StuckDownEnumerable(stickWhileEvent));
            var wrapper = new EnumerableThreadSafeWrapper<int>(enumerator);

            var result = new List<int>();
            
            new Thread(() =>
            {
                int i;
                if (wrapper.TryGetNext(out i)) 
                    lock (result) result.Add(i);
            }).Start();
            
            new Thread(() =>
            {
                int i;
                if (wrapper.TryGetNext(out i)) 
                    lock (result) result.Add(i);
            }).Start();

            Thread.Sleep(100);
            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(1, result[0]);
            
            wrapper.Dispose();
            // Source Enumerator is stuck down but Dispose returns immediately 
            // and real disposal is schedulled on the ending of last TryGetNex()

            int item;
            AssertEx.Throws<ObjectDisposedException>(() => wrapper.TryGetNext(out item)); 
            
            stickWhileEvent.Set();
            Thread.Sleep(100);
            
            // 2nd TryGetNext was invoked before Dispose() and should finish successfully
            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(2, result[1]);

            AssertEx.Throws<ObjectDisposedException>(() => wrapper.TryGetNext(out item));
        }

        [TestMethod]
        public void TestBlockingQueueStress()
        {
            var queue = new BlockingQueue<int>(1);

            var rnd = new ThreadLocal<Random>(() => new Random());
            var generatedTotal = 0;
            var consummedTotal = 0;

            var producers = TestMonitorSimple.RunSimultanously(5, () =>
            {
                for (var i = 0; i < 1e6; i++)
                {
                    var value = rnd.Value.Next(100);
                    Interlocked.Add(ref generatedTotal, value);
                    queue.AddIfNotCompleted(value);
                }
            }, false);

            var consumers = TestMonitorSimple.RunSimultanously(5, () =>
            {
                foreach (var value in queue.GetConsumingEnumerable())
                {
                    Interlocked.Add(ref consummedTotal, value);
                }
            }, false);

            producers.ForEach(t => t.Join());
            queue.CompleteAdding();

            consumers.ForEach(t => t.Join());

            Assert.IsTrue(consummedTotal == generatedTotal);
        }

        private static IEnumerable<int> StuckDownEnumerable(WaitHandle stickWhileEvent)
        {
            yield return 1;
            stickWhileEvent.WaitOne();
            yield return 2;
        }
    }
}