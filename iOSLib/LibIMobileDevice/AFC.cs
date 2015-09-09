using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.IO;

namespace IOSLib.LibIMobileDevice
{
    internal class AFC
    {
        internal enum AFCError
        {
            AFC_E_SUCCESS = 0,
            AFC_E_UNKNOWN_ERROR = 1,
            AFC_E_OP_HEADER_INVALID = 2,
            AFC_E_NO_RESOURCES = 3,
            AFC_E_READ_ERROR = 4,
            AFC_E_WRITE_ERROR = 5,
            AFC_E_UNKNOWN_PACKET_TYPE = 6,
            AFC_E_INVALID_ARG = 7,
            AFC_E_OBJECT_NOT_FOUND = 8,
            AFC_E_OBJECT_IS_DIR = 9,
            AFC_E_PERM_DENIED = 10,
            AFC_E_SERVICE_NOT_CONNECTED = 11,
            AFC_E_OP_TIMEOUT = 12,
            AFC_E_TOO_MUCH_DATA = 13,
            AFC_E_END_OF_DATA = 14,
            AFC_E_OP_NOT_SUPPORTED = 15,
            AFC_E_OBJECT_EXISTS = 16,
            AFC_E_OBJECT_BUSY = 17,
            AFC_E_NO_SPACE_LEFT = 18,
            AFC_E_OP_WOULD_BLOCK = 19,
            AFC_E_IO_ERROR = 20,
            AFC_E_OP_INTERRUPTED = 21,
            AFC_E_OP_IN_PROGRESS = 22,
            AFC_E_INTERNAL_ERROR = 23,
            AFC_E_MUX_ERROR = 30,
            AFC_E_NO_MEM = 31,
            AFC_E_NOT_ENOUGH_DATA = 32,
            AFC_E_DIR_NOT_EMPTY = 33,
            AFC_E_FORCE_SIGNED_TYPE = -1
        }
        internal enum FileOpenMode
        {
            AFC_FOPEN_RDONLY = 0x00000001,
            AFC_FOPEN_RW = 0x00000002,
            AFC_FOPEN_WRONLY = 0x00000003,
            AFC_FOPEN_WR = 0x00000004,
            AFC_FOPEN_APPEND = 0x00000005,
            AFC_FOPEN_RDAPPEND = 0x00000006
        }

        // Connect
        #region DllImport
        [DllImport("libimobiledevice.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern AFCError afc_client_start_service(IntPtr deviceHandle, out IntPtr afcClient, string label);
        #endregion

        // Working with AFC
        #region DllImport
        [DllImport("libimobiledevice.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern AFCError afc_read_directory(IntPtr afcClient, string directoryPath, out IntPtr directoryInfo);

        [DllImport("libimobiledevice.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern AFCError afc_file_open(IntPtr afcClient, string fileName, FileOpenMode fileMode, out ulong fileHandle);

        [DllImport("libimobiledevice.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern AFCError afc_file_close(IntPtr afcClient, ulong fileHandle);

        [DllImport("libimobiledevice.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern AFCError afc_get_file_info(IntPtr afcClient, string fileName, out IntPtr fileInfo);

        [DllImport("libimobiledevice.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern AFCError afc_file_read(IntPtr afcClient, ulong fileHandle, byte[] data, uint length, out uint bytesRead);

        [DllImport("libimobiledevice.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern AFCError afc_file_write(IntPtr afcClient, ulong fileHandle, byte[] data, uint length, out uint bytesWritten);

        [DllImport("libimobiledevice.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern AFCError afc_remove_path(IntPtr afcClient, string filePath);
        #endregion

        internal static AFCError openPhoto(Photo photo, string photoPath, iDevice device, FileOpenMode openMode, out IntPtr afcClient, out ulong fileHandle)
        {
            fileHandle = 0;
            AFCError returnCode = afc_client_start_service(device.handle, out afcClient, "iOSLib");
            if (returnCode != AFCError.AFC_E_SUCCESS)
            {
                return returnCode;
            }

            if ((returnCode = afc_file_open(afcClient, photoPath + photo.getName(), openMode, out fileHandle))
                != AFCError.AFC_E_SUCCESS)
            {
                afc_client_free(afcClient);
            }

            return returnCode;
        }

        internal static AFCError closePhoto(IntPtr afcClient, ulong fileHandle)
        {
            AFCError returnCode = afc_file_close(afcClient, fileHandle);

            if (afcClient != IntPtr.Zero)
            {
                returnCode = afc_client_free(afcClient);
            }

            return returnCode;
        }

        internal static AFCError copyToDisk(IntPtr afcClient, string filePath, string savePath)
        {
            IntPtr infoPtr;
            AFCError returnCode = afc_get_file_info(afcClient, filePath, out infoPtr);
            if (returnCode != AFCError.AFC_E_SUCCESS)
            {
                return returnCode;
            }

            List<string> infoList = iOSLib.ptrToStringList(infoPtr, 0);
            long fileSize = Convert.ToInt64(infoList[infoList.FindIndex(x => x == "st_size") + 1]);

            ulong fileHandle;
            returnCode = afc_file_open(afcClient, filePath, FileOpenMode.AFC_FOPEN_RDONLY, out fileHandle);
            if (returnCode != AFCError.AFC_E_SUCCESS)
            {
                return returnCode;
            }

            FileStream fileStream = new FileStream(savePath, FileMode.Create, FileAccess.Write);
            const int bufferSize = 4194304;
            for (int i = 0; i < fileSize / bufferSize + 1; i++)
            {
                uint bytesRead;

                long remainder = fileSize - i * bufferSize;
                int currBufferSize = remainder >= bufferSize ? bufferSize : (int)remainder;
                byte[] currBuffer = new byte[currBufferSize];

                if ((returnCode = afc_file_read(afcClient, fileHandle, currBuffer, Convert.ToUInt32(currBufferSize), out bytesRead))
                    != AFCError.AFC_E_SUCCESS)
                {
                    afc_file_close(afcClient, fileHandle);
                    return returnCode;
                }

                fileStream.Write(currBuffer, 0, currBufferSize);
            }

            fileStream.Close();
            returnCode = afc_file_close(afcClient, fileHandle);

            return returnCode;
        }

        internal static AFCError copyToDevice(IntPtr afcClient, string filePath, string savePath)
        {
            byte[] fileContent = File.ReadAllBytes(filePath);

            ulong fileHandle;
            AFCError returnCode = afc_file_open(afcClient, savePath, FileOpenMode.AFC_FOPEN_WRONLY, out fileHandle);
            if (returnCode != AFCError.AFC_E_SUCCESS)
            {
                return returnCode;
            }

            uint bytesWritten;
            returnCode = afc_file_write(afcClient, fileHandle, fileContent, Convert.ToUInt32(fileContent.Length), out bytesWritten);

            return returnCode;
        }

        // Freeing
        #region DllImport
        [DllImport("libimobiledevice.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern AFCError afc_client_free(IntPtr afcClient);

        [DllImport("libimobiledevice.dll", CallingConvention = CallingConvention.Cdecl)]
        internal static extern AFCError afc_dictionary_free(IntPtr dictionary);
        #endregion
    }
}
