@echo off
rem Build InvincibleMinions with the .NET Framework compiler shipped with Windows.
rem No dotnet SDK / Visual Studio required (verified absent on this machine).
setlocal
set GAME=F:\SteamLibrary\steamapps\common\OxygenNotIncluded
set MANAGED=%GAME%\OxygenNotIncluded_Data\Managed
set CSC=C:\Windows\Microsoft.NET\Framework64\v4.0.30319\csc.exe
set DIST=%~dp0dist

if not exist "%DIST%" mkdir "%DIST%"

"%CSC%" /nologo /target:library /optimize+ /nostdlib+ /out:"%DIST%\InvincibleMinions.dll" ^
 /r:"%MANAGED%\mscorlib.dll" ^
 /r:"%MANAGED%\netstandard.dll" ^
 /r:"%MANAGED%\System.Runtime.dll" ^
 /r:"%MANAGED%\Assembly-CSharp.dll" ^
 /r:"%MANAGED%\Assembly-CSharp-firstpass.dll" ^
 /r:"%MANAGED%\0Harmony.dll" ^
 /r:"%MANAGED%\UnityEngine.CoreModule.dll" ^
 /r:"%MANAGED%\UnityEngine.dll" ^
 src\*.cs

if errorlevel 1 (
  echo BUILD FAILED
  exit /b 1
)
copy /y "%~dp0mod.yaml" "%DIST%\" >nul
copy /y "%~dp0mod_info.yaml" "%DIST%\" >nul
echo BUILD OK -^> %DIST%
