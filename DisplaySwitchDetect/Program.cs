using System;
using System.Diagnostics;
using System.Management;
using System.Threading;

namespace DisplaySwitchDetect
{
    class Program
    {
        /// <summary>
        /// Device connected/removed events can flood in.  Keep processing going one at a time.
        /// </summary>
        private static Semaphore _semaphore = new Semaphore(1, 1);

        /// <summary>
        /// Keeping track of whether the system thinks an external monitor is connected or not.
        /// </summary>
        private static bool _externalMonitorConnected = false;

        /// <summary>
        /// Main program.
        /// </summary>
        /// <param name="args">Command-line parameters</param>
        /// <seealso cref="https://stackoverflow.com/questions/620144/detecting-usb-drive-insertion-and-removal-using-windows-service-and-c-sharp"/>
        static void Main(string[] args)
        {
            ManagementEventWatcher watcher = new ManagementEventWatcher();
            WqlEventQuery query = new WqlEventQuery("SELECT * FROM Win32_DeviceChangeEvent WHERE EventType = 2 OR EventType = 3");
            watcher.EventArrived += new EventArrivedEventHandler(EventArrived);
            watcher.Query = query;
            watcher.Start();

            Enumerate();

            while (true)
            {
                watcher.WaitForNextEvent();
            }
        }

        /// <summary>
        /// Process a device connected or removed event.
        /// </summary>
        /// <param name="sender">Event sender</param>
        /// <param name="arguments">Event details</param>
        private static void EventArrived(object sender, EventArrivedEventArgs arguments)
        {
            _semaphore.WaitOne();

            Console.WriteLine("{0} - Something happened!", DateTime.Now);
            Enumerate();

            _semaphore.Release();
        }

        /// <summary>
        /// Enumerate all devices.  If there is an external monitor, limit the GPU speed.
        /// </summary>
        private static void Enumerate()
        {
            bool foundExternalMonitor = false;

            ManagementObjectSearcher searcher = new ManagementObjectSearcher("SELECT * FROM Win32_PnPEntity");
            foreach (ManagementObject obj in searcher.Get())
            {
                object hardwareIdsObj = obj.GetPropertyValue("HardwareID");
                if (hardwareIdsObj != null)
                {
                    string[] hardwareIds = (string[])hardwareIdsObj;
                    foreach (string hardwareId in hardwareIds)
                    {
                        if (hardwareId.StartsWith(@"MONITOR\"))
                        {
                            Console.WriteLine("  {0}", hardwareId);

                            if (!hardwareId.EndsWith(@"\LGD02DA")) // TODO: Make system built-in monitor ID configurable.
                            {
                                Console.WriteLine("    External monitor detected!");
                                foundExternalMonitor = true;
                            }
                        }
                    }
                }
            }

            Console.WriteLine("Done enumerating.");

            if (foundExternalMonitor)
            {
                Console.WriteLine("It looks like there is an external monitor connected.");

                if (!_externalMonitorConnected)
                {
                    _externalMonitorConnected = true;
                    SwitchState(true);
                }
            }
            else
            {
                Console.WriteLine("It looks like there is NOT an external monitor connected.");

                if (_externalMonitorConnected)
                {
                    _externalMonitorConnected = false;
                    SwitchState(false);
                }
            }
        }

        /// <summary>
        /// Call NVIDIA inspector and either set it to P8 (idle) or automatic P-state.
        /// </summary>
        /// <param name="forceIdle">If true, force to P8 (idle); if false, set to automatic P-state</param>
        private static void SwitchState(bool forceIdle)
        {
            Process process = new Process();
            process.StartInfo.FileName = @"C:\Program Files (x86)\NVIDIA Inspector\nvidiaInspector.exe"; // TODO: This shouldn't be hard-coded.
            process.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
            process.StartInfo.CreateNoWindow = true;

            if (forceIdle)
            {
                process.StartInfo.Arguments = "-forcepstate:0,8";
            }
            else
            {
                process.StartInfo.Arguments = "-forcepstate:0,16";
            }

            Console.WriteLine("  Running: {0} {1}", process.StartInfo.FileName, process.StartInfo.Arguments);

            process.Start();
            process.WaitForExit();
        }
    }
}
