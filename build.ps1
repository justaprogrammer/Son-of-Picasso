function printHeader {
    param (
        $text
    )

    Write-Host "**** $text ****"
}

printHeader "Clean"

dotnet clean .\SonOfPicasso.sln -v m
Remove-Item -Recurse .\reports -ErrorAction Ignore

printHeader "Publish"

dotnet publish -c Release .\SonOfPicasso.sln -v m

printHeader "Test"

function test {
    param (
        $project
    )

    dotnet vstest src\$project\bin\Release\netcoreapp3.0\publish\$project.dll `
        --logger:"trx;LogFileName=$project.trx" `
        --ResultsDirectory:reports `
        --collect:"XPlat code coverage" `
        --settings:"src\coverletArgs.runsettings" | Tee-Object -Variable cmdOutput 
  
    $match = Select-String -Pattern "Attachments:\s+(.*?opencover.xml)" -InputObject $cmdOutput
    Write-Host "Last Exit Code: $LastExitCode"

    $coverageFile = Resolve-Path -Relative $match.Matches[0].Groups[1].Value
    Move-Item  $coverageFile ".\reports\$project.opencover.xml"
}

test "SonOfPicasso.Core.Tests"
test "SonOfPicasso.Data.Tests"
test "SonOfPicasso.Integration.Tests"
test "SonOfPicasso.Tools.Tests"
test "SonOfPicasso.UI.Tests"