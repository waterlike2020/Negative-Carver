$ErrorActionPreference = "Stop"
$env:DOTNET_NOLOGO = "1"
$env:DOTNET_SKIP_FIRST_TIME_EXPERIENCE = "1"

$dotnet = "dotnet"
$listed = & $dotnet --list-sdks 2>&1
if ($LASTEXITCODE -ne 0 -or [string]::IsNullOrWhiteSpace(($listed | Out-String))) {
    Write-Error "NET SDK가 없습니다. Microsoft.DotNet.SDK.8 이상을 설치한 뒤 이 명령을 다시 실행하십시오."
    exit 1
}

& $dotnet test NegativeCarver.sln --nologo --verbosity minimal
if ($LASTEXITCODE -ne 0) {
    exit $LASTEXITCODE
}

$dll = "src\NegativeCarver.Core\bin\Debug\netstandard2.1\NegativeCarver.Core.dll"
if (-not (Test-Path -LiteralPath $dll)) {
    Write-Error "Core DLL was not built: $dll"
    exit 1
}

New-Item -ItemType Directory -Force -Path "Assets\Plugins" | Out-Null
Copy-Item -LiteralPath $dll -Destination "Assets\Plugins\NegativeCarver.Core.dll" -Force

New-Item -ItemType Directory -Force -Path .harness | Out-Null
$stamp = (Get-Date).ToUniversalTime().ToString("o")
Set-Content -LiteralPath ".harness/last-verify" -Value $stamp -Encoding ascii
Write-Output "verify passed $stamp"
exit 0
