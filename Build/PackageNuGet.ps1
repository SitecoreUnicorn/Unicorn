param($scriptRoot)

$ErrorActionPreference = "Stop"

$programFilesx86 = ${Env:ProgramFiles(x86)}
$msBuild = "$programFilesx86\MSBuild\14.0\bin\msbuild.exe"
$nuGet = "$scriptRoot..\tools\NuGet.exe"
$solution = "$scriptRoot\..\Unicorn.sln"

& $nuGet restore $solution
& $msBuild $solution /p:Configuration=Release /t:Rebuild /m

$UnicornAssembly = Get-Item "$scriptRoot\..\src\Unicorn\bin\Release\Unicorn.dll" | Select-Object -ExpandProperty VersionInfo
$targetAssemblyVersion = $UnicornAssembly.ProductVersion

$rainbowVersion = Read-Host 'Enter Rainbow version to depend on'

& $nuGet pack "$scriptRoot\Unicorn.nuget\Unicorn.nuspec" -version $targetAssemblyVersion -Prop "rainbowversion=$rainbowVersion"

& $nuGet pack "$scriptRoot\..\src\Unicorn\Unicorn.csproj" -Symbols -Prop "Configuration=Release;rainbowversion=$rainbowVersion"

& $nuGet pack "$scriptRoot\Unicorn.Roles.nuget\Unicorn.Roles.nuspec" -version $targetAssemblyVersion -Prop "rainbowversion=$rainbowVersion"

& $nuGet pack "$scriptRoot\..\src\Unicorn.Roles\Unicorn.Roles.csproj" -Symbols -Prop "Configuration=Release;rainbowversion=$rainbowVersion"

& $nuGet pack "$scriptRoot\Unicorn.Users.nuget\Unicorn.Users.nuspec" -version $targetAssemblyVersion -Prop "rainbowversion=$rainbowVersion"

& $nuGet pack "$scriptRoot\..\src\Unicorn.Users\Unicorn.Users.csproj" -Symbols -Prop "Configuration=Release;rainbowversion=$rainbowVersion"