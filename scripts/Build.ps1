param([ValidateSet('Debug','Release')][string]$Configuration='Debug')
$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskVsWhere="${env:ProgramFiles(x86)}\Microsoft Visual Studio\Installer\vswhere.exe"
$taskMsBuild=& $taskVsWhere -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
if (-not $taskMsBuild) { throw 'No se encontró MSBuild de Visual Studio 2022.' }
& $taskMsBuild "$taskRoot\A1IntegrationsAdmin.sln" -restore -p:Configuration=$Configuration -p:RestorePackagesPath="$taskRoot\packages" -verbosity:minimal -nologo
if ($LASTEXITCODE -ne 0) { throw 'La compilación falló. Revisa los errores anteriores.' }
# ASP.NET loads Web.config redirects, not the generated DLL.config. Merge them explicitly.
$taskGenerated="$taskRoot\Web\bin\A1IntegrationsAdmin.Web.dll.config"
if (Test-Path -LiteralPath $taskGenerated) {
    [xml]$taskWeb=Get-Content "$taskRoot\Web\Web.config"
    [xml]$taskBindings=Get-Content $taskGenerated
    if ($taskBindings.configuration.runtime) {
        $taskWeb.configuration.ReplaceChild($taskWeb.ImportNode($taskBindings.configuration.runtime,$true),$taskWeb.configuration.runtime) | Out-Null
        $taskWeb.Save("$taskRoot\Web\Web.config")
    }
}
Write-Host 'Compilación y configuración completadas.'
