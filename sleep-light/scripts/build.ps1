param(
    [ValidateSet('auto','zh','en')][string]$Language = 'auto',
    [string]$OutputDirectory
)
$ErrorActionPreference = 'Stop'
$moduleRoot = Split-Path -Parent $PSScriptRoot
if (-not $OutputDirectory) { $OutputDirectory = Join-Path $moduleRoot 'dist' }
$compiler = Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw 'The .NET Framework 4.x x64 C# compiler is required.' }
New-Item -ItemType Directory -Path $OutputDirectory -Force | Out-Null
$sources = @(Join-Path $moduleRoot 'src\SleepPolicy.cs'; Join-Path $moduleRoot 'src\NativeHid.cs'; Join-Path $moduleRoot 'src\Program.cs'; Join-Path $moduleRoot 'config\SleepLightConfig.cs')
$defines = @()
if ($Language -ne 'auto') { $defines = @('/define:UI_' + $Language.ToUpperInvariant()) }
$references = @('/reference:System.Windows.Forms.dll','/reference:System.Drawing.dll','/reference:System.Web.Extensions.dll')
$runner = Join-Path $OutputDirectory 'SleepLightTests.exe'
& $compiler /nologo /target:exe /platform:x64 /main:DS4SleepLight.Tests "/out:$runner" @references @defines @sources (Join-Path $moduleRoot 'tests\SleepLightTests.cs')
if ($LASTEXITCODE -ne 0) { throw "Offline test compilation failed ($LASTEXITCODE)." }
& $runner (Join-Path $OutputDirectory 'test-results.json')
if ($LASTEXITCODE -ne 0) { throw "Offline checks failed ($LASTEXITCODE)." }
$executable = Join-Path $OutputDirectory 'DS4SleepLight.exe'
& $compiler /nologo /target:winexe /platform:x64 /main:DS4SleepLight.Program "/out:$executable" @references @defines @sources
if ($LASTEXITCODE -ne 0) { throw "Application compilation failed ($LASTEXITCODE)." }
$hashes = [ordered]@{}
foreach ($source in $sources) { $hashes[$source.Substring($moduleRoot.Length + 1).Replace('\','/')] = (Get-FileHash -LiteralPath $source -Algorithm SHA256).Hash.ToLowerInvariant() }
[ordered]@{version=1;language=$Language;executableSha256=(Get-FileHash -LiteralPath $executable -Algorithm SHA256).Hash.ToLowerInvariant();sources=$hashes} | ConvertTo-Json -Depth 4 | Set-Content -LiteralPath (Join-Path $OutputDirectory 'build-info.json') -Encoding UTF8
Write-Output "Built: $executable (language=$Language). Build did not start the watcher or send USB commands."
