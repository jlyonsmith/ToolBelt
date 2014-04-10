#!/bin/bash
mono .nuget/NuGet.exe restore -PackagesDirectory packages
exit $?