@echo off
setlocal

rem Define source and target folders
set "source_folder=Translator"
set "target_folder=%LOCALAPPDATA%\Microsoft\PowerToys\PowerToys Run\Plugins\Translator"

rem Check if the target folder exists, and delete it if it does
if exist "%target_folder%" (
    rmdir /s /q "%target_folder%"
)

rem Copy the source folder to the target folder
xcopy "%source_folder%" "%target_folder%" /e /i

echo Copy completed!
endlocal
