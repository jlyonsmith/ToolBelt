@echo off
setlocal
call SetSlnRoot.bat
set CONFIGURATION=Debug
set TESTDIR=%SOLUTIONROOT%\TestDeployment
md %TESTDIR%
toast.exe %SOLUTIONROOT%\ToolBelt\Tests\bin\%CONFIGURATION%\ToolBelt.Tests.dll -di:%SOLUTIONROOT%\ToolBelt\bin\%CONFIGURATION%\ToolBelt.dll -dd:%TESTDIR% -o:%TESTDIR%\TestResults.testresults %*
endlocal