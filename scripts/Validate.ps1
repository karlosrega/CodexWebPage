$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
& "$taskRoot\Tests\bin\Debug\net48\A1IntegrationsAdmin.Tests.exe" "$taskRoot\Database"
if ($LASTEXITCODE -ne 0) { throw 'Fallaron las pruebas. Revisa el detalle anterior.' }
