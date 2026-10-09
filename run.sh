#!/usr/bin/env bash
# Starts the API and the Angular UI. Needs: .NET SDK, Node.js, and a SQL Server (see docker-compose.yml).
set -e
ROOT="$(cd "$(dirname "$0")" && pwd)"
(cd "$ROOT/backend/TicketDesk.Api" && dotnet run) &
API_PID=$!
trap "kill $API_PID 2>/dev/null" EXIT
cd "$ROOT/frontend"
npm install
npm start
