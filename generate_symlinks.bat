@echo off

set PROJ_DIR=C:\East\Prog\Unity\LumberSim
set list=bootstrapper;core;menucontroller;networking;playermgmt;scenemgmt

:: Loop through the list and create symlinks
for %%i in (%list%) do (
    echo Creating symlink for %%i...
    mklink /D "%PROJ_DIR%\Assets\net.emullen.%%i" "C:\east\prog\unity\net.emullen.%%i"
    if %ERRORLEVEL% neq 0 (
        echo Failed to create symlink for %%i
    ) else (
        echo Symlink for %%i created successfully
    )
)
pause
