using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace TestGZipTest
{
    public static class AssertEx
    {
        public static void Throws<TException>(Action a, Func<TException, bool> predicate = null, bool anyDescendantSuits = true, string message = "") where TException : Exception
        {
            try
            {
                a();
            }
            catch (TException e)
            {
                Assert.IsTrue(anyDescendantSuits || e.GetType() == typeof(TException), message);

                if (predicate != null)
                    Assert.IsTrue(predicate(e));

                return;
            }
            Assert.Fail(message);
        }
    }
}