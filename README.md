### ToolBelt Utility Library

This is my collection of .NET utility classes which works on both Microsoft and Mono implementations of .NET. Some of the highlights include:

- `Command` class for easily running external processes and capturing output
- `ConsoleUtility` including standard formatting of messages, warnings and errors
- `CommandLineParser` powerful yet easy command line parsing using attributes
- `DirectoryUtility` for power file and directory searching.
- Big & little endian binary readers/writers
- Memory buffer management
- File/directory parsing using the `ParsedPath` class
- `ParsedPathList` for lists of paths
- `StringUtility`, including word wrapping text and tag substition
- `ProcessCycleStopwatch` (Windows only) for CPU cycle timings

Plus a host of other useful classes.

### ServiceBelt Utility Library

A host of classes for use with:

- ToolBelt
- [ServiceStack](https://github.com/ServiceStack/ServiceStack)
- [MongoDB C# Driver](https://github.com/mongodb/mongo-csharp-driver)
- [REST Query Language](https://github.com/jlyonsmith/Rql)

### AppBelt 

PCL classes for connecting to RESTful services, in particular those written with ServiceBelt.

[![Build Status](https://travis-ci.org/jlyonsmith/ToolBelt.svg?branch=master)](https://travis-ci.org/jlyonsmith/ToolBelt)
