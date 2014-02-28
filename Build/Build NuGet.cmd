
@echo off
"%~dp0..\tools\nuget.exe" restore "%~dp0..\Unicorn.sln"
"%~dp0..\tools\nuget.exe" pack "%~dp0..\src\Unicorn\Unicorn.csproj" -Build -Symbols -Properties Configuration=Release
PAUSE