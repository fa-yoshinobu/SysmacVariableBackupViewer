@echo off
setlocal
pushd "%~dp0"

echo SysmacVariableBackupViewer build starting...

echo Restoring dependencies...
dotnet restore
if errorlevel 1 goto :error

echo Publishing single-file executable...
dotnet publish -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -o "bin\Release\net8.0-windows\win-x64\publish"
if errorlevel 1 goto :error

echo.
echo Build completed successfully.
echo Output directory: bin\Release\net8.0-windows\win-x64\publish
dir "bin\Release\net8.0-windows\win-x64\publish\*.exe" /-c

popd
exit /b 0

:error
echo.
echo Build failed.
popd
exit /b 1
