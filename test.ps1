param (
    $project
)

$defaultErrorActionPreference = $ErrorActionPreference
$ErrorActionPreference="silentlycontinue"

$testSuccess = $true

dotnet vstest src\$project\bin\Release\netcoreapp3.0\publish\$project.dll `
	--logger:"trx;LogFileName=$project.trx" `
	--ResultsDirectory:reports `
	--collect:"XPlat code coverage" `
	--settings:"src\coverletArgs.runsettings" | Tee-Object -Variable cmdOutput 
    
$script:testSuccess = $script:testSuccess -and ($LastExitCode -eq 0)
  
$match = Select-String -Pattern "Attachments:\s+(.*?opencover.xml)" -InputObject $cmdOutput

$coverageFile = Resolve-Path -Relative $match.Matches[0].Groups[1].Value
Move-Item  $coverageFile ".\reports\$project.opencover.xml"

$ErrorActionPreference = $defaultErrorActionPreference

if(!$testSuccess) {
    Write-Host "Test Failed"
    exit 1
}