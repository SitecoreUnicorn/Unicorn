$ErrorActionPreference = 'Stop'
$ScriptPath = Split-Path $MyInvocation.MyCommand.Path
$MicroCHAP = $ScriptPath + '\MicroCHAP.dll'
Add-Type -Path $MicroCHAP
$global:unicornErrors = @{ }
$global:unicornWarnings = @{ }
[string]$Global:unicornEndOfWork = ""
[string]$Global:unicornLog = ""
[int]$Global:syncedProjectCount
[int]$Global:totalProjectCount

Function Sync-Unicorn {
	Param(
		[Parameter(Mandatory = $True)]
		[string]$ControlPanelUrl,

		[Parameter(Mandatory = $True)]
		[string]$SharedSecret,

		[string[]]$Configurations,

		[string]$Verb = 'Sync',

		[switch]$SkipTransparentConfigs,

		[switch]$DebugSecurity,
		
		# defines, if logs shall be streamed to output
		[switch]$StreamLogs, 
		[int]$SleepTime = 30
	)

	# PARSE THE URL TO REQUEST
	$parsedConfigurations = '' # blank/default = all
	
	if ($Configurations) {
		$parsedConfigurations = ($Configurations) -join "^"
	}

	$skipValue = 0
	if ($SkipTransparentConfigs) {
		$skipValue = 1
	}

	$url = "{0}?verb={1}&configuration={2}&skipTransparentConfigs={3}" -f $ControlPanelUrl, $Verb, $parsedConfigurations, $skipValue 

	if ($DebugSecurity) {
		Write-Host "Sync-Unicorn: Preparing authorization for $url"
	}

	# GET AN AUTH CHALLENGE
	$challenge = Get-Challenge -ControlPanelUrl $ControlPanelUrl

	if ($DebugSecurity) {
		Write-Host "Sync-Unicorn: Received challenge from remote server: $challenge"
	}

	# CREATE A SIGNATURE WITH THE SHARED SECRET AND CHALLENGE
	$signatureService = New-Object MicroCHAP.SignatureService -ArgumentList $SharedSecret

	$signature = $signatureService.CreateSignature($challenge, $url, $null)

	if ($DebugSecurity) {
		Write-Host "Sync-Unicorn: MAC '$($signature.SignatureSource)'"
		Write-Host "Sync-Unicorn: HMAC '$($signature.SignatureHash)'"
		Write-Host "Sync-Unicorn: If you get authorization failures compare the values above to the Sitecore logs."
	}

	Write-Host "Sync-Unicorn: Executing $Verb..."

	# setting default values
	$Global:syncedProjectCount = 0
	$Global:totalProjectCount = 0

	# USING THE SIGNATURE, EXECUTE UNICORN
	[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
	$result = Invoke-StreamingWebRequest -Uri $url -Mac $signature.SignatureHash -Nonce $challenge -RequestVerb $Verb

	while ($result.Trim().ToLowerInvariant() -eq "Sync in progress".ToLowerInvariant()) {
		Write-Host "Sync is still running, sleeping for $SleepTime seconds"
		Start-Sleep $SleepTime
		# renew challenge and signature
		$challenge = Get-Challenge -ControlPanelUrl $ControlPanelUrl
		$signature = $signatureService.CreateSignature($challenge, $url, $null)
		$result = Invoke-StreamingWebRequest -Uri $url -Mac $signature.SignatureHash -Nonce $challenge -RequestVerb $Verb
	}

	if ($result.TrimEnd().EndsWith('****ERROR OCCURRED****')) {
		throw "Unicorn $Verb to $url returned an error. See the preceding log for details."
	}

	# Uncomment this if you want the console results to be returned by the function
	# $result
}

Function Get-Challenge {
	Param(
		[Parameter(Mandatory = $True)]
		[string]$ControlPanelUrl
	)

	$url = "$($ControlPanelUrl)?verb=Challenge"

	[Net.ServicePointManager]::SecurityProtocol = [Net.SecurityProtocolType]::Tls12
	$result = Invoke-WebRequest -Uri $url -TimeoutSec 360 -UseBasicParsing

	$result.Content
}

function Get-ProjectName {
	param (
		[Parameter(Mandatory = $True)]
		[string]$Line,
		[Parameter(Mandatory = $True)]
		[string]$ProjectIndicator
	)
	
	# collecting project name
	$resultingString = $Line.Substring(0, $Line.IndexOf($ProjectIndicator))
	# remove INFO: from project name
	$resultingString = $resultingString.Substring(6)
	$Global:syncedProjectCount = $Global:syncedProjectCount + 1
	return $resultingString
}

function Set-TotalProjectsCount {
	param (
		[Parameter(Mandatory = $True)]
		[string]$Line
	)
	$Global:totalProjectCount = $Line -replace "[^0-9]" , ''
}

Function Invoke-StreamingWebRequest($Uri, $MAC, $Nonce, $RequestVerb) {
	# this is needed to collect publishing data
	$publishingData = new-object -TypeName "System.Text.StringBuilder"
	$publishIndicator = $false
	$projectLineIndicator = ' is being synced with '
	$totalProjectsCountLineIndicator = 'Precaching items in '

	$request = [System.Net.WebRequest]::Create($Uri)
	$request.Headers["X-MC-MAC"] = $MAC
	$request.Headers["X-MC-Nonce"] = $Nonce
	$request.Timeout = 10800000

	$response = $request.GetResponse()
	
	$responseStream = $response.GetResponseStream()
	$responseStreamReader = new-object System.IO.StreamReader $responseStream
	$project = "Init"
	# cleaning up previous errors (even if they where present)
	$global:unicornErrors = @{ }
	$global:unicornWarnings = @{ }
	
	if ($StreamLogs) {
		$responseText = new-object -TypeName "System.Text.StringBuilder"
		while (-not $responseStreamReader.EndOfStream) {
			$line = $responseStreamReader.ReadLine()
			
			if ($line.Contains($totalProjectsCountLineIndicator)) {
				Set-TotalProjectsCount -Line $line
			}

			if ($line.Contains($projectLineIndicator)) {
				$project = Get-ProjectName -Line $line -ProjectIndicator $projectLineIndicator
			}
			if ($line.StartsWith('Error:')) {
				Write-Host $line.Substring(7) -ForegroundColor Red
				$global:unicornErrors[$project] += @($line)
			}
			elseif ($line.StartsWith('Warn:')) {
				$Global:unicornWarnings[$project] += @($line)
				Write-Host $line.Substring(9) -ForegroundColor Yellow
			}
			elseif ($line.StartsWith('Debug:')) {
				Write-Host $line.Substring(7) -ForegroundColor Gray
			}
			elseif ($line.StartsWith('Info:')) {
				Write-Host $line.Substring(6) -ForegroundColor White
			}
			else {
				Write-Host $line -ForegroundColor White
			}
	
			if ($publishIndicator) {
				# appending data for further analysis in script
				[void]$publishingData.AppendLine($line)
			}
			if ($line.Contains('[P] Auto-publishing of synced items is beginning')) {
				# this means that work is done and we are collecting this for further analysis in script
				$publishIndicator = $true
			}
			[void]$responseText.AppendLine($line)
		}
		$resultingData = $responseText.ToString()
	} 
	else {
		$resultingData = $responseStreamReader.ReadToEnd()
		Write-Host $resultingData
		
		foreach ($line in $resultingData.Split([Environment]::NewLine)) {
			if ($line.Contains($totalProjectsCountLineIndicator)) {
				Set-TotalProjectsCount -Line $line
			}

			if ($line.Contains($projectLineIndicator)) {
				$project = Get-ProjectName -Line $line -ProjectIndicator $projectLineIndicator
			}
	
			if ($line.StartsWith('Error:')) {
				$Global:unicornErrors[$project] += @($line)
			}
			elseif ($line.StartsWith('Warn:')) {
				$Global:unicornWarnings[$project] += @($line)
			}
	
			if ($publishIndicator) {
				# appending data for further analysis in script
				[void]$publishingData.AppendLine($line)
			}
			if ($line.Contains('[P] Auto-publishing of synced items is beginning')) {
				# this means that work is done and we are collecting this for further analysis in script
				$publishIndicator = $true
			}
		}
	}
	
	# exposing collected data to host process here
	$Global:unicornEndOfWork = $publishingData.ToString()
	$Global:unicornLog = $resultingData
	$response.Close();
	return $resultingData
}

Export-ModuleMember -Function Sync-Unicorn
