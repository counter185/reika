$PathReleaseInfo = "ReencGUI/ReleaseInfo.cs"

$textNow = [System.IO.File]::ReadAllText($PathReleaseInfo)

$textNow = $textNow -replace '"[^"]*"\/\*HINT_VERSION_INFO\*\/', ('"'+ $env:VERSION_NAME +'"')
$textNow = $textNow -replace '"[^"]*"\/\*HINT_GIT_REF\*\/', ('"'+ $env:VERSION_REF +'"')

[System.IO.File]::WriteAllText($PathReleaseInfo, $textNow)