param(
    [Parameter()]
    [string]$Version = "2.3.0"
)

$ErrorActionPreference = "Stop"

$repoRoot = Resolve-Path (Join-Path $PSScriptRoot "..")
$solution = Join-Path $repoRoot "V2rayN.FlowLens.sln"
$appProject = Join-Path $repoRoot "V2rayN.FlowLens.App\V2rayN.FlowLens.App.csproj"
$artifactsRoot = Join-Path $repoRoot "artifacts"
$packageName = "V2rayN.FlowLens-$Version-win-x64"
$publishDir = Join-Path $artifactsRoot $packageName
$zipPath = Join-Path $artifactsRoot "$packageName.zip"

if (Test-Path $publishDir) {
    Remove-Item -LiteralPath $publishDir -Recurse -Force
}

if (Test-Path $zipPath) {
    Remove-Item -LiteralPath $zipPath -Force
}

New-Item -ItemType Directory -Path $artifactsRoot -Force | Out-Null

dotnet build $solution --no-restore
dotnet test $solution --no-restore
dotnet publish $appProject -c Release -r win-x64 --self-contained true -p:Version=$Version -p:AssemblyVersion=$Version.0 -p:FileVersion=$Version.0 -o $publishDir

Compress-Archive -Path (Join-Path $publishDir "*") -DestinationPath $zipPath -Force

Write-Host "Release folder: $publishDir"
Write-Host "Release zip:    $zipPath"
