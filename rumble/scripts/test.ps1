param(
    [ValidateSet('default','gentle','focus','celebration')][string]$Preset = 'default',
    [ValidateSet('auto','zh','en')][string]$Language = 'auto',
    [string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
$moduleRoot = Split-Path -Parent $PSScriptRoot
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $moduleRoot 'dist' }
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) {
    throw 'The .NET Framework 4.x x64 C# compiler is required.'
}
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$source = Join-Path $moduleRoot 'src\LoLDamageRumble.cs'
$config = Join-Path $moduleRoot 'config\FeedbackConfig.cs'
$tests = Join-Path $moduleRoot 'tests\LoLRumbleTests.cs'
$regression = Join-Path $moduleRoot 'tests\RegressionSettings.cs'
$runner = Join-Path $OutputDirectory 'LoLRumbleTests.exe'
$report = Join-Path $OutputDirectory 'test-results.json'
$fixture = Join-Path $moduleRoot 'tests\fixtures\live-client-example.json'
$symbols = @()
if ($Preset -ne 'default') { $symbols += 'PRESET_' + $Preset.ToUpperInvariant() }
if ($Language -ne 'auto') { $symbols += 'UI_' + $Language.ToUpperInvariant() }
$defineArgument = @()
if ($symbols.Count) { $defineArgument = @('/define:' + ($symbols -join ',')) }
& $compiler /nologo /target:exe /platform:x64 /main:LoLDamageRumble.Tests "/out:$runner" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll @defineArgument $source $config $tests $regression
if ($LASTEXITCODE -ne 0) { throw "Test compilation failed ($LASTEXITCODE)." }
& $runner $report $fixture $Preset $Language
if ($LASTEXITCODE -ne 0) { throw "Behavior tests failed ($LASTEXITCODE)." }
