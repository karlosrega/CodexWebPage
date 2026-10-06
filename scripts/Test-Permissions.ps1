param([string]$BaseUrl='http://localhost:5081')
$ErrorActionPreference='Stop'
$taskCount=0
function Assert-Http([bool]$Condition,[string]$Description) {
    if (-not $Condition) { throw "HTTP FAIL: $Description" }
    $script:taskCount++
    Write-Host "PASS $Description"
}
function Get-Csrf([string]$Html) {
    return [regex]::Match($Html,'name="__RequestVerificationToken"[^>]*value="([^"]+)"').Groups[1].Value
}
$taskPage=Invoke-WebRequest "$BaseUrl/" -SessionVariable taskSession
$taskHome=Invoke-WebRequest "$BaseUrl/Public/Login" -Method Post -WebSession $taskSession -Body @{__RequestVerificationToken=(Get-Csrf $taskPage.Content);UserName='reader';Password='New-test-password-2026!'}
Assert-Http ($taskHome.Content.Contains('Hola, Reader.')) 'Read-only account login'
$taskUsers=Invoke-WebRequest "$BaseUrl/Users" -WebSession $taskSession
Assert-Http ($taskUsers.StatusCode -eq 200 -and -not $taskUsers.Content.Contains('Nuevo usuario +')) 'Read-only account lists users without write controls'
foreach($taskPath in @('/Users/Edit','/Profiles/Edit')) {
    $taskDenied=Invoke-WebRequest "$BaseUrl$taskPath" -WebSession $taskSession -SkipHttpErrorCheck
    Assert-Http ($taskDenied.StatusCode -eq 403 -and $taskDenied.Content.Contains('Tu perfil no permite')) "Direct write page denied: $taskPath"
}
$taskPost=Invoke-WebRequest "$BaseUrl/Users/Edit" -Method Post -WebSession $taskSession -SkipHttpErrorCheck -Body @{__RequestVerificationToken=(Get-Csrf $taskHome.Content);Id='1';UserName='reader';DisplayName='Unauthorized';Email='reader@example.invalid';Active='true'}
Assert-Http ($taskPost.StatusCode -eq 403) 'Direct unauthorized POST denied'
Write-Host "RESULT $taskCount permission HTTP checks passed; 0 failures."
