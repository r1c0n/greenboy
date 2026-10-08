@echo off
dotnet publish "%~dp0GreenBoy.Windows\GreenBoy.Windows.csproj" -c Release -r win-x64 --self-contained true
