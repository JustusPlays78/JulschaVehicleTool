@echo off
setlocal

if "%1"=="" goto build
if "%1"=="build" goto build
if "%1"=="release" goto release
if "%1"=="test" goto test
if "%1"=="run" goto run
if "%1"=="clean" goto clean
if "%1"=="publish" goto publish
goto help

:build
echo [BUILD] Debug...
dotnet build JulschaVehicleTool.sln
goto end

:release
echo [BUILD] Release...
dotnet build JulschaVehicleTool.sln -c Release
goto end

:test
echo [TEST]...
dotnet test JulschaVehicleTool.sln --verbosity normal
goto end

:run
echo [RUN]...
dotnet run --project src\JulschaVehicleTool.App
goto end

:clean
echo [CLEAN]...
dotnet clean JulschaVehicleTool.sln
goto end

:publish
echo [PUBLISH] Single-file EXE...
dotnet publish src\JulschaVehicleTool.App -c Release -r win-x64 --self-contained -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o publish\
echo.
echo Output: publish\JulschaVehicleTool.App.exe
goto end

:help
echo Usage: build.bat [command]
echo.
echo   build     Debug build (default)
echo   release   Release build
echo   test      Run tests
echo   run       Start the app
echo   clean     Clean build output
echo   publish   Publish self-contained exe
goto end

:end
