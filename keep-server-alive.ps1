# ========================================
# Keep Render Server Alive Script
# Chạy lặp lại request để keep server running
# ========================================

param(
    [string]$BackendUrl = "https://tuvi1.onrender.com",
    [string]$FrontendUrl = "https://tuvi1.onrender.com",
    [int]$IntervalMinutes = 10
)

Write-Host "🔄 Keep Render Server Alive Script" -ForegroundColor Cyan
Write-Host "=================================" -ForegroundColor Cyan
Write-Host "Backend URL: $BackendUrl" -ForegroundColor Yellow
Write-Host "Frontend URL: $FrontendUrl" -ForegroundColor Yellow
Write-Host "Interval: $IntervalMinutes minutes" -ForegroundColor Yellow
Write-Host ""
Write-Host "⏱️  Script sẽ chạy liên tục. Nhấn Ctrl+C để dừng" -ForegroundColor Green
Write-Host ""

$counter = 0

while ($true) {
    $counter++
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    
    Write-Host "[$timestamp] Request #$counter" -ForegroundColor Cyan
    
    # Ping Backend
    try {
        $response = Invoke-WebRequest -Uri "$BackendUrl/api/tuvi/health" `
                                     -TimeoutSec 10 `
                                     -ErrorAction Stop
        Write-Host "✅ Backend: $($response.StatusCode) OK" -ForegroundColor Green
    }
    catch {
        Write-Host "⚠️  Backend: Error - $($_.Exception.Message)" -ForegroundColor Yellow
    }
    
    # Ping Frontend
    try {
        $response = Invoke-WebRequest -Uri $FrontendUrl `
                                     -TimeoutSec 10 `
                                     -ErrorAction Stop
        Write-Host "✅ Frontend: $($response.StatusCode) OK" -ForegroundColor Green
    }
    catch {
        Write-Host "⚠️  Frontend: Error - $($_.Exception.Message)" -ForegroundColor Yellow
    }
    
    Write-Host ""
    
    # Chờ trước request tiếp theo
    $sleepSeconds = $IntervalMinutes * 60
    Write-Host "💤 Chờ $IntervalMinutes phút trước request tiếp theo..." -ForegroundColor DarkGray
    Start-Sleep -Seconds $sleepSeconds
}
