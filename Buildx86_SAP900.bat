
set BaseDir="C:\VisualK\Proyectos\Addon_Bebidas"
set BinDir="C:\VisualK\Proyectos\Addon_Bebidas\Bin\x86\Debug"
set BinFile="C:\VisualK\Proyectos\Addon_Bebidas\Bin\x86\Debug\Bebidas.exe"
set Version=1.001.8
call "%VS110COMNTOOLS%vsvars32.bat"

msbuild Bebidas.csproj /t:Clean,Build /p:Configuration=Debug;Platform=x86
set BUILD_STATUS=%ERRORLEVEL%
if %BUILD_STATUS%==0 GOTO Reactor
pause
EXIT

:Reactor
"C:\Program Files (x86)\Eziriz\.NET Reactor\dotNET_Reactor.exe" -project %BinDir%\Bebidas.nrproj -targetfile %BinFile%
set REACTOR_STATUS=%ERRORLEVEL%
if %REACTOR_STATUS%==0 GOTO INNO
pause
EXIT

:INNO
"C:\Program Files (x86)\Inno Setup 5\iscc.exe" "%BaseDir%\Bebidas.iss"
set INNO_STATUS=%ERRORLEVEL%
if %INNO_STATUS%==0 GOTO ARD
pause
EXIT

:ARD 
"C:\Program Files (x86)\SAP\SAP Business One SDK\Tools\AddOnRegDataGen\AddOnRegDataGen.exe" "C:\VisualK\Proyectos\Addon_Bebidas\OutPut\BebidasSAP900x86.xml" %Version% "C:\VisualK\Proyectos\Addon_Bebidas\OutPut\setup.exe" "C:\VisualK\Proyectos\Addon_Bebidas\OutPut\setup.exe" %BinFile%
ECHO %ERRORLEVEL%
pause