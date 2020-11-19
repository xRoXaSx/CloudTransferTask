# CloudTransferTask
## About CloudTransferTask 🎉
CloudTransferTask is a highly configurable C# based wrapper for **rclone**. If you want to maximize your rclone experience you came to the right place! 😉  
With this console appication you can define _tasks_ (also known as jobs) that can be used to start a rclone action with a single parameter.  
Ex. `CloudTransferTask.exe GDriveBackup`

## Requirements & Installation
### Requirements:
- **OS**: Linux, Windows, (OS X - Currently not tested)
- **rclone**: Newest Version
- **Runtime**: .NET Core 3.1
- **Time to set it up** ☕

### Installation:
1. Download the latest version of rclone ([can be found here](https://rclone.org/downloads/)) and set it up
2. Download the latest [release](https://github.com/xRoXaSx/CloudTransferTask/releases) or clone the project
3. If not already installed, install the .NET Core Runtime  
    - Windows: [.NET Core Runtime 3.1](https://dotnet.microsoft.com/download/dotnet-core/3.1) (For more ease "<u>.NET Core Desktop Runtime 3.1.\*</u>" **or** for minimalists "<u>.NET Core Runtime 3.1.\*</u>")
    - Linux: Use your packetmanager to download the latest .NET Core Runtime 3.1 from Microsoft's repo ([MS Docs](https://docs.microsoft.com/de-de/dotnet/core/install/linux))
    - MacOS: Use the corresponding donwload link from [MS Docs](https://docs.microsoft.com/de-de/dotnet/core/install/macos#supported-releases) 
4. Move the downloaded zip into a directory where it doesn't bother you, unzip it and start the application from there.
    1. Open up a terminal window (sh, ksh, bash, CMD, PowerShell, ...)
    2. Start the application via command line:
        - **.NET Core Desktop Runtime**: path\to\CloudTransferTask.exe
        - **.NET Core Runtime**: dotnet path/to/CloudTransferTask.dll
5. Open your file explorer and change into your `ApplicationData` directory: 
    - Windows: `%appdata%` (`C:\Users\%username%\AppData\Roaming`)
    - Linux: `$HOME/.config` (`/home/$USER/.config`)
6. Find the `CloudTransferTask` folder (lower case if you're on a Unix like OS) and open the `config.json` file
7. Modify the json file to your liking.

<br /><br />
***
<br />

## Features
| Feature            | Description | OS | Status |
|--------------------|-------------|----|--------|
| Background service | Windows Service and Linux Deamon which are listening to sourcepath changes | Win/Lin/Mac |⭕|
| Finish OSX tests   | OS X _should_ be supported, is not tested yet!  | Mac |📅|
| Script on fail     | Option for scripts or programs if a rclone action fails | Win/Lin/Mac |📅|
| Script on timeout  | Option for scripts or programs if pre / post action timesout | Win/Lin/Mac |📅|
| Custom filter presets | Option to use customized filter presets instead of copy pasting  `FileType` | Win/Lin/Mac |📅|
| Pre action script  | Option for scripts or programs before the main rclone action | Win/Lin/Mac |✅|
| Post action script | Option for scripts or programs after the main rclone action | Win/Lin/Mac |✅|
| Placeholders | Optional placeholders for `source` and `AdditionalArguments` like date and time | Win/Lin/Mac |✅|
| Custom script parameters | Pass custom parameters to pre and post scripts | Win/Lin/Mac |✅|
| Wildcard extension patterns | Patterns to match specific names | Win/Lin/Mac |✅|
| Custom console color | Changable console color for the rclone action | Win/Lin/Mac |✅|

<br />

📅 => Planned  
✅ => Implemented  
⭕ => Currently being worked on  
❌ => Dropped

<br /><br />
***
<br />

## Used APIs / Libs
###### Libs used in this project:
<br />

**`Newtonsoft.Json`**: [API](https://github.com/JamesNK/Newtonsoft.Json) used for createing and reading the config.<br>

