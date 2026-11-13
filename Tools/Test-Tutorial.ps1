param([string]$UnityData = 'C:\Program Files\Unity\Hub\Editor\6000.3.6f1\Editor\Data')
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path $PSScriptRoot -Parent
Push-Location $projectRoot
try {
    New-Item -ItemType Directory -Path Logs -Force | Out-Null
    $mono = Join-Path $UnityData 'MonoBleedingEdge\bin\mono.exe'
    $compiler = Join-Path $UnityData 'MonoBleedingEdge\lib\mono\4.5\mcs.exe'
    $jsonLibrary = Get-ChildItem Library/PackageCache/com.unity.nuget.newtonsoft-json*/Runtime/Newtonsoft.Json.dll | Select-Object -First 1
    $nunitLibrary = Get-ChildItem Library/PackageCache/com.unity.ext.nunit*/net40/unity-custom/nunit.framework.dll | Select-Object -First 1
    if (!$jsonLibrary -or !$nunitLibrary) { throw 'Open the project in Unity to restore packages first.' }
    $references = @(('-r:' + $jsonLibrary.FullName), ('-r:' + $nunitLibrary.FullName), ('-r:' + (Join-Path $UnityData 'MonoBleedingEdge\lib\mono\4.5\Facades\netstandard.dll')))
    $coreSources = Get-ChildItem Assets/Scripts/TutorialCore -Filter *.cs | Select-Object -ExpandProperty FullName
    & $mono $compiler -out:Logs/TutorialTests.exe $references $coreSources Assets/Tests/Editor/TutorialTests.cs Tools/Tests/TutorialTestRunner.cs
    if ($LASTEXITCODE -ne 0) { throw 'Tutorial test compilation failed.' }
    $previousMonoPath = $env:MONO_PATH
    try {
        $env:MONO_PATH = $jsonLibrary.DirectoryName + ';' + $nunitLibrary.DirectoryName
        & $mono Logs/TutorialTests.exe
        if ($LASTEXITCODE -ne 0) { throw 'Tutorial tests failed.' }
    } finally { $env:MONO_PATH = $previousMonoPath }
} finally { Pop-Location }
