using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GZipTest.Parallelizing;
using GZipTest.Threading;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestGZipTest
{
    [TestClass]
    public class TestMonitorSimple
    {
        [TestMethod]
        public void TestEnterExitInGeneral()
        {
            const int threadCount = 10;
            const int iterations = 100000;

            long n = 0;

            var montor = new MonitorSimple();

            RunSimultanously(threadCount, () =>
            {
                for (var i = 0; i < iterations; i++)
                {
                    using (montor.GetLocked())
                    using (montor.GetLocked())
                    using (montor.GetLocked())
                    {
                        n = n + 1;
                        if (i % 100 == 0)
                            Thread.SpinWait(10000);
                    }
                }
            }, true);

            var expected = Enumerable.Repeat(1, iterations).Sum() * threadCount;
            Assert.AreEqual(expected, n);
        }

        [TestMethod]
        public void TestStressLockedSectionsDoNotOverlapp()
        {
            const int iterations = 100000;

            var entered = 0;
            var overlapped = false;

            var m = new MonitorSimple();

            RunSimultanously(5, () =>
            {
                for (var i = 0; i < iterations; i++)
                {
                    using (m.GetLocked())
                    using (m.GetLocked())
                    {
                        if (Interlocked.CompareExchange(ref entered, 1, 0) != 0)
                            overlapped = true;

                        Thread.SpinWait(i % 100 == 0 ? 10000 : 1);

                        if (Interlocked.CompareExchange(ref entered, 0, 1) != 1)
                            overlapped = true;
                    }
                }
            }, true);

            Assert.IsTrue(entered == 0);
            Assert.IsFalse(overlapped);
        }

        [TestMethod]
        public void TestWait()
        {
            var e = new ManualResetEvent(false);
            var m = new MonitorSimple();

            var testValue = 0;

            var th1 = new Thread(() =>
            {
                m.Enter();
                testValue = 1;
                e.WaitOne();

                m.Wait();
                testValue = 2;
                m.Exit();
            }) { Name = "th1" };

            var th2 = new Thread(() =>
            {
                m.Enter();
                testValue = 100;
                m.Exit();
            }) { Name = "th2" };

            th1.Start();

            WaitAlittle();

            th2.Start();

            WaitAlittle();

            Assert.AreEqual(1, testValue);

            e.Set();

            WaitAlittle();

            Assert.AreEqual(100, testValue);

            m.Enter();
            m.PulseAll();
            m.Exit();

            WaitAlittle();

            Assert.AreEqual(2, testValue);
        }

        [TestMethod]
        public void TestWaitPulseStress()
        {
            var queue = new BlockingQueue<int>(1);

            var rnd = new ThreadLocal<Random>(() => new Random());
            var generatedTotal = 0;
            var consummedTotal = 0;
            
            var producers = RunSimultanously(5, () =>
            {
                for (var i = 0; i < 1e6; i++)
                {
                    var value = rnd.Value.Next(100);
                    Interlocked.Add(ref generatedTotal, value);
                    queue.AddIfNotCompleted(value);
                }
            }, false);
            
            var consumers = RunSimultanously(5, () =>
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

        [TestMethod]
        public void TestStressWait()
        {
            const int iterations = 1000000;
            const int doHardWorkBeforeEachNthProducing = iterations / 100;
            var threadCount = Environment.ProcessorCount;

            var src = 0;
            var dst = 0;
            var addingCompleted = false;

            var m = new MonitorSimple();

            var readers = RunSimultanously(threadCount, () =>
            {
                while (true)
                {
                    using (m.GetLocked())
                    using (m.GetLocked())
                    using (m.GetLocked())
                    {
                        while (src == 0 && !addingCompleted)
                            m.Wait();

                        if (addingCompleted && src == 0)
                            break;

                        src = src - 1;
                        dst = dst + 1;
                    }
                }
            }, false);

            RunSimultanously(threadCount, () =>
            {
                for (var i = 0; i < iterations; i++)
                {
                    if (i % doHardWorkBeforeEachNthProducing == 0)
                        DoHardWork();

                    using (m.GetLocked())
                    using (m.GetLocked()) //for extra stressing
                    {
                        src = src + 1;

                        // such a weird construct is for extra stressing
                        if (i % 2 == 0)
                            m.Pulse();
                        else
                            m.PulseAll();
                    }
                }
            }, true);

            using (m.GetLocked())
            {
                addingCompleted = true;
                m.PulseAll();
            }

            readers.ForEach(r => r.Join());

            var expected = Enumerable.Repeat(1, iterations).Sum() * threadCount;

            Assert.AreEqual(expected, dst);
        }

        [TestMethod]
        public void TestCheckingEnterExitOwnership()
        {
            var monitor = new MonitorSimple();

            var finished = 0;

            var th = new Thread(() =>
            {
                monitor.Enter();
                while (finished == 0) { }
            }) { IsBackground = true };

            th.Start();

            WaitAlittle();

            AssertEx.Throws<SynchronizationLockException>(() => monitor.Exit());

            Thread.VolatileWrite(ref finished, 1);
        }

        [TestMethod]
        public void TestCheckingWaitPulseOwnership()
        {
            var monitor = new MonitorSimple();
            var finished = 0;

            AssertEx.Throws<SynchronizationLockException>(() => { monitor.Wait(); });
            AssertEx.Throws<SynchronizationLockException>(() => { monitor.Pulse(); });
            AssertEx.Throws<SynchronizationLockException>(() => { monitor.PulseAll(); });

            var th = new Thread(() =>
            {
                monitor.Enter();
                while (finished == 0) { }
            }) { IsBackground = true };
            th.Start();
            WaitAlittle();

            // other thread has lock
            AssertEx.Throws<SynchronizationLockException>(() => { monitor.Wait(); });
            AssertEx.Throws<SynchronizationLockException>(() => { monitor.Pulse(); });
            AssertEx.Throws<SynchronizationLockException>(() => { monitor.PulseAll(); });

            Thread.VolatileWrite(ref finished, 1);
        }


        private static List<Thread> RunSimultanously(int threadCount, Action action, bool waitUntilFinished)
        {
            var threads = Enumerable.Repeat(0, threadCount).Select(
                i => new Thread(() => action()) { IsBackground = true }).ToList();

            threads.ForEach(th => th.Start());

            if (waitUntilFinished)
                threads.ForEach(th => th.Join());

            return threads;
        }

        public static void WaitAlittle()
        {
            Thread.Sleep(50);
        }

        private static void DoHardWork()
        {
            Thread.SpinWait(100000);
        }
    }
}
