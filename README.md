# TimeTrackerDotnet

## Installation
- Have .net 10 SDK installed
- Clone the repository
- run ``dotnet publish TimeTracker.sln``

The binary is the build into ``bin\Release\net10.0``

## First Time Usage
On first start an empty ``TimeTracker.csv`` is created in the application data folder
(``%APPDATA%\TimeTracker`` on Windows, ``~/.config/TimeTracker`` on Linux) and loaded automatically.
If the last used file is missing, it is recreated empty at its old location, as long as its folder still exists.

To use an existing file instead, load it with the browse button. Each line looks like this:
```
03.09.2026;08:30;13:00;;Your first entry;
```

The csv file does not have a header.