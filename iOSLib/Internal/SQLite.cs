using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Data;
using System.Text.RegularExpressions;

namespace IOSLib.Internal
{
    class SQLite
    {
        #region Main FUcntions
        internal static List<Photo> readPhotoList(string photosSqlitePath, iDevice device)
        {
            SQLiteConnection sqlConnection = new SQLiteConnection(@"Data Source=" + photosSqlitePath + ";Version=3;New=False;");
            sqlConnection.Open();

            // Get name of photos and albums associative table.
            int firstNumber = 0;
            int secondNumber = 0;
            DataRowCollection tableRows = sqlConnection.GetSchema("Columns").Rows;
            foreach (DataRow currTable in tableRows)
            {
                Regex regEx = new Regex("Z_([0-9]{1,2})ASSETS");
                string tableName = currTable.ItemArray[2].ToString();
                if (regEx.IsMatch(tableName))
                {
                    firstNumber = Convert.ToInt32(regEx.Match(tableName).Groups[1].ToString());

                    string columnName = currTable.ItemArray[3].ToString();
                    regEx = new Regex("Z_([0-9]{1,2})ASSETS");
                    if (regEx.IsMatch(columnName))
                    {
                        secondNumber = Convert.ToInt32(regEx.Match(columnName).Groups[1].ToString());
                    }
                }
            }

            device.Photos.setAlbumToAssetTableNames(firstNumber, secondNumber);

            // Get list and basic properties of photos.
            SQLiteDataReader photoGenericReader = makeRequest(sqlConnection, @"SELECT *, cast(ZDATECREATED as string) AS DATECREATED, cast(ZMODIFICATIONDATE as string) AS MODIFICATIONDATE FROM ZGENERICASSET");
            List<Photo> photoList = new List<Photo>();
            while (photoGenericReader.Read())
            {
                string name = photoGenericReader["ZFILENAME"].ToString();

                if (name != "")
                {
                    // Id, name, extension, media type, filetype, path, creation time, modification time
                    Regex nameRegex = new Regex(@"(.*)\.(.*)");
                    GroupCollection regexGroups = nameRegex.Match(name).Groups;
                    name = regexGroups[1].ToString();
                    string extension = regexGroups[2].ToString();

                    int id = Convert.ToInt32(photoGenericReader["Z_PK"]);
                    string mediaType = Convert.ToInt16(photoGenericReader["ZKIND"]) == 0 ? "Photo" : "Video";
                    string fileType = photoGenericReader["ZUNIFORMTYPEIDENTIFIER"].ToString();
                    string path = photoGenericReader["ZDIRECTORY"].ToString();

                    double creationTimeSeconds = Convert.ToDouble(photoGenericReader["DATECREATED"]);
                    DateTime creationTime = new DateTime(2001, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                    creationTime = creationTime.AddSeconds(creationTimeSeconds).ToLocalTime();

                    double modificationTimeSeconds = Convert.ToDouble(photoGenericReader["MODIFICATIONDATE"]);
                    DateTime modificationTime = new DateTime(2001, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
                    modificationTime = modificationTime.AddSeconds(modificationTimeSeconds).ToLocalTime();

                    // File size
                    int size = 0;
                    string additionalId = photoGenericReader["ZADDITIONALATTRIBUTES"].ToString();
                    if (additionalId != "")
                    {
                        SQLiteDataReader photoAdditionalReader = makeRequest(sqlConnection, @"SELECT ZORIGINALFILESIZE FROM ZADDITIONALASSETATTRIBUTES WHERE Z_PK = " + additionalId);
                        photoAdditionalReader.Read();
                        size = Convert.ToInt32(photoAdditionalReader["ZORIGINALFILESIZE"]);
                        photoAdditionalReader.Close();
                    }

                    // Album id and name
                    List<Album> albumList = new List<Album>();
                    int albumId = 0;
                    string albumName = "";
                    string albumKeyName = device.Photos.albumKeyName;
                    SQLiteDataReader photoAlbumReader = makeRequest(sqlConnection, @"SELECT " + albumKeyName + " FROM " + device.Photos.assetTableName
                        + " WHERE " + device.Photos.assetKeyName + " = " + id);
                    while (photoAlbumReader.Read())
                    {
                        albumId = Convert.ToInt32(photoAlbumReader[albumKeyName]);

                        SQLiteDataReader albumTableReader = makeRequest(sqlConnection, @"SELECT ZTITLE FROM ZGENERICALBUM WHERE Z_PK = " + albumId);
                        if (albumTableReader.Read())
                        {
                            albumName = albumTableReader["ZTITLE"].ToString();
                        }

                        albumList.Add(new Album(albumId, albumName));
                        albumTableReader.Close();
                    }
                    photoAlbumReader.Close();

                    photoList.Add(new Photo(device.Photos, id, Convert.ToInt32(additionalId), name, extension, mediaType, fileType, device, path, size,
                        creationTimeSeconds, creationTime, modificationTimeSeconds, modificationTime, albumList));
                }
            }

            photoGenericReader.Close();
            sqlConnection.Close();
            return photoList;
        }

        internal static string getProperty(SQLiteConnection sqlConnection, string tableName, string columnName, string primaryKeyName, string primaryKeyValue)
        {
            string value = "";
            SQLiteDataReader propertyReader = makeRequest(sqlConnection, @"SELECT " + columnName + " FROM " + tableName + " WHERE " + primaryKeyName + " = " + primaryKeyValue);
            if (propertyReader.Read())
            {
                value = propertyReader[columnName].ToString();
            }
            propertyReader.Close();

            sqlConnection.Close();
            return value;
        }

        internal Dictionary<string, string> getAllProperties(string photosSqlitePath)
        {
            SQLiteConnection sqlConnection = new SQLiteConnection(@"Data Source=" + photosSqlitePath + ";Version=3;New=False;");
            sqlConnection.Open();

            SQLiteDataReader propertyReader = makeRequest(sqlConnection, @"");
            Dictionary<string, string> propertiesDic = new Dictionary<string, string>();

            return propertiesDic;
        }

        internal static long countPhotos(string photosSqlitePath)
        {
            SQLiteConnection sqlConnection = new SQLiteConnection(@"Data Source=" + photosSqlitePath + ";Version=3;New=False;");
            sqlConnection.Open();

            SQLiteDataReader photoCountReader = makeRequest(sqlConnection, @"SELECT COUNT(*) AS COUNT FROM ZGENERICASSET WHERE ZFILENAME IS NOT NULL");
            photoCountReader.Read();
            long count = Convert.ToInt64(photoCountReader["COUNT"]);
            photoCountReader.Close();

            sqlConnection.Close();
            return count;
        }

        internal static void deleteFromDatabase(Photo photo)
        {
            SQLiteConnection sqlConnection = new SQLiteConnection(@"Data Source=" + photo.parent.tempFolder + @"\Photos.sqlite;Version=3;New=False;");
            sqlConnection.Open();
            string updateString;

            // Get moment ids
            SQLiteDataReader dataReader = makeRequest(sqlConnection, @"SELECT ZMOMENT FROM ZGENERICASSET WHERE Z_PK = " + photo.Id);
            dataReader.Read();
            int momentId = Convert.ToInt32(dataReader["ZMOMENT"]);
            dataReader.Close();

            dataReader = makeRequest(sqlConnection, @"SELECT ZMEGAMOMENTLIST, ZCACHEDCOUNT FROM ZMOMENT WHERE Z_PK = " + momentId);
            dataReader.Read();
            int megaMomentId = Convert.ToInt32(dataReader["ZMEGAMOMENTLIST"]);
            int cachedCount = Convert.ToInt32(dataReader["ZCACHEDCOUNT"]);
            dataReader.Close();

            dataReader = makeRequest(sqlConnection, @"SELECT ZYEARMOMENTLIST FROM ZMOMENT WHERE ZMEGAMOMENTLIST = " + megaMomentId);
            dataReader.Read();
            int yearMomentId = Convert.ToInt32(dataReader["ZYEARMOMENTLIST"]);
            dataReader.Close();

            // Update moment
            if (cachedCount == 1)
            {
                makeChange(sqlConnection, @"DELETE FROM ZMOMENT WHERE Z_PK = " + momentId);
            }

            else
            {
                dataReader = makeRequest(sqlConnection, "SELECT MIN(ZDATECREATED) AS STARTDATE, MAX(ZDATECREATED) AS ENDDATE FROM ZGENERICASSET WHERE ZMOMENT = " + momentId
                + " AND Z_PK != " + photo.Id + " AND ZTRASHEDSTATE = 0");
                dataReader.Read();

                try
                {
                    double startDate = Convert.ToDouble(dataReader["STARTDATE"]);
                    double endDate = Convert.ToDouble(dataReader["ENDDATE"]);

                    updateString = "UPDATE ZMOMENT";
                    updateString += " SET Z_OPT = Z_OPT + 1, ZCACHEDCOUNT = ZCACHEDCOUNT - 1, "
                        + (photo.MediaType == "Photo" ? "ZCACHEDPHOTOSCOUNT = ZCACHEDPHOTOSCOUNT - 1" : "ZCACHEDVIDEOSCOUNT = ZCACHEDVIDEOSCOUNT - 1")
                        + ", ZREPRESENTATIVEDATE = " + startDate + ", ZSTARTDATE = " + startDate + ", ZENDDATE = " + endDate;
                    updateString += " WHERE Z_PK = " + momentId;
                    makeChange(sqlConnection, updateString);
                }
                catch { }

                dataReader.Close();
            }

            // Update megamoment
            dataReader = makeRequest(sqlConnection, @"SELECT MIN(ZSTARTDATE) AS MEGASTARTDATE, MAX(ZENDDATE) AS MEGAENDDATE FROM ZMOMENT WHERE ZMEGAMOMENTLIST = " + megaMomentId);
            dataReader.Read();
            double megaStartDate = 0;
            double megaEndDate = 0;
            try
            {
                megaStartDate = Convert.ToDouble(dataReader["MEGASTARTDATE"]);
                megaEndDate = Convert.ToDouble(dataReader["MEGAENDDATE"]);

                makeChange(sqlConnection, @"UPDATE ZMOMENTLIST SET Z_OPT = Z_OPT + 1, ZSTARTDATE = " + megaStartDate + ", ZREPRESENTATIVEDATE = " + megaStartDate
                    + ", ZENDDATE = " + megaEndDate + " WHERE Z_PK = " + megaMomentId);
            }

            catch
            {
                makeChange(sqlConnection, @"DELETE FROM ZMOMENTLIST WHERE Z_PK = " + megaMomentId);
            }
            dataReader.Close();

            // Update year moment
            dataReader = makeRequest(sqlConnection, @"SELECT MIN(ZSTARTDATE) AS YEARSTARTDATE, MAX(ZENDDATE) AS YEARENDDATE FROM ZMOMENT WHERE ZYEARMOMENTLIST = " + yearMomentId);
            dataReader.Read();
            try
            {
                double yearStartDate = Convert.ToDouble(dataReader["YEARSTARTDATE"]);
                double yearEndDate = Convert.ToDouble(dataReader["YEARENDDATE"]);

                makeChange(sqlConnection, @"UPDATE ZMOMENTLIST SET Z_OPT = Z_OPT + 1, ZSTARTDATE = " + yearStartDate + ", ZENDDATE = " + yearEndDate + " WHERE Z_PK = " + yearMomentId);
            }
            catch
            {
                makeChange(sqlConnection, @"DELETE FROM ZMOMENTLIST WHERE Z_PK = " + yearMomentId);
            }
            dataReader.Close();

            // Update album
            foreach (Album currAlbum in photo.AlbumList)
            {
                makeChange(sqlConnection, @"UPDATE ZGENERICALBUM SET Z_OPT = Z_OPT + 1, ZCACHEDCOUNT = ZCACHEDCOUNT - 1, "
                    + (photo.MediaType == "Photo" ? "ZCACHEDPHOTOSCOUNT = ZCACHEDPHOTOSCOUNT - 1" : "ZCACHEDVIDEOSCOUNT = ZCACHEDVIDEOSCOUNT - 1")
                    + " WHERE Z_PK = " + currAlbum.Id);
                makeChange(sqlConnection, @"UPDATE ZGENERICALBUM SET Z_OPT = Z_OPT + 1, ZKEYASSET = NULL, Z" + photo.parent.secondNumber + "_KEYASSET = NULL WHERE ZCACHEDCOUNT = 0 AND Z_PK = " + currAlbum.Id);

                dataReader = makeRequest(sqlConnection, @"SELECT ZKEYASSET, ZSECONDARYKEYASSET, ZTERTIARYKEYASSET FROM ZGENERICALBUM WHERE Z_PK = " + currAlbum.Id);
                dataReader.Read();
                string keyAsset = "";
                if ((keyAsset = dataReader["ZKEYASSET"].ToString()) == "")
                {
                    dataReader.Close();
                    continue;
                }

                if (Convert.ToInt32(dataReader["ZKEYASSET"]) == photo.Id || Convert.ToInt32(dataReader["ZSECONDARYKEYASSET"]) == photo.Id
                    || Convert.ToInt32(dataReader["ZTERTIARYKEYASSET"]) == photo.Id)
                {
                    dataReader.Close();

                    keyAsset = "";
                    string secondaryKeyAsset = "";
                    string tertiaryKeyAsset = "";
                    dataReader = makeRequest(sqlConnection, @"SELECT " + photo.parent.assetKeyName + " AS KEYASSET FROM " + photo.parent.assetTableName + " WHERE "
                        + photo.parent.albumKeyName + " = " + currAlbum.Id + " AND (SELECT ZTRASHEDSTATE FROM ZGENERICASSET WHERE Z_PK = "
                        + photo.parent.assetKeyName + ") != 1 AND " + photo.parent.assetKeyName + " != " + photo.Id + " ORDER BY " + photo.parent.assetKeyName + " DESC LIMIT 3");
                    for (int i = 0; i < 3; i++)
                    {
                        dataReader.Read();

                        try
                        {
                            switch (i)
                            {
                                case 0:
                                    keyAsset = dataReader["KEYASSET"].ToString();
                                    break;
                                case 1:
                                    secondaryKeyAsset = dataReader["KEYASSET"].ToString();
                                    break;
                                case 2:
                                    tertiaryKeyAsset = dataReader["KEYASSET"].ToString();
                                    break;
                                default:
                                    break;
                            }
                        }
                        catch { };
                    }

                    updateString = "UPDATE ZGENERICALBUM";
                    updateString += " SET Z_OPT = Z_OPT + 1, ZKEYASSET = " + (keyAsset != "" ? keyAsset : "NULL, Z17_KEYASSET = NULL")
                    + ", ZSECONDARYKEYASSET = " + (secondaryKeyAsset != "" ? secondaryKeyAsset : "NULL, Z" + photo.parent.firstNumber + "_SECONDARYKEYASSET = NULL")
                    + ", ZTERTIARYKEYASSET = " + (tertiaryKeyAsset != "" ? tertiaryKeyAsset : "NULL, Z" + photo.parent.firstNumber + "_TERTIARYKEYASSET = NULL");
                    updateString += " WHERE Z_PK = " + currAlbum.Id;
                    makeChange(sqlConnection, updateString);

                    dataReader.Close();
                }

                dataReader.Close();
            }

            // Delete photo from database
            makeChange(sqlConnection, @"DELETE FROM ZADDITIONALASSETATTRIBUTES WHERE Z_PK = " + photo.additionalId);
            makeChange(sqlConnection, @"DELETE FROM ZGENERICASSET WHERE Z_PK = " + photo.Id);
            makeChange(sqlConnection, @"DELETE FROM " + photo.parent.assetTableName + " WHERE " + photo.parent.assetKeyName + " = " + photo.Id);

            sqlConnection.Close();
        }
        #endregion

        #region Helper Fucntions
        static SQLiteDataReader makeRequest(SQLiteConnection sqlConnection, string requestString)
        {
            SQLiteCommand sqlCommand = sqlConnection.CreateCommand();
            sqlCommand.CommandText = requestString;

            SQLiteDataReader sqlDataReader = sqlCommand.ExecuteReader();
            sqlCommand.Dispose();
            return sqlDataReader;
        }

        static int makeChange(SQLiteConnection sqlConnection, string updateString)
        {
            SQLiteCommand sqlCommand = sqlConnection.CreateCommand();
            sqlCommand.CommandText = updateString;

            int rowsAffected = sqlCommand.ExecuteNonQuery();
            sqlCommand.Dispose();
            return rowsAffected;
        }
        #endregion
    }
}
