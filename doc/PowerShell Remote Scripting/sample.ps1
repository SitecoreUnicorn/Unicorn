param([string]$url = 'https://localhost/unicorn.aspx')
$ErrorActionPreference = 'Stop'

$ScriptPath = Split-Path $MyInvocation.MyCommand.Path

# This is an example PowerShell script that will remotely execute a Unicorn sync using the new CHAP authentication system.

Import-Module $ScriptPath\Unicorn.psm1

Sync-Unicorn -ControlPanelUrl $url -SharedSecret 'your-sharedsecret-here' -Configurations @('Test1', 'Test2')

# Note: you may pass -Configurations @(' ') to syncronize all configurations.
# Note: you may pass -Verb 'Reserialize' for remote reserialize. Usually not needed though.