
@echo off
"%~dp0..\tools\nuget.exe" restore "%~dp0..\Unicorn.sln"
"%systemroot%\Microsoft.NET\Framework64\v4.0.30319\msbuild.exe" "%~dp0..\src\Unicorn\Unicorn-SC6.csproj" /p:Configuration=Release /toolsversion:4.0 
"%~dp0..\tools\nuget.exe" pack "%~dp0..\src\Unicorn\Unicorn.csproj" -Build -Symbols -Properties Configuration=Release
PAUSE