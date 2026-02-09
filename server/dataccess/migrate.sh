#!/usr/bin/env bash
set -e

dotnet ef migrations add InitialCreate \
  --project ./dataccess.csproj \
  --startup-project ../api/api.csproj \
  --context ChatDbContext \
  --output-dir Migrations

dotnet ef database update \
  --project ./dataccess.csproj \
  --startup-project ../api/api.csproj \
  --context ChatDbContext

