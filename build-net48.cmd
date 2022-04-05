@echo off
pushd %0\..
set ROOTDIR=%CD%

rem 
rem Find MSBUILD
rem
setlocal enableextensions 
for /F "tokens=*" %%F in ('vswhere.exe -latest -requires Microsoft.Component.MSBuild -property installationPath') do set MSBUILD_DIR=%%F
set MSBUILD_DIR="%MSBUILD_DIR%\MSBuild\Current\Bin"
set MSBUILD=%MSBUILD_DIR%\msbuild.exe
if exist %MSBUILD% goto :work

echo MSBUILD.EXE not found!
goto :end

rem
rem Find libraries
rem
if exist _libraries\net48\SpringCard.PCSC.dll goto :work

echo SpringCard libraries not found!
echo Please install them manually in _libraries\net48

:work

@mkdir _output
@mkdir _output\net48
@mkdir _libraries
@mkdir _libraries\net48

cd projects\Beginners\vb
%MSBUILD% /target:Build /property:Configuration=Release
cd %ROOTDIR%

cd projects\Beginners\csharp
%MSBUILD% /target:Build /property:Configuration=Release
cd %ROOTDIR%

cd projects\MemoryCardTool
%MSBUILD% /target:Build /property:Configuration=Release
cd %ROOTDIR%

cd projects\NfcForum\NfcTagTool
%MSBUILD% /target:Build /property:Configuration=Release
cd %ROOTDIR%

cd projects\NfcForum\NfcTagEmul
%MSBUILD% /target:Build /property:Configuration=Release
cd %ROOTDIR%

cd projects\NfcForum\NfcTagBeam
%MSBUILD% /target:Build /property:Configuration=Release
cd %ROOTDIR%

cd projects\NfcPass\PassKitRdr
%MSBUILD% /target:Build /property:Configuration=Release
cd %ROOTDIR%

cd projects\NfcPass\SmartTapRdr
%MSBUILD% /target:Build /property:Configuration=Release
cd %ROOTDIR%

cd projects\NfcPass\SpringPassRdr
%MSBUILD% /target:Build /property:Configuration=Release
cd %ROOTDIR%

cd projects\PcscDiag2
%MSBUILD% /target:Build /property:Configuration=Release
cd %ROOTDIR%

cd projects\PcscScriptor
%MSBUILD% /target:Build /property:Configuration=Release
cd %ROOTDIR%

:end
popd

