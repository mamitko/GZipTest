using System.Collections.Generic;
using System.Linq;
using System.Threading;
using GZipTest.Parallelizing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestGZipTest
{
    [TestClass]
    public class TestForeach
    {
        [TestMethod]
        public void TestWorksInGeneral()
        {
            var list = new List<int>();
            var forAll = new ForAll<int>(Enumerable.Range(0, 10), new ParallelSettings(),
                i =>
                {
                    lock (list)
                    {
                        list.Add(i);
                    }
                });

            var completed = new ManualResetEvent(false);
            forAll.Completed += delegate { completed.Set(); };

            forAll.Start();

            completed.WaitOne();

            Assert.AreEqual(10, list.Distinct().Count());
        }
    }
}