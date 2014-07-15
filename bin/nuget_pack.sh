#!/bin/bash
mono ~/lib/NuGet/NuGet.exe pack ToolBelt.nuspec
mono ~/lib/NuGet/NuGet.exe pack ToolBelt.MongoDB.nuspec
mono ~/lib/NuGet/NuGet.exe pack ToolBelt.ServiceStack.nuspec
mono ~/lib/NuGet/NuGet.exe pack ToolBelt.ServiceStack.Pcl.nuspec
