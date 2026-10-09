@echo off
title TicketDesk launcher
echo Starting TicketDesk API (http://localhost:5080) and Angular UI (http://localhost:4200) ...
echo First run downloads packages, so give it a couple of minutes.
start "TicketDesk API" cmd /k "cd /d %~dp0backend\TicketDesk.Api && dotnet run"
start "TicketDesk UI" cmd /k "cd /d %~dp0frontend && npm install && npm start"
timeout /t 25 >nul
start http://localhost:4200
