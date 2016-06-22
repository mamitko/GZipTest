using System;
using System.Runtime.InteropServices;
using System.Security.Permissions;
using System.Threading;

namespace GZipTest.Threading
{
    struct SpinWaitStolen
    // Сopypasted from FCL source code just a little bit less than completly
    {
        [DllImport("kernel32.dll"), HostProtection(SecurityAction.LinkDemand, ExternalThreading = true)]
        static extern bool SwitchToThread();

        private static readonly bool IsSingleProcessor = Environment.ProcessorCount == 1;
        // Author assumes that singlecore hyperthreading CPUs are quite rare so the number of logical cores will fit the needs.
        // Think it works for virtual machines too.

        private int _spinsOnceDone;
        
        public void SpinOnce()
        {
            const int trueSpinsBeforeYeild = 10;

            if (_spinsOnceDone < trueSpinsBeforeYeild && !IsSingleProcessor)
            {
                Thread.SpinWait(4 << _spinsOnceDone);
            }
            else
            {
                var spinsAfterTrueSpins = _spinsOnceDone >= trueSpinsBeforeYeild ? trueSpinsBeforeYeild - 10 : _spinsOnceDone;

                if (spinsAfterTrueSpins % 20 == 19)
                {
                    Thread.Sleep(1);
                }
                else
                {
                    if (spinsAfterTrueSpins % 5 == 4)
                    {
                        Thread.Sleep(0);
                    }
                    else
                    {
                        SwitchToThread(); // instead of Thread.Yield;
                    }
                }
            }
            
            _spinsOnceDone = _spinsOnceDone < int.MaxValue ? _spinsOnceDone + 1 : trueSpinsBeforeYeild;
        }

        // from FCL source code:
        // We prefer to call Thread.Yield first, triggering a SwitchToThread. This
        // unfortunately doesn't consider all runnable threads on all OS SKUs. In
        // some cases, it may only consult the runnable threads whose ideal processor
        // is the one currently executing code. Thus we oc----ionally issue a call to
        // Sleep(0), which considers all runnable threads at equal priority. Even this
        // is insufficient since we may be spin waiting for lower priority threads to
        // execute; we therefore must call Sleep(1) once in a while too, which considers
        // all runnable threads, regardless of ideal processor and priority, but may
        // remove the thread from the scheduler's queue for 10+ms, if the system is
        // configured to use the (default) coarse-grained system timer.
    }
}
