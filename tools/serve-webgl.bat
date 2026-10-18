@echo off
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0serve-webgl.ps1" %*
pause
