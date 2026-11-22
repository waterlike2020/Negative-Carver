$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding $false

function Escape-JsonText([string]$Value) {
    if ([string]::IsNullOrEmpty($Value)) { return "" }
    return (($Value -replace "\\", "\\\\") -replace '"', '\"' -replace "`r", "\r" -replace "`n", "\n")
}

function Write-HookJson([string]$Json) {
    [Console]::Out.WriteLine($Json)
}

try {
    $raw = [Console]::In.ReadToEnd()
    $status = "completed"
    $loopCount = 0
    if (-not [string]::IsNullOrWhiteSpace($raw)) {
        $parsed = $raw | ConvertFrom-Json
        if ($null -ne $parsed.status) { $status = [string]$parsed.status }
        if ($null -ne $parsed.loop_count) { $loopCount = [int]$parsed.loop_count }
    }

    if ($status -ne "completed" -or $loopCount -ge 1) {
        Write-HookJson "{}"
        exit 0
    }

    $files = @()
    foreach ($root in @("src", "tests")) {
        if (Test-Path -LiteralPath $root) {
            $files += @(Get-ChildItem -LiteralPath $root -Filter *.cs -Recurse -File -ErrorAction SilentlyContinue)
        }
    }

    if ($files.Count -eq 0) {
        Write-HookJson "{}"
        exit 0
    }

    $newest = ($files | Sort-Object LastWriteTimeUtc -Descending | Select-Object -First 1).LastWriteTimeUtc
    $stampPath = ".harness/last-verify"
    if (Test-Path -LiteralPath $stampPath) {
        $stampTime = (Get-Item -LiteralPath $stampPath).LastWriteTimeUtc
        if ($stampTime -ge $newest) {
            Write-HookJson "{}"
            exit 0
        }
    }

    $message = "C# 파일이 마지막 성공 검증보다 새롭다. powershell.exe -NoProfile -ExecutionPolicy Bypass -File scripts/verify.ps1 을 실행하라. 실패하면 같은 할 일 칸 안에서만 고치고 다시 실행하라. 통과하기 전에는 to-do-list.md 를 완료로 바꾸지 마라."
    Write-HookJson ('{"followup_message":"' + (Escape-JsonText $message) + '"}')
    exit 0
}
catch {
    Write-HookJson "{}"
    exit 0
}
