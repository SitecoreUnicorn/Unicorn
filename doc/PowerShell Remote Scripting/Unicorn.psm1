$ErrorActionPreference = 'Stop'
$ScriptPath = Split-Path $MyInvocation.MyCommand.Path
$MicroCHAP = $ScriptPath + '\MicroCHAP.dll'
Add-Type -Path $MicroCHAP

Function Sync-Unicorn {
	Param(
		[Parameter(Mandatory=$True)]
		[string]$ControlPanelUrl,

		[Parameter(Mandatory=$True)]
		[string]$SharedSecret,

		[string[]]$Configurations,

		[string]$Verb = 'Sync',

		[switch]$NoDebug
	)

	# PARSE THE URL TO REQUEST
	$parsedConfigurations = '' # blank/default = all
	
	if($Configurations) {
		$parsedConfigurations = ($Configurations) -join "^"
	}

	$url = "{0}?verb={1}&configuration={2}" -f $ControlPanelUrl, $Verb, $parsedConfigurations

	Write-Host "Sync-Unicorn: Preparing authorization for $url"

	# GET AN AUTH CHALLENGE
	$challenge = Get-Challenge -ControlPanelUrl $ControlPanelUrl

	Write-Host "Sync-Unicorn: Received challenge from remote server: $challenge"

	# CREATE A SIGNATURE WITH THE SHARED SECRET AND CHALLENGE
	$signatureService = New-Object MicroCHAP.SignatureService -ArgumentList $SharedSecret

	$signature = $signatureService.CreateSignature($challenge, $url, $null)

	if(-not $NoDebug) {
		Write-Host "Sync-Unicorn: MAC '$($signature.SignatureSource)'"
		Write-Host "Sync-Unicorn: HMAC '$($signature.SignatureHash)'"
		Write-Host "Sync-Unicorn: If you get authorization failures compare the values above to the Sitecore logs."
	}

	Write-Host "Sync-Unicorn: Executing $Verb..."

	# USING THE SIGNATURE, EXECUTE UNICORN
	[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
	$result = Invoke-WebRequest -Uri $url -Headers @{ "X-MC-MAC" = $signature.SignatureHash; "X-MC-Nonce" = $challenge } -TimeoutSec 10800 -UseBasicParsing

	$result.Content
}

Function Get-Challenge {
	Param(
		[Parameter(Mandatory=$True)]
		[string]$ControlPanelUrl
	)

	$url = "$($ControlPanelUrl)?verb=Challenge"

	[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
	$result = Invoke-WebRequest -Uri $url -TimeoutSec 360 -UseBasicParsing

	$result.Content
}

Export-ModuleMember -Function Sync-Unicorn