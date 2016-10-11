$ErrorActionPreference = 'Stop'

$ScriptPath = Split-Path $MyInvocation.MyCommand.Path

# This is an example PowerShell script that will remotely execute a Unicorn sync using the new CHAP authentication system.

Import-Module $ScriptPath\Unicorn.psm1

# SYNC ALL CONFIGURATIONS
Sync-Unicorn -ControlPanelUrl 'https://localhost/unicorn.aspx' -SharedSecret 'your-sharedsecret-here'

# SYNC SPECIFIC CONFIGURATIONS
Sync-Unicorn -ControlPanelUrl 'https://localhost/unicorn.aspx' -SharedSecret 'your-sharedsecret-here' -Configurations @('Test1', 'Test2')

# Note: you may pass -Verb 'Reserialize' for remote reserialize. Usually not needed though.

# Note: the default configuration will write out signature debug data to the console which includes the shared secret
# If you wish to disable this, pass -NoDebug to Sync-Unicorn.