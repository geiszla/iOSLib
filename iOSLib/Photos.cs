using IOSLib.Internal;
using IOSLib.LibIMobileDevice;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;

namespace IOSLib
{
    public class Photos
    {
        internal int firstNumber;
        internal int secondNumber;

        internal string assetTableName;
        internal string assetKeyName;
        internal string albumKeyName;


        iDevice device;
        public long Count;
        public List<Photo> PhotoList;

        internal string tempFolder;

        public Photos(iDevice currDevice)
        {
            device = currDevice;

            tempFolder = iOSLib.TempFolder + @"\" + currDevice.Udid;
            if (!Directory.Exists(tempFolder))
            {
                Directory.CreateDirectory(tempFolder);
            }
        }

        #region Main Functions
        internal void refreshList()
        {
            RefreshDatabase();
            List<Photo> photoList = SQLite.readPhotoList(tempFolder + @"\Photos.sqlite", device);

            Count = Convert.ToInt64(photoList.Count);
            PhotoList = photoList;
        }

        public void RefreshDatabase()
        {
            IntPtr afcClient;
            AFC.AFCError returnCode = AFC.afc_client_start_service(device.handle, out afcClient, "iOSLib");
            if (returnCode != AFC.AFCError.AFC_E_SUCCESS || afcClient == IntPtr.Zero)
            {
                return;
            }

            string photoDataPath = @"/PhotoData/Photos.sqlite";

            if (AFC.copyToDisk(afcClient, photoDataPath, tempFolder + @"\Photos.sqlite") != AFC.AFCError.AFC_E_SUCCESS)
            {
                AFC.afc_client_free(afcClient);
                return;
            }

            AFC.copyToDisk(afcClient, photoDataPath + "-wal", tempFolder + @"\Photos.sqlite-wal");
            AFC.copyToDisk(afcClient, photoDataPath + "-shm", tempFolder + @"\Photos.sqlite-shm");

            AFC.afc_client_free(afcClient);

            Count = SQLite.countPhotos(tempFolder + @"\Photos.sqlite");
        }

        public bool Exists(Photo photo)
        {
            return PhotoList.Any(x =>
            x.CreationTime == photo.CreationTime
            && x.FileType == photo.FileType
            && x.MediaType == photo.MediaType
            && x.ModificationTime == photo.ModificationTime
            && x.Size == photo.Size);
        }

        public void SavePhotos(List<Photo> photoList, string savePath)
        {
            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
            }

            IEnumerable photoLists = photoList.GroupBy(x => x.Device.Udid).Select(x => x.ToList());
            foreach (List<Photo> currPhotoList in photoLists)
            {
                iDevice currDevice = currPhotoList[0].Device;
                IntPtr afcClient;
                if (AFC.afc_client_start_service(device.handle, out afcClient, "iOSLib") != AFC.AFCError.AFC_E_SUCCESS
                    || afcClient == IntPtr.Zero)
                {
                    return;
                }

                foreach (Photo currPhoto in currPhotoList)
                {
                    AFC.copyToDisk(afcClient, currPhoto.getNameWithPath(), savePath + @"\" + currPhoto.getName());
                    Photo.serializeXml(currPhoto, savePath);
                }
            }
        }

        public void RemovePhotos(List<Photo> photoList)
        {
            IEnumerable photoLists = photoList.GroupBy(x => x.Device.Udid).Select(x => x.ToList());

            foreach (List<Photo> currPhotoList in photoLists)
            {
                iDevice currDevice = currPhotoList[0].Device;

                IntPtr afcClient;
                AFC.AFCError returnCode = AFC.afc_client_start_service(currDevice.handle, out afcClient, "iOSLib");
                if (returnCode != AFC.AFCError.AFC_E_SUCCESS || afcClient == IntPtr.Zero)
                {
                    continue;
                }
                currPhotoList[0].parent.RefreshDatabase();

                if (AFC.afc_remove_path(afcClient, @"/PhotoData/Photos.sqlite-shm") != AFC.AFCError.AFC_E_SUCCESS
                    || AFC.afc_remove_path(afcClient, @"/PhotoData/Photos.sqlite-wal") != AFC.AFCError.AFC_E_SUCCESS)
                {
                    AFC.afc_client_free(afcClient);
                    continue;
                }

                if (AFC.afc_remove_path(afcClient, @"/PhotoData/Photos.sqlite") != AFC.AFCError.AFC_E_SUCCESS)
                {
                    AFC.afc_client_free(afcClient);
                    continue;
                }

                foreach (Photo currPhoto in currPhotoList)
                {
                    SQLite.deleteFromDatabase(currPhoto);
                    returnCode = AFC.afc_remove_path(afcClient, currPhoto.getNameWithPath());
                }

                AFC.copyToDevice(afcClient, currPhotoList[0].parent.tempFolder + @"\Photos.sqlite", @"/PhotoData/Photos.sqlite");
                AFC.afc_client_free(afcClient);
            }
        }

        public void Restore(string path)
        {
            Photo photo = new Photo(path);

            IntPtr afcClient;
            ulong fileHandle;
            if (AFC.openPhoto(photo, device.Photos.PhotoList[device.Photos.PhotoList.Count - 1].Path + photo.getName(),
                device, AFC.FileOpenMode.AFC_FOPEN_WRONLY, out afcClient, out fileHandle) != AFC.AFCError.AFC_E_SUCCESS
                || afcClient == IntPtr.Zero || fileHandle == 0)
            {
                return;
            }

            byte[] fileContent = File.ReadAllBytes(photo.getNameWithPath());
            uint bytesWritten;
            AFC.afc_file_write(afcClient, fileHandle, fileContent, Convert.ToUInt32(photo.Size), out bytesWritten);

            AFC.closePhoto(afcClient, fileHandle);
        }

        public string CustomDatabaseSearch(string tableName, string columnName, string primaryKeyName, string primaryKeyValue)
        {
            SQLiteConnection sqlConnection = new SQLiteConnection(@"Data Source=" + tempFolder + @"\Photos.sqlite;Version=3;New=False;");
            sqlConnection.Open();

            string value = SQLite.getProperty(sqlConnection, tableName, columnName, primaryKeyName, primaryKeyValue);

            sqlConnection.Close();
            return value;
        }
        #endregion

        #region Helper Functions
        internal void setAlbumToAssetTableNames(int firstNumber, int secondNumber)
        {
            this.firstNumber = firstNumber;
            this.secondNumber = secondNumber;

            assetTableName = "Z_" + firstNumber + "ASSETS";
            assetKeyName = "Z_" + secondNumber + "ASSETS";
            albumKeyName = "Z_" + firstNumber + "ALBUMS";
        }
        #endregion
    }
}
