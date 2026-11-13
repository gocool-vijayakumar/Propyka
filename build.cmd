@echo off
rem Propyka build: runs build.ps1 with the arguments you pass in.
rem Example: build.cmd -SkipTests
setlocal
powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1" %*
exit /b %ERRORLEVEL%
