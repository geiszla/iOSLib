# iOSLib
iOSLib is a .NET library written in C# for communicating with iOS using libimobiledevice. Right now it only supports photo and video management, but it's planned to do much more later (see "State of Development" section for more info).

##Releases
Releases can be found under the "Releases" tab or by clicking [here](https://github.com/geiszla/iOSLib/releases).

(Note: You need the required dlls to be in the same folder as the ".exe" to make the program work.)

##Requirements
 - .NET 4 (Client profile).
 - LibiMobileDevice dll and its dependencies (included in the released 7z).

<br />Only tested on iOS 8.4 but it should work on every iOS 8.X device. Theoretically it works on older versions too except the Remove function (due to differences in Photos.sqlite database). Also on newer versions as long as they're supported by libimobiledevice library. <b>Doesn't require jailbreak.</b>


##Change log
<h4>1.0</h4>
Initial release with photo and video management.

##State of Development
###Done
 - Device detection and connection
 - Photo and video management

###To-Do
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