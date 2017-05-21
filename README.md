# iOSLib
iOSLib is a .NET library written in C# for communicating with iOS using libimobiledevice. Right now it only supports photo and video management, but it's planned to do much more later (see "State of Development" section for more info).

<b>Note: this library is no longer under active development.</b>

## Releases
Releases can be found under the "Releases" tab or by clicking [here](https://github.com/geiszla/iOSLib/releases).
<br />(Note: You need the required dlls to be in the same folder as the ".exe" to make the program work.)

## Requirements
 - .NET 4 (Client profile)
 - LibiMobileDevice dll and its dependencies (included in the released 7z)
 - System.Data.Sqlite (from NuGet package manager)

Only tested on iOS 8.4 but it should work on every iOS 8.X device. Theoretically it works on older versions too except the Remove function (due to differences in Photos.sqlite database). Also on newer versions as long as they're supported by libimobiledevice library. <b>Doesn't require jailbreak.</b>

## Setting up
1. Download, uncompress project zip file and open solution
2. Due to the fact, that LibiMobileDevice library is only compiled for 32-bit (x86) Windows, you have to <b>change the target CPU architecture</b> of the project. To do it click "Any CPU" at the toolbar (next to "Debug") and select "x86". If it isn't already there, click "Configuration Manager...", then click "Any CPU" at the top of the newly opened window, select "\<New\>", select "x86" instead of "x64", click OK, click Close. (Note, that you have to change this again if you change "Debug" to "Release")
3. Now you can build the dll.

## Changelog
<h4>1.0</h4>
Initial release with photo and video management.

## State of Development
### Done
 - Device detection and connection
 - Photo and video management

### To-Do
 - Music
 - Apps
 - Books
 - Messages
 - Calendar
 - Contacts
 - Call logs
 - Internet
 - Notes
 - Backup and Restore

## Documentation
For more info on the source code and individual functions see wiki pages [here](https://github.com/geiszla/iOSLib/wiki)

## Feedback
Feel free to report all bugs you found by creating a new issue [here](https://github.com/geiszla/iOSLib/issues).

## Credits
Used free open source libraries:
 - libimobiledevice (by Nikias Bassen)
 - System.Data.SQLite .Net library (by mistachkin)
