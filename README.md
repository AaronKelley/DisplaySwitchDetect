# DisplaySwitchDetect
Detect when an external monitor is connected to or disconnected from the laptop, and take action.

This is a quick hack job that I am releasingly publicly, as others may find it useful and be able to tweak it to their needs.  This basically uses WMI to watch for device "connect" and "disconnect" events.  Every time one happens, it enumerates the devices in the PC, looking specifically for monitors.  If a monitor is found that isn't the laptop built-in montior, that means that an external monitor is connected to the laptop.  In my case I need to limit the discrete GPU speed when an external monitor is connected, so it fires a call to NVIDIA Inspector to do this, and another one to set it back to automatic speed if the external monitor is disconnected.

The project is a C# Visual Studio solution.  It is ready to simply open, build, and run if you have .NET Framework 4.7.2 installed.
