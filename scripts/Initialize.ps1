param([switch]$CreateAdmin)
$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
[xml]$taskLocal=Get-Content "$taskRoot\Web\ConnectionStrings.local.config"
$env:A1ADMIN_CONNECTION_STRING=$taskLocal.connectionStrings.add.connectionString
try {
    & "$taskRoot\Tools\bin\Debug\net48\A1IntegrationsAdmin.Tools.exe" migrate "$taskRoot\Database"
    if ($LASTEXITCODE -ne 0) { throw 'No se pudo inicializar el esquema.' }
    if ($CreateAdmin) {
        Push-Location $taskRoot
        try {
            & "$taskRoot\Tools\bin\Debug\net48\A1IntegrationsAdmin.Tools.exe" create-admin
            if ($LASTEXITCODE -ne 0) { throw 'No se pudo crear el administrador.' }
        } finally { Pop-Location }
    }
} finally { Remove-Item Env:\A1ADMIN_CONNECTION_STRING -ErrorAction SilentlyContinue }
