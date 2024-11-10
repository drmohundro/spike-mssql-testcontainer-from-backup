# Test Container with Backup - Sample

This is a proof of concept showing how to use TestContainers, except with a backup (i.e. bak file) in place. This is useful for situations where there isn't a good seed strategy in place or where a seed script would be unwieldy.

## Prerequisites

You'll need a `bak` file first... drop it in the `TestContainerSample.Tests/Backups` directory.

Mine is a backup of a table that looks generally like this:

```sql
CREATE DATABASE SampleDatabase;
       
USE SampleDatabase;

CREATE TABLE Stuff (
    [ID] [int] IDENTITY(1,1) NOT NULL,
    [Name] [varchar](50) NOT NULL
)
```

The general script to create said backup is:

```sql
BACKUP DATABASE [SampleDatabase] TO DISK = N'/var/opt/mssql/backups/SampleDatabase.bak'
WITH NOFORMAT, NOINIT, NAME = 'SampleDatabase-full', SKIP, NOREWIND, NOUNLOAD, STATS = 10
```

Keep in mind that this is just the proof of concept side of things.

## Explanation

The included `Dockerfile` creates a new mssql image, but it copies a `bak` file in and restores it. The final stage copies the entire SQL Server data directory over so that you now have a SQL Server with a database ready to go.

Then, the included `MsSqlBuilderWithBackup` is a slightly modified version of TestContainers.MsSql except it uses the image name of the newly built container instead.

## Resources

- [TestContainers](https://dotnet.testcontainers.org/)
- [StackOverflow Answer]()