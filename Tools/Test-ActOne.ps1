param([string]$UnityData = 'C:\Program Files\Unity\Hub\Editor\6000.3.6f1\Editor\Data')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
Push-Location $projectRoot
try {
    New-Item -ItemType Directory -Path Logs -Force | Out-Null
    $mono = Join-Path $UnityData 'MonoBleedingEdge\bin\mono.exe'
    $compiler = Join-Path $UnityData 'MonoBleedingEdge\lib\mono\4.5\mcs.exe'
    $jsonLibrary = Get-ChildItem Library/PackageCache/com.unity.nuget.newtonsoft-json*/Runtime/Newtonsoft.Json.dll | Select-Object -First 1
    if (!$jsonLibrary) { throw 'Open the project in Unity to restore Newtonsoft.Json first.' }
    $references = @(('-r:' + $jsonLibrary.FullName), ('-r:' + (Join-Path $UnityData 'MonoBleedingEdge\lib\mono\4.5\Facades\netstandard.dll')))
    & $mono $compiler -out:Logs/ActOneVerification.exe $references Assets/Scripts/Data/GameState.cs Assets/Scripts/Data/ActOneStory.cs Assets/Scripts/Data/SaveData.cs Assets/Scripts/SceneDirective.cs Assets/Scripts/SaveSystem.cs Tools/Tests/ActOneVerification.cs
    if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed.' }
    $previousMonoPath = $env:MONO_PATH
    try {
        $env:MONO_PATH = $jsonLibrary.DirectoryName
        & $mono Logs/ActOneVerification.exe
        if ($LASTEXITCODE -ne 0) { throw 'Act One verification failed.' }
    } finally { $env:MONO_PATH = $previousMonoPath }
} finally { Pop-Location }
