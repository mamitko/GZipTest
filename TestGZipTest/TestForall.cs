﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using GZipTest.Parallelizing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestGZipTest
{
    [TestClass]
    public class TestForall
    {
        [TestMethod]
        public void TestWorksInGeneral()
        {
            var list = new List<int>();
            var forAll = new ForAll<int>(Enumerable.Range(0, 10),
                i =>
                {
                    lock (list)
                    {
                        list.Add(i);
                    }
                });

            var completed = new ManualResetEvent(false);
            forAll.OnCompleted(s => completed.Set());

            forAll.Start();

            completed.WaitOne();

            Assert.AreEqual(10, list.Distinct().Count());
        }

        [TestMethod]
        public void TestCancel()
        {
            bool? cancelled = null;

            var lastEnumerated = -1;
            var lastProcessed = -1;

            var cancellation = new Cancellation();

            var endlessSource = Enumerable.Range(0, 10).Select(n =>
            {
                if (n > 3)
                    //todo move to separated statc class TestHelpers
                    TestMonitorSimple.WaitAlittle();

                lastEnumerated = n;
                return n;
            });

            var fa = new ForAll<int>(endlessSource, n => Thread.VolatileWrite(ref lastProcessed, n),
                new ParallelSettings {Cancellation = cancellation});

            fa.OnCompleted( s => cancelled = s.Cancelled);

            fa.Start();
            TestMonitorSimple.WaitAlittle();
            cancellation.Cancel();
            TestMonitorSimple.WaitAlittle();

            Assert.IsTrue(lastProcessed > -1, "Looks like processing had not even started");
            Assert.IsTrue(lastEnumerated > -1, "Looks like enumeration of source had not even started");
            Assert.IsTrue(cancelled != null && cancelled.Value);
            Assert.IsTrue(fa.CompletionResult.Cancelled);
            Assert.IsTrue(lastProcessed < 9);
            Assert.IsTrue(lastEnumerated < 9);
        }
    }
}