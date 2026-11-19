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
## Working Time Rules
Work and active travel count as working time, passive travel is recorded separately.

Since 01.12.2026 the following rules apply (earlier days are calculated as before with 8 target hours):
- Work and active travel count up to 10 h a day, the rest is not counted
- Work and active travel get a surcharge of 25% on Saturday and 100% on Sunday
- Passive travel on a day without work counts up to the daily target hours
- Passive travel on a day with work counts fully up to 6 h, the rest by half

Time that is not counted is shown in the column *Loss* of the daily overview.
The rules are defined as rule sets with a start date in `WorkingTimeRules.CreateDefault`, a rule change only needs a further rule set.

The daily target hours (default 8) can be set in ``TimeTrackerSettings.json`` in the application data folder:
```json
{
  "DailyTargetHours": 7.5
}
```
