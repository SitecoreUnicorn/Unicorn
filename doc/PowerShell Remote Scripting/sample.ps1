$ErrorActionPreference = 'Stop'

$ScriptPath = Split-Path $MyInvocation.MyCommand.Path

# This is an example PowerShell script that will remotely execute a Unicorn sync using the new CHAP authentication system.

Import-Module $ScriptPath\Unicorn.psm1

# SYNC ALL CONFIGURATIONS
Sync-Unicorn -ControlPanelUrl 'https://localhost/unicorn.aspx' -SharedSecret 'your-sharedsecret-here' -StreamLogs

# SYNC SPECIFIC CONFIGURATIONS
Sync-Unicorn -ControlPanelUrl 'https://localhost/unicorn.aspx' -SharedSecret 'your-sharedsecret-here' -Configurations @('Test1', 'Test2') -StreamLogs

# SYNC ALL CONFIGURATIONS, SKIPPING ANY WITH TRANSPARENT SYNC ON
Sync-Unicorn -ControlPanelUrl 'https://localhost/unicorn.aspx' -SharedSecret 'your-sharedsecret-here' -SkipTransparentConfigs -StreamLogs

# Note: you may pass -Verb 'Reserialize' for remote reserialize. Usually not needed though.

# Note: If you are having authorization issues, add -DebugSecurity to your cmdlet invocation; this will display the raw signatures being used to compare to the server.