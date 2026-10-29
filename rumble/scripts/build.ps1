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
$icon = Join-Path $moduleRoot 'assets\app.ico'
$symbols = @()
if ($Preset -ne 'default') { $symbols += 'PRESET_' + $Preset.ToUpperInvariant() }
if ($Language -ne 'auto') { $symbols += 'UI_' + $Language.ToUpperInvariant() }
$defineArgument = @()
if ($symbols.Count) { $defineArgument = @('/define:' + ($symbols -join ',')) }
# Validate all presets and run simulated tests before producing the user application.
& (Join-Path $PSScriptRoot 'test.ps1') -Preset $Preset -Language $Language -OutputDirectory $OutputDirectory
$executable = Join-Path $OutputDirectory 'LoLDamageRumble.exe'
& $compiler /nologo /target:winexe /platform:x64 "/out:$executable" "/win32icon:$icon" /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Web.Extensions.dll @defineArgument $source $config
if ($LASTEXITCODE -ne 0) { throw "C# build failed ($LASTEXITCODE)." }
$manifest = [ordered]@{
    preset = $Preset
    language = $Language
    executableSha256 = (Get-FileHash -LiteralPath $executable -Algorithm SHA256).Hash.ToLowerInvariant()
    sourceSha256 = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash.ToLowerInvariant()
    configurationSha256 = (Get-FileHash -LiteralPath $config -Algorithm SHA256).Hash.ToLowerInvariant()
    iconSha256 = (Get-FileHash -LiteralPath $icon -Algorithm SHA256).Hash.ToLowerInvariant()
}
$manifest | ConvertTo-Json | Set-Content -LiteralPath (Join-Path $OutputDirectory 'build-info.json') -Encoding UTF8
Write-Output "Built: $executable (preset=$Preset, language=$Language)"
