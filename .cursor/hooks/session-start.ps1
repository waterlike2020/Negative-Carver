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
    $lines = @()
    if (Test-Path -LiteralPath "to-do-list.md") {
        $path = (Resolve-Path -LiteralPath "to-do-list.md").Path
        $lines = [System.IO.File]::ReadAllLines($path, [System.Text.Encoding]::UTF8)
    }

    $stage = ($lines | Where-Object { $_ -like "현재 단계:*" } | Select-Object -First 1)
    $total = ($lines | Where-Object { $_ -like "전체:*" } | Select-Object -First 1)
    $open = @($lines | Where-Object { $_ -match '^- \[ \]' } | Select-Object -First 3)

    $text = @(
        "Negative Carver harness. Read AGENTS.md and follow .cursor/skills/negative-carver-harness.",
        $(if ($stage) { $stage.Trim() } else { "현재 단계: unknown" }),
        $(if ($total) { $total.Trim() } else { "전체: unknown" }),
        "Next open items:",
        $(if ($open.Count -gt 0) { ($open -join "`n") } else { "(none)" })
    ) -join "`n"

    Write-HookJson ('{"additional_context":"' + (Escape-JsonText $text) + '"}')
    exit 0
}
catch {
    Write-HookJson '{"additional_context":"Negative Carver harness. Read AGENTS.md. sessionStart hook could not read to-do-list.md."}'
    exit 0
}
