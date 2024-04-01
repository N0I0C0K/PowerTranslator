@echo off
set "source=%cd%\Translator"
set "destination=%LOCALAPPDATA%\Microsoft\PowerToys\PowerToys Run\Plugins\"

echo Moving directory...
move "%source%" "%destination%"

echo Move completed.
pause