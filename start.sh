#!/bin/bash
set -ex
echo "=== DIAGNOSTICS ==="
echo "PORT=$PORT"
echo "ASPNETCORE_ENVIRONMENT=$ASPNETCORE_ENVIRONMENT"
echo "DOTNET_ROOT=$DOTNET_ROOT"
which dotnet
dotnet --list-runtimes 2>&1 | head -5
ls -la /app/out/Wesal.API.dll
echo "=== END DIAGNOSTICS ==="
exec dotnet /app/out/Wesal.API.dll