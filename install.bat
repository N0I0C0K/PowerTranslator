@echo off

%1 mshta vbscript:CreateObject("Shell.Application").ShellExecute("cmd.exe","/c %~s0 ::","","runas",1)(window.close)&&exit

set "source=%cd%\Translator"
set "destination=%LOCALAPPDATA%\Microsoft\PowerToys\PowerToys Run\Plugins\"

echo Moving directory...

taskkill /f /im PowerToys.exe

move "%source%" "%destination%"

echo Move completed.

run "C:\Program Files\PowerToys\PowerToys.exe"

pause