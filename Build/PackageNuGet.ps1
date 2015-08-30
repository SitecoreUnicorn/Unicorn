param($scriptRoot)

$ErrorActionPreference = "Stop"

$msBuild = "$env:WINDIR\Microsoft.NET\Framework\v4.0.30319\msbuild.exe"
$nuGet = "$scriptRoot..\tools\NuGet.exe"
$solution = "$scriptRoot\..\Unicorn.sln"

& $nuGet restore $solution
& $msBuild $solution /p:Configuration=Release /t:Rebuild /m

$UnicornAssembly = Get-Item "$scriptRoot\..\src\Unicorn\bin\Release\Unicorn.dll" | Select-Object -ExpandProperty VersionInfo
$targetAssemblyVersion = $UnicornAssembly.ProductVersion

$rainbowVersion = Read-Host 'Enter Rainbow version to depend on'

& $nuGet pack "$scriptRoot\Unicorn.nuget\Unicorn.nuspec" -version $targetAssemblyVersion -Prop "rainbowversion=$rainbowVersion"

& $nuGet pack "$scriptRoot\..\src\Unicorn\Unicorn.csproj" -Symbols -Prop "Configuration=Release;rainbowversion=$rainbowVersion"