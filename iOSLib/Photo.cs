using IOSLib.Internal;
using IOSLib.LibIMobileDevice;
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;

namespace IOSLib
{
    public class Photo
    {
        #region Variables
        public int Id;
        internal int additionalId;
        public string Name;
        public string Extension;
        public string MediaType;
        public string FileType;

        [XmlIgnore]
        public iDevice Device;
        internal Photos parent;
        public string Path;
        public int Size;

        internal double modificationTimeSeconds;
        public DateTime ModificationTime;
        internal double creationTimeSeconds;
        public DateTime CreationTime;
        public List<Album> AlbumList;

        internal static string[] imageFormats = { "tiff", "tif", "jpg", "jpeg", "gif", "png", "bmp", "bmpf", "ico", "cur", "xbm" };
        internal static string[] videoFormats = { "m4v", "mp4", "mov" };
        #endregion

        #region Constructors
        private Photo() { }
        public Photo(Photos parent, int id, int additionalId, string name, string extension, string mediaType, string fileType, iDevice device, string path, int size,
            double creationTimeSeconds, DateTime creationTime, double modificationTimeSeconds, DateTime modificationTime, List<Album> albumList)
        {
            this.parent = parent;
            Device = device;

            Id = id;
            this.additionalId = additionalId;
            Name = name;
            Extension = extension;
            MediaType = mediaType;
            FileType = fileType;

            Path = path;
            Size = size;
            this.creationTimeSeconds = creationTimeSeconds;
            CreationTime = creationTime;
            this.modificationTimeSeconds = modificationTimeSeconds;
            ModificationTime = modificationTime;

            AlbumList = albumList;
        }

        public Photo(string path)
        {
            string extension = System.IO.Path.GetExtension(path).Substring(1);
            if (File.Exists(path) && imageFormats.Concat(videoFormats).Any(x => x == extension.ToLower()))
            {
                string mediaType = imageFormats.Any(x => x == extension.ToLower()) ? "Photo" : "Video";
                FileInfo imageFile = new FileInfo(path);

                Name = System.IO.Path.GetFileNameWithoutExtension(path);
                MediaType = mediaType;
                FileType = extension;
                Path = System.IO.Path.GetDirectoryName(path) + @"\";
                Size = Convert.ToInt32(imageFile.Length);

                string xmlName = Path + Name + ".xml";
                if (File.Exists(xmlName))
                {
                    XDocument plist = XDocument.Load(xmlName);
                    ModificationTime = Convert.ToDateTime(plist.Descendants("ModificationTime").FirstOrDefault().Value);
                    CreationTime = Convert.ToDateTime(plist.Descendants("CreationTime").FirstOrDefault().Value);
                }

                else
                {
                    ModificationTime = imageFile.LastWriteTime;
                    CreationTime = imageFile.CreationTime;
                }
            }
        }
        #endregion

        #region Main Functions
        public void Save(string savePath)
        {
            IntPtr afcClient;
            AFC.AFCError returnCode = AFC.afc_client_start_service(Device.handle, out afcClient, "iOSLib");
            if (returnCode != AFC.AFCError.AFC_E_SUCCESS || afcClient == IntPtr.Zero)
            {
                return;
            }

            if (AFC.copyToDisk(afcClient, getNameWithPath(), savePath + @"\" + getName()) != AFC.AFCError.AFC_E_SUCCESS)
            {
                AFC.afc_client_free(afcClient);
                return;
            }
            AFC.afc_client_free(afcClient);

            serializeXml(this, savePath);
        }

        public void Remove()
        {
            parent.RefreshDatabase();
            SQLite.deleteFromDatabase(this);

            IntPtr afcClient;
            AFC.AFCError returnCode = AFC.afc_client_start_service(Device.handle, out afcClient, "iOSLib");
            if (returnCode != AFC.AFCError.AFC_E_SUCCESS || afcClient == IntPtr.Zero)
            {
                return;
            }
            returnCode = AFC.afc_remove_path(afcClient, getNameWithPath());

            if (AFC.afc_remove_path(afcClient, @"/PhotoData/Photos.sqlite-shm") != AFC.AFCError.AFC_E_SUCCESS
                || AFC.afc_remove_path(afcClient, @"/PhotoData/Photos.sqlite-wal") != AFC.AFCError.AFC_E_SUCCESS)
            {
                AFC.afc_client_free(afcClient);
                return;
            }

            if (AFC.afc_remove_path(afcClient, @"/PhotoData/Photos.sqlite") != AFC.AFCError.AFC_E_SUCCESS)
            {
                AFC.afc_client_free(afcClient);
                return;
            }

            AFC.copyToDevice(afcClient, parent.tempFolder + @"\Photos.sqlite", @"/PhotoData/Photos.sqlite");
            AFC.afc_client_free(afcClient);
        }

        public string GetProperty(string propertyName)
        {
            SQLiteConnection sqlConnection = new SQLiteConnection(@"Data Source=" + parent.tempFolder + @"\Photos.sqlite;Version=3;New=False;");
            sqlConnection.Open();

            string value;
            try
            {
                value = SQLite.getProperty(sqlConnection, "ZGENERICASSET", propertyName, "Z_PK", Id.ToString());
            }
            catch (SQLiteException)
            {
                value = SQLite.getProperty(sqlConnection, "ZADDITIONALASSETATTRIBUTES", propertyName, "Z_PK", Id.ToString());
            }

            sqlConnection.Close();
            return value;
        }
        #endregion

        #region Helper Functions
        internal string getName()
        {
            return Name + "." + Extension;
        }

        internal string getNameWithPath()
        {
            return Path + "/" + Name + "." + Extension;
        }

        internal static double convertToUnixDate(DateTime inputDateTime)
        {
            return (inputDateTime - new DateTime(2001, 1, 1).ToLocalTime()).TotalSeconds;
        }

        internal static void serializeXml(Photo photo, string savePath)
        {
            XmlSerializer serializer = new XmlSerializer(photo.GetType());
            XmlWriter xmlWriter = XmlWriter.Create(savePath + @"\" + photo.Name + ".xml");
            serializer.Serialize(xmlWriter, photo);
            xmlWriter.Close();
        }
        #endregion
    }
}
