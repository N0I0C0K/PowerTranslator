@echo off

%1 mshta vbscript:CreateObject("Shell.Application").ShellExecute("cmd.exe","/c %~s0 ::","","runas",1)(window.close)&&exit

set "source=%cd%\Translator"
set "destination=%LOCALAPPDATA%\Microsoft\PowerToys\PowerToys Run\Plugins\"

echo Moving directory...

taskkill /f /im PowerToys.exe

move "%source%" "%destination%"

echo Move completed.

@REM 不要试图假设用户的安装位置
echo 请重启PowerToys以查看更改。
echo Please restart PowerToys.

pause