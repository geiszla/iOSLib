using System;
using System.Runtime.InteropServices;

namespace IOSLib.LibIMobileDevice
{
    public class LibiMobileDevice
    {
        internal const string libimobiledeviceDllPath = @"libimobiledevice.dll";
        public const string LibplistDllPath = @"libplist.dll";
        internal enum IDeviceError
        {
            IDEVICE_E_SUCCESS = 0,
            IDEVICE_E_INVALID_ARG = -1,
            IDEVICE_E_UNKNOWN_ERROR = -2,
            IDEVICE_E_NO_DEVICE = -3,
            IDEVICE_E_NOT_ENOUGH_DATA = -4,
            IDEVICE_E_BAD_HEADER = -5,
            IDEVICE_E_SSL_ERROR = -6
        }

        #region Dll Import
        [DllImport(libimobiledeviceDllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IDeviceError idevice_get_device_list(out IntPtr devicesPtr, out int count);

        [DllImport(libimobiledeviceDllPath, EntryPoint = "idevice_new", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IDeviceError NewDevice(out IntPtr deviceHandle, string udid);

        [DllImport(libimobiledeviceDllPath, EntryPoint = "idevice_free", CallingConvention = CallingConvention.Cdecl)]
        internal static extern IDeviceError FreeDevice(IntPtr deviceHandle);

        [DllImport(libimobiledeviceDllPath, CallingConvention = CallingConvention.Cdecl)]
        internal static extern IDeviceError idevice_device_list_free(IntPtr devicesPtr);
        #endregion
    }
}
