$ErrorActionPreference = "Stop"
[Console]::OutputEncoding = New-Object System.Text.UTF8Encoding $false

function Escape-JsonText([string]$Value) {
    if ([string]::IsNullOrEmpty($Value)) { return "" }
    return (($Value -replace "\\", "\\\\") -replace '"', '\"' -replace "`r", "\r" -replace "`n", "\n")
}

function Write-Decision([string]$Permission, [string]$UserMessage, [string]$AgentMessage) {
    $json = '{"permission":"' + $Permission + '"'
    if (-not [string]::IsNullOrEmpty($UserMessage)) {
        $json += ',"user_message":"' + (Escape-JsonText $UserMessage) + '"'
    }
    if (-not [string]::IsNullOrEmpty($AgentMessage)) {
        $json += ',"agent_message":"' + (Escape-JsonText $AgentMessage) + '"'
    }
    $json += "}"
    [Console]::Out.WriteLine($json)
}

try {
    $raw = [Console]::In.ReadToEnd()
    $command = ""
    if (-not [string]::IsNullOrWhiteSpace($raw)) {
        $parsed = $raw | ConvertFrom-Json
        if ($null -ne $parsed.command) { $command = [string]$parsed.command }
    }

    if ([string]::IsNullOrWhiteSpace($command)) {
        Write-Decision "allow" "" ""
        exit 0
    }

    if ($command -match '(?<![\w.-])npm(\.cmd|\.exe)?(?![\w.-])') {
        Write-Decision "deny" "npm은 막았습니다. pnpm을 쓰십시오." "npm 대신 pnpm으로 바꿔 다시 실행하라."
        exit 0
    }

    if ($command -match '(?i)\bgit\s+reset\b.*--hard\b' -or $command -match '(?i)\bgit\s+clean\b') {
        Write-Decision "deny" "되돌릴 수 없는 git 정리는 막았습니다." "git reset --hard 와 git clean은 사용자가 이번 요청에 명시하기 전에는 실행하지 마라."
        exit 0
    }

    if ($command -match '(?i)\bgit\s+push\b' -and $command -match '(?i)(^|\s)(--force\b|-f\b|--force-with-lease\b)') {
        Write-Decision "ask" "강제 푸시입니다. 실행 전에 확인이 필요합니다." "force push는 사용자 승인 없이 진행하지 마라."
        exit 0
    }

    if ($command -match '(?i)(^|[;&|]\s*|^\s*|.*\s)(.*[\\/])?Unity(Hub)?\.exe\b') {
        Write-Decision "ask" "Unity 에디터 실행은 확인이 필요합니다." "에디터를 대신 켜지 말고, 재생 절차를 사용자에게 전하라."
        exit 0
    }

    Write-Decision "allow" "" ""
    exit 0
}
catch {
    Write-Decision "deny" "셸 게이트가 실패하여 명령을 막았습니다." "gate-shell.ps1 이 실패했다. 명령을 실행하지 마라."
    exit 2
}
