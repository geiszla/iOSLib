using IOSLib.LibIMobileDevice;
using System;

namespace IOSLib
{
    public class iDevice
    {
        internal IntPtr handle;
        public string Udid;
        public Photos Photos;

        public iDevice(string udid)
        {
            Udid = udid;
        }

        public void Connect()
        {
            IntPtr deviceHandle;
            LibiMobileDevice.IDeviceError returnCode = LibiMobileDevice.NewDevice(out deviceHandle, Udid);
            if (returnCode != LibiMobileDevice.IDeviceError.IDEVICE_E_SUCCESS || deviceHandle == IntPtr.Zero)
            {
                return;
            }

            handle = deviceHandle;
        }

        public void Disconnect()
        {
            LibiMobileDevice.FreeDevice(handle);
            handle = IntPtr.Zero;
        }

        public void GetPhotos()
        {
            Photos = new Photos(this);
            Photos.refreshList();
        }
    }
}
