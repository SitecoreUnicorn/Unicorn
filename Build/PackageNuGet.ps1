param($scriptRoot)

$ErrorActionPreference = "Stop"

function Resolve-MsBuild {
	$msb2017 = Resolve-Path "${env:ProgramFiles(x86)}\Microsoft Visual Studio\*\*\MSBuild\*\bin\msbuild.exe" -ErrorAction SilentlyContinue
	if($msb2017) {
		Write-Host "Found MSBuild 2017 (or later)."
		Write-Host $msb2017
		return $msb2017
	}

	$msBuild2015 = "${env:ProgramFiles(x86)}\MSBuild\14.0\bin\msbuild.exe"

	if(-not (Test-Path $msBuild2015)) {
		throw 'Could not find MSBuild 2015 or later.'
	}

	Write-Host "Found MSBuild 2015."
	Write-Host $msBuild2015

	return $msBuild2015
}

$msBuild = Resolve-MsBuild
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