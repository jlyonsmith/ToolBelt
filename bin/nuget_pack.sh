#!/bin/bash
rm *.nupkg
mono ~/lib/NuGet/NuGet.exe pack ToolBelt.nuspec
mono ~/lib/NuGet/NuGet.exe pack ServiceBelt.nuspec
mono ~/lib/NuGet/NuGet.exe pack AppBelt.nuspec
