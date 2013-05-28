using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Diagnostics;

namespace ToolBelt
{
    public class ProcessCycleStopwatch
    {
        ulong startCycles;
        ulong elapsedCycles;
        bool isRunning;

        public ProcessCycleStopwatch() 
        {
            if (!ProcessCycleStopwatch.IsSupported)
                throw new NotSupportedException("Process cycle timer not supported on Windows NT O/S versions below 6.0");

            Reset(); 
        }

        public void Reset() { startCycles = 0; elapsedCycles = 0; isRunning = false; }

        public void Start() 
        {
            if (!isRunning)
            {
                startCycles = GetProcessCycles();
                isRunning = true;
            }
        }

        public static ProcessCycleStopwatch StartNew()
        {
            ProcessCycleStopwatch stopwatch = new ProcessCycleStopwatch();

            stopwatch.Start();

            return stopwatch;
        }

        public static bool IsSupported
        {
            get
            {
                OperatingSystem vistaOr2008 = new OperatingSystem(PlatformID.Win32NT, new Version(6, 0));

                return (Environment.OSVersion.Version >= vistaOr2008.Version);
            }
        }

        public void Stop()
        {
            if (isRunning)
            {
                ulong elapsed = GetProcessCycles() - startCycles;
                
                elapsedCycles += elapsed;
                isRunning = false;
            }
        }

        public bool IsRunning { get { return isRunning; } }

        public ulong GetProcessCycles()
        {
            ulong cycles;

            NativeMethods.QueryProcessCycleTime(Process.GetCurrentProcess().Handle, out cycles);

            return cycles;
        }

        public ulong ElapsedCycles
        {
            get
            {
                ulong elapsed = elapsedCycles;

                // If we are running, add in the additional elapsed ticks since we last started
                if (isRunning)
                {
                    elapsedCycles += GetProcessCycles() - startCycles;
                }

                return elapsed; 
            }
        }

        private class NativeMethods
        {
            [DllImport("kernel32.dll", ExactSpelling = true, PreserveSig = true)]
            public static extern ulong QueryProcessCycleTime(IntPtr handle, out ulong cycles);
        }
    }
}
