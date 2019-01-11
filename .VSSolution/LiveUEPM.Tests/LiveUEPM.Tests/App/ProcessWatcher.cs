using System;
using System.Diagnostics;
using System.Management;

namespace LiveUEPM.Tests.App
{
    public class ProcessWatch : IDisposable
    {
        private bool disposed = false;
        private bool isUnityTriggered;

        private ManagementEventWatcher processStartEvent = new ManagementEventWatcher("SELECT * FROM Win32_ProcessStartTrace");
        private ManagementEventWatcher processStopEvent = new ManagementEventWatcher("SELECT * FROM Win32_ProcessStopTrace");

        public event Action<PropertyDataCollection> UnityInitialized = delegate { };

        public event Action<PropertyDataCollection> UnityStopped = delegate { };

        public bool Debug { get; }

        private ProcessWatch()
        {
        }

        public ProcessWatch(bool fDebug = false)
        {
            Debug = fDebug;
            isUnityTriggered = Process.GetProcessesByName("UnityCrashHandler64").Length > 0;

            processStartEvent.EventArrived += new EventArrivedEventHandler(processStartEvent_EventArrived);
            processStartEvent.Start();
            processStopEvent.EventArrived += new EventArrivedEventHandler(processStopEvent_EventArrived);
            processStopEvent.Start();
        }

        // Public implementation of Dispose pattern callable by consumers.
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        // Protected implementation of Dispose pattern.
        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                processStartEvent.Stop();
                processStartEvent.Dispose();

                processStopEvent.Stop();
                processStopEvent.Dispose();
            }

            disposed = true;
        }

        ~ProcessWatch()
        {
            processStartEvent.Stop();
        }

        private void processStartEvent_EventArrived(object sender, EventArrivedEventArgs e)
        {
            PropertyDataCollection props;
            if (IsUnityProcess(e, out props, false))
                UnityInitialized?.Invoke(props);

            if (Debug)
                DebugInfo(e, false);
        }

        private void processStopEvent_EventArrived(object sender, EventArrivedEventArgs e)
        {
            PropertyDataCollection props;
            if (IsUnityProcess(e, out props, true))
                UnityInitialized?.Invoke(props);

            if (Debug)
                DebugInfo(e, true);
        }

        private bool IsUnityProcess(EventArrivedEventArgs e, out PropertyDataCollection props, bool fStopped)
        {
            props = e.NewEvent.Properties;
            string processName = props["ProcessName"].Value.ToString();

            if (processName == "Unity.exe")
                isUnityTriggered = true;

            if (isUnityTriggered && (!fStopped && processName.Contains("UnityCrashHandler") || !fStopped && processName == "UnityShaderCompiler.exe"))
            {
                if (fStopped)
                    isUnityTriggered = false;

                return true;
            }

            return false;
        }

        private void DebugInfo(EventArrivedEventArgs e, bool fStopped)
        {
            string processName = e.NewEvent.Properties["ProcessName"].Value.ToString();
            string processID = Convert.ToInt32(e.NewEvent.Properties["ProcessID"].Value).ToString();

            Console.WriteLine($"Process {(fStopped ? "stopped" : "started")}. Name: " + processName + " | ID: " + processID);
        }
    }
}