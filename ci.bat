@echo off
setlocal

cd /d "%~dp0"

echo [1/5] restore...
dotnet restore SysmacXmlViewer.sln
if errorlevel 1 goto :error

echo [2/5] format check...
dotnet format whitespace SysmacXmlViewer.sln --verify-no-changes --no-restore
if errorlevel 1 goto :error

echo [3/5] build...
dotnet build SysmacXmlViewer.sln -c Release --no-restore /warnaserror -p:EnableNETAnalyzers=true -p:AnalysisMode=Minimum -p:AnalysisLevel=latest -p:WarningsNotAsErrors=CA1822%%3BCA1845%%3BCA1847%%3BCA1854%%3BCA1861%%3BCA1862%%3BCA1866%%3BCA1872
if errorlevel 1 goto :error

echo [4/5] test...
dotnet test SysmacXmlViewer.sln -c Release --no-build --verbosity normal
if errorlevel 1 goto :error

echo [5/5] publish smoke test...
call build.bat win-x64 artifacts\ci
if errorlevel 1 goto :error
if not exist artifacts\ci\SysmacVariableBackupViewer.exe goto :error

echo CI checks passed.
exit /b 0

:error
echo CI checks failed.
exit /b 1
