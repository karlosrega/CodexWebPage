param([string]$BaseUrl='http://localhost:5080')
$ErrorActionPreference='Stop'
$taskRoot=Split-Path $PSScriptRoot -Parent
$taskCount=0
function Assert-Http([bool]$Condition,[string]$Description) {
    if (-not $Condition) { throw "HTTP FAIL: $Description" }
    $script:taskCount++
    Write-Host "PASS $Description"
}
function Get-Csrf([string]$Html) {
    $taskMatch=[regex]::Match($Html,'name="__RequestVerificationToken"[^>]*value="([^"]+)"')
    if(-not $taskMatch.Success){throw 'No se encontró la protección antifalsificación.'}
    return [System.Net.WebUtility]::HtmlDecode($taskMatch.Groups[1].Value)
}
$taskPublic=Invoke-WebRequest "$BaseUrl/" -SessionVariable taskHttpSession
Assert-Http ($taskPublic.StatusCode -eq 200 -and $taskPublic.Content.Contains('Bienvenido de nuevo.')) 'Public login page renders'
Assert-Http (([regex]::Matches($taskPublic.Content,'class="promotion ').Count) -eq 5) 'Five promotional sections render'
foreach($taskAsset in @('admit-one-logo.webp','cinema-lobby.jpg','cinema-concessions.jpg','cinema-digital.jpg')) {
    $taskMedia=Invoke-WebRequest "$BaseUrl/Content/images/$taskAsset"
    Assert-Http ($taskMedia.StatusCode -eq 200) "Asset available: $taskAsset"
}
foreach($taskPrivatePath in @('/ConnectionStrings.local.config','/App_Data/errors.log','/Views/Public/Index.cshtml')) {
    $taskPrivate=Invoke-WebRequest "$BaseUrl$taskPrivatePath" -SkipHttpErrorCheck
    Assert-Http ($taskPrivate.StatusCode -eq 404) "Private resource blocked: $taskPrivatePath"
}
$taskRedirect=Invoke-WebRequest "$BaseUrl/Users" -WebSession $taskHttpSession
Assert-Http ($taskRedirect.Content.Contains('Bienvenido de nuevo.')) 'Anonymous administration redirects to login'
$taskMissingCsrf=Invoke-WebRequest "$BaseUrl/Public/Login" -Method Post -Body @{UserName='admin';Password='incorrect'} -WebSession $taskHttpSession -SkipHttpErrorCheck
Assert-Http ($taskMissingCsrf.StatusCode -eq 400) 'POST without CSRF rejected'
$taskPublic=Invoke-WebRequest "$BaseUrl/" -WebSession $taskHttpSession
$taskWrong=Invoke-WebRequest "$BaseUrl/Public/Login" -Method Post -Body @{__RequestVerificationToken=(Get-Csrf $taskPublic.Content);UserName='unknown-http-user';Password='wrong-password'} -WebSession $taskHttpSession
Assert-Http ($taskWrong.Content.Contains('No fue posible acceder.')) 'Incorrect credentials receive generic error'
# The generated administrator secret stays in local memory and is never printed.
$taskSecretLine=Get-Content "$taskRoot\.local\initial-access.txt" | Where-Object {$_ -like 'Contraseña:*'}
$taskSecret=$taskSecretLine.Substring($taskSecretLine.IndexOf(':')+1).Trim()
$taskLogin=Invoke-WebRequest "$BaseUrl/Public/Login" -Method Post -Body @{__RequestVerificationToken=(Get-Csrf $taskWrong.Content);UserName='admin';Password=$taskSecret} -WebSession $taskHttpSession
Assert-Http ($taskLogin.Content.Contains('Hola, Administrador.')) 'Valid login reaches administration home'
foreach($taskRoute in @('/Users','/Users/Edit','/Profiles','/Profiles/Edit','/Account')) {
    $taskPage=Invoke-WebRequest "$BaseUrl$taskRoute" -WebSession $taskHttpSession
    Assert-Http ($taskPage.StatusCode -eq 200 -and -not $taskPage.Content.Contains('No pudimos completar')) "Administrative view renders: $taskRoute"
}
$taskUsers=Invoke-WebRequest "$BaseUrl/Users" -WebSession $taskHttpSession
$taskEditLink=[regex]::Match($taskUsers.Content,'href="(/Users/Edit/\d+)"').Groups[1].Value
$taskEdit=Invoke-WebRequest "$BaseUrl$taskEditLink" -WebSession $taskHttpSession
$taskId=[regex]::Match($taskEdit.Content,'name="Id"[^>]*value="(\d+)"').Groups[1].Value
$taskLastAdmin=Invoke-WebRequest "$BaseUrl/Users/Edit" -Method Post -WebSession $taskHttpSession -Body @{__RequestVerificationToken=(Get-Csrf $taskEdit.Content);Id=$taskId;UserName='admin';DisplayName='Administrador';Email='admin@localhost.invalid';Active='false'}
Assert-Http ($taskLastAdmin.Content.Contains('Debe permanecer al menos un administrador activo.')) 'Last administrator protected through HTTP'
$taskHome=Invoke-WebRequest "$BaseUrl/Home" -WebSession $taskHttpSession
$taskLogout=Invoke-WebRequest "$BaseUrl/Account/Logout" -Method Post -WebSession $taskHttpSession -Body @{__RequestVerificationToken=(Get-Csrf $taskHome.Content)}
Assert-Http ($taskLogout.Content.Contains('Bienvenido de nuevo.')) 'Logout returns to login'
$taskAfterLogout=Invoke-WebRequest "$BaseUrl/Home" -WebSession $taskHttpSession
Assert-Http ($taskAfterLogout.Content.Contains('Bienvenido de nuevo.')) 'Protected home unavailable after logout'
Write-Host "RESULT $taskCount HTTP checks passed; 0 failures."
