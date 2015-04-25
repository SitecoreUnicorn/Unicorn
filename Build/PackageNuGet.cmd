@ECHO off

SET scriptRoot=%~dp0

powershell.exe -ExecutionPolicy Unrestricted -NoExit .\PackageNuGet.ps1 %scriptRoot%