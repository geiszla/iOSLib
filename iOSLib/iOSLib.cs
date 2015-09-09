using IOSLib.Internal;
using IOSLib.LibIMobileDevice;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace IOSLib
{
    public class iOSLib
    {
        public static string TempFolder = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + @"\iOSLib";

        public static List<iDevice> GetDevices()
        {
            IntPtr devicesPtr;
            int count;
            LibiMobileDevice.IDeviceError returnCode = LibiMobileDevice.idevice_get_device_list(out devicesPtr, out count);

            List<iDevice> deviceList = new List<iDevice>();
            if (returnCode != LibiMobileDevice.IDeviceError.IDEVICE_E_SUCCESS || devicesPtr == IntPtr.Zero
                || count == 0 || Marshal.ReadInt32(devicesPtr) == 0)
            {
                return deviceList;
            }

            string currUdid;
            int i = 0;
            while ((currUdid = Marshal.PtrToStringAnsi(Marshal.ReadIntPtr(devicesPtr, i))) != null)
            {
                deviceList.Add(new iDevice(currUdid));
                i = i + 4;
            }

            LibiMobileDevice.idevice_device_list_free(devicesPtr);

            return deviceList;
        }

        internal static List<string> ptrToStringList(IntPtr listPtr, int skip)
        {
            List<string> stringList = new List<string>();
            if (Marshal.ReadInt32(listPtr) == 0)
            {
                return stringList;
            }

            string currString;
            int i = skip * 4;
            while ((currString = Marshal.PtrToStringAnsi(Marshal.ReadIntPtr(listPtr, i))) != null)
            {
                stringList.Add(currString);
                i = i + 4;
            }

            return stringList;
        }
    }
}
