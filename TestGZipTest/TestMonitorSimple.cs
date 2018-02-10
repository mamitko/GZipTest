using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
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
            const int ThreadCount = 10;
            const int Iterations = 100000;

            long n = 0;

            var monitor = new MonitorSimple();

            RunSimultaneously(ThreadCount, () =>
            {
                for (var i = 0; i < Iterations; i++)
                {
                    using (monitor.GetLocked())
                    using (monitor.GetLocked())
                    using (monitor.GetLocked())
                    {
                        n = n + 1;
                        if (i % 100 == 0)
                            Thread.SpinWait(10000);
                    }
                }
            }, true);

            var expected = Enumerable.Repeat(1, Iterations).Sum() * ThreadCount;
            Assert.AreEqual(expected, n);
        }

        [TestMethod]
        public void TestStressLockedSectionsDoNotOverlap()
        {
            const int Iterations = 100000;

            var entered = 0;
            var overlapped = false;

            var m = new MonitorSimple();

            RunSimultaneously(5, () =>
            {
                for (var i = 0; i < Iterations; i++)
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
            }) { Name = "Thread 1" };

            var th2 = new Thread(() =>
            {
                m.Enter();
                testValue = 100;
                m.Exit();
            }) { Name = "Thread 2" };

            th1.Start();

            WaitALittle();

            th2.Start();

            WaitALittle();

            Assert.AreEqual(1, testValue);

            e.Set();

            WaitALittle();

            Assert.AreEqual(100, testValue);

            m.Enter();
            m.PulseAll();
            m.Exit();

            WaitALittle();

            Assert.AreEqual(2, testValue);
        }

        [TestMethod]
        public void TestStressWait()
        {
            const int Iterations = 1000000;
            const int DoHardWorkBeforeEachNthProducing = Iterations / 100;

            var threadCount = Environment.ProcessorCount;

            var src = 0;
            var dst = 0;
            var addingCompleted = false;

            var m = new MonitorSimple();

            var readers = RunSimultaneously(threadCount, () =>
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

            RunSimultaneously(threadCount, () =>
            {
                for (var i = 0; i < Iterations; i++)
                {
                    if (i % DoHardWorkBeforeEachNthProducing == 0)
                        DoHardWork();

                    using (m.GetLocked())
                    using (m.GetLocked()) 
                    // nested locks for extra stressing
                    {
                        src = src + 1;

                        // for extra stressing
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

            var expected = Enumerable.Repeat(1, Iterations).Sum() * threadCount;

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

            WaitALittle();

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
            WaitALittle();

            // other thread has lock
            AssertEx.Throws<SynchronizationLockException>(() => { monitor.Wait(); });
            AssertEx.Throws<SynchronizationLockException>(() => { monitor.Pulse(); });
            AssertEx.Throws<SynchronizationLockException>(() => { monitor.PulseAll(); });

            Thread.VolatileWrite(ref finished, 1);
        }
        
        public static List<Thread> RunSimultaneously(int threadCount, Action action, bool waitUntilFinished)
        {
            var threads = Enumerable.Repeat(0, threadCount).Select(
                i => new Thread(() => action()) { IsBackground = true }).ToList();

            threads.ForEach(th => th.Start());

            if (waitUntilFinished)
                threads.ForEach(th => th.Join());

            return threads;
        }

        public static void WaitALittle()
        {
            Thread.Sleep(50);
        }

        private static void DoHardWork()
        {
            Thread.SpinWait(100000);
        }
    }
}
