@echo off

%1 mshta vbscript:CreateObject("Shell.Application").ShellExecute("cmd.exe","/c %~s0 ::","","runas",1)(window.close)&&exit

tasklist | find /i "PowerToys.exe" >nul
if %errorlevel% equ 0 goto killpowertoys
goto upgrade

:killpowertoys
echo 检测到PowerToys正在运行
set /p input=是否要关闭 PowerToys.exe?(y/[n])
if /i %input%==y (
    taskkill /f /im PowerToys.exe
    echo 进程已关闭。
) else (
    echo 请关闭PowerToys后再进行升级
    echo 任务结束
    pause
    exit /b
)
goto upgrade

:upgrade

:upgrade
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
