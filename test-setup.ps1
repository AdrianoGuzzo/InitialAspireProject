# Script de teste para verificar a configuração do IdentityServer

Write-Host "=== Teste do IdentityServer com PostgreSQL ===" -ForegroundColor Green
Write-Host ""

# URLs base
$API_BASE = "https://localhost:7001"
$WEB_BASE = "https://localhost:7002"

Write-Host "1. Testando se a API está respondendo..." -ForegroundColor Yellow
try {
    $response = Invoke-WebRequest -Uri "$API_BASE/health" -SkipCertificateCheck -UseBasicParsing
    Write-Host "✅ API respondendo (Status: $($response.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "❌ API não está respondendo: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "2. Testando endpoint de descoberta do OpenIddict..." -ForegroundColor Yellow
try {
    $discovery = Invoke-WebRequest -Uri "$API_BASE/.well-known/openid_configuration" -SkipCertificateCheck -UseBasicParsing
    Write-Host "✅ Endpoint de descoberta encontrado" -ForegroundColor Green
} catch {
    Write-Host "❌ Endpoint de descoberta não encontrado: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "3. Testando acesso à API protegida (deve retornar 401)..." -ForegroundColor Yellow
try {
    $weatherResponse = Invoke-WebRequest -Uri "$API_BASE/api/weather" -SkipCertificateCheck -UseBasicParsing
    Write-Host "❌ API não está protegida (retornou $($weatherResponse.StatusCode))" -ForegroundColor Red
} catch {
    if ($_.Exception.Response.StatusCode -eq 401) {
        Write-Host "✅ API protegida corretamente (retornou 401)" -ForegroundColor Green
    } else {
        Write-Host "❌ Erro inesperado: $($_.Exception.Message)" -ForegroundColor Red
    }
}

Write-Host ""
Write-Host "4. Testando frontend web..." -ForegroundColor Yellow
try {
    $webResponse = Invoke-WebRequest -Uri $WEB_BASE -SkipCertificateCheck -UseBasicParsing
    Write-Host "✅ Frontend respondendo (Status: $($webResponse.StatusCode))" -ForegroundColor Green
} catch {
    Write-Host "❌ Frontend não está respondendo: $($_.Exception.Message)" -ForegroundColor Red
}

Write-Host ""
Write-Host "=== Instruções para teste manual ===" -ForegroundColor Cyan
Write-Host "1. Acesse: $WEB_BASE"
Write-Host "2. Clique em 'Login' ou acesse: $WEB_BASE/login"
Write-Host "3. Use as credenciais:"
Write-Host "   Email: test@example.com"
Write-Host "   Senha: Test123!"
Write-Host "4. Após o login, teste o acesso aos dados do weather"
Write-Host ""
Write-Host "=== URLs úteis ===" -ForegroundColor Cyan
Write-Host "- Dashboard Aspire: https://localhost:15888"
Write-Host "- API: $API_BASE"
Write-Host "- Frontend: $WEB_BASE"
Write-Host "- pgAdmin: https://localhost:8080"
Write-Host ""