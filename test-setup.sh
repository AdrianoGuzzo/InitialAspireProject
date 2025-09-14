#!/bin/bash

# Script de teste para verificar a configuração do IdentityServer

echo "=== Teste do IdentityServer com PostgreSQL ==="
echo ""

# URLs base
API_BASE="https://localhost:7001"
WEB_BASE="https://localhost:7002"

echo "1. Testando se a API está respondendo..."
curl -k -s -o /dev/null -w "%{http_code}" "$API_BASE/health" || echo "API não está respondendo"

echo ""
echo "2. Testando endpoint de descoberta do OpenIddict..."
curl -k -s "$API_BASE/.well-known/openid_configuration" | jq . || echo "Endpoint de descoberta não encontrado"

echo ""
echo "3. Testando acesso à API protegida (deve retornar 401)..."
HTTP_CODE=$(curl -k -s -o /dev/null -w "%{http_code}" "$API_BASE/api/weather")
if [ "$HTTP_CODE" = "401" ]; then
    echo "✅ API protegida corretamente (retornou 401)"
else
    echo "❌ API não está protegida (retornou $HTTP_CODE)"
fi

echo ""
echo "4. Testando frontend web..."
curl -k -s -o /dev/null -w "%{http_code}" "$WEB_BASE" || echo "Frontend não está respondendo"

echo ""
echo "=== Instruções para teste manual ==="
echo "1. Acesse: $WEB_BASE"
echo "2. Clique em 'Login' ou acesse: $WEB_BASE/login"
echo "3. Use as credenciais:"
echo "   Email: test@example.com"
echo "   Senha: Test123!"
echo "4. Após o login, teste o acesso aos dados do weather"
echo ""
echo "=== URLs úteis ==="
echo "- Dashboard Aspire: https://localhost:15888"
echo "- API: $API_BASE"
echo "- Frontend: $WEB_BASE"
echo "- pgAdmin: https://localhost:8080"
echo ""