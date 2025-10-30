param(
    [Parameter(Mandatory=$true)] [string]$Server,
    [Parameter(Mandatory=$true)] [string]$Database,
    [string]$ScriptsPath = "$(Split-Path -Path $MyInvocation.MyCommand.Path -Parent)\migrations",
    [switch]$Trusted,
    [string]$Username,
    [string]$Password,
    [switch]$WhatIf
)

Write-Host "Applying SQL migrations from: $ScriptsPath" -ForegroundColor Cyan

if (!(Test-Path $ScriptsPath)) {
    throw "Scripts path not found: $ScriptsPath"
}

$authArgs = @()
if ($Trusted) {
    $authArgs += "-E"
} else {
    if (-not $Username -or -not $Password) {
        throw "Provide -Trusted or -Username and -Password."
    }
    $authArgs += @('-U', $Username, '-P', $Password)
}

$files = Get-ChildItem -Path $ScriptsPath -Filter *.sql | Sort-Object Name
if ($files.Count -eq 0) {
    Write-Host "No .sql files found." -ForegroundColor Yellow
    exit 0
}

foreach ($f in $files) {
    Write-Host ("Running: {0}" -f $f.Name) -ForegroundColor Green
    $args = @('-S', $Server, '-d', $Database, '-i', $f.FullName, '-b') + $authArgs
    if ($WhatIf) {
        Write-Host ("sqlcmd " + ($args -join ' '))
    } else {
        & sqlcmd @args
        if ($LASTEXITCODE -ne 0) {
            throw "sqlcmd failed on $($f.Name) with exit code $LASTEXITCODE"
        }
    }
}

Write-Host "All migrations applied successfully." -ForegroundColor Cyan

