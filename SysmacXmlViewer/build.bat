@echo off
echo SysmacVariableBackupViewer Build Starting...

REM Clean build
echo Running clean build...
dotnet clean -c Release

REM Restore dependencies
echo Restoring dependencies...
dotnet restore

REM Release build
echo Running release build...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o "bin\Release\net6.0-windows\win-x64\publish"

REM Check result
echo.
echo Build completed!
echo Output directory: bin\Release\net6.0-windows\win-x64\publish
echo.
echo Checking file size...
dir "bin\Release\net6.0-windows\win-x64\publish\*.exe" /-c

REM Check executable
set "EXE_PATH=bin\Release\net6.0-windows\win-x64\publish\SysmacVariableBackupViewer.exe"
if exist "%EXE_PATH%" (
    echo.
    echo Executable found: %EXE_PATH%
    echo.
    echo Do you want to launch the application? (Y/N)
    set /p "choice="
    if /i "%choice%"=="Y" (
        echo.
        echo Launching application...
        start "" "%EXE_PATH%"
        echo Application launched.
    ) else (
        echo.
        echo Application launch skipped.
    )
) else (
    echo.
    echo Warning: Executable not found.
    echo Path: %EXE_PATH%
    echo Please check if the build completed successfully.
)

echo.
echo Build completed successfully.
pause
