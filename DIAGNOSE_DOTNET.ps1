# .NET SDK Diagnostic Script for Windows

Write-Host "--- .NET SDK Diagnostic ---" -ForegroundColor Cyan

# 1. Check dotnet version and info
Write-Host "`n1. Running 'dotnet --info'..." -ForegroundColor Yellow
dotnet --info

# 2. Check installed SDKs
Write-Host "`n2. Running 'dotnet --list-sdks'..." -ForegroundColor Yellow
dotnet --list-sdks

# 3. Check PATH environment variable
Write-Host "`n3. Checking PATH environment variable for dotnet..." -ForegroundColor Yellow
$env:Path -split ';' | Where-Object { $_ -like "*dotnet*" }

# 4. Check where dotnet is located
Write-Host "`n4. Checking 'where dotnet'..." -ForegroundColor Yellow
where.exe dotnet

# 5. Check Program Files directories
Write-Host "`n5. Checking Program Files for dotnet SDKs..." -ForegroundColor Yellow
$pf64 = "C:\Program Files\dotnet\sdk"
$pf86 = "C:\Program Files (x86)\dotnet\sdk"

if (Test-Path $pf64) {
    Write-Host "Found 64-bit SDKs in: $pf64"
    Get-ChildItem $pf64 | Select-Object Name
} else {
    Write-Host "64-bit SDK directory not found: $pf64" -ForegroundColor Red
}

if (Test-Path $pf86) {
    Write-Host "`nFound 32-bit SDKs in: $pf86"
    Get-ChildItem $pf86 | Select-Object Name
} else {
    Write-Host "`n32-bit SDK directory not found: $pf86"
}

Write-Host "`n--- End of Diagnostic ---" -ForegroundColor Cyan
Write-Host "`nPlease copy the output above and share it with me." -ForegroundColor Green
