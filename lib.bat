mkdir -p -e lib
copy "C:\Program Files\PowerToys\PowerToys.Common.UI.dll" "lib\PowerToys.Common.UI.dll" /y
copy "C:\Program Files\PowerToys\PowerToys.ManagedCommon.dll" "lib\PowerToys.ManagedCommon.dll" /y
copy "C:\Program Files\PowerToys\PowerToys.Settings.UI.Lib.dll" "lib\PowerToys.Settings.UI.Lib.dll" /y
copy "C:\Program Files\PowerToys\Wox.Infrastructure.dll" "lib\Wox.Infrastructure.dll" /y
copy "C:\Program Files\PowerToys\Wox.Plugin.dll" "lib\Wox.Plugin.dll" /y
copy "%LOCALAPPDATA%\PowerToys.Common.UI.dll" "lib\PowerToys.Common.UI.dll" /y
copy "%LOCALAPPDATA%\PowerToys.ManagedCommon.dll" "lib\PowerToys.ManagedCommon.dll" /y
copy "%LOCALAPPDATA%\PowerToys.Settings.UI.Lib.dll" "lib\PowerToys.Settings.UI.Lib.dll" /y
copy "%LOCALAPPDATA%\Wox.Infrastructure.dll" "lib\Wox.Infrastructure.dll" /y
copy "%LOCALAPPDATA%\Wox.Plugin.dll" "lib\Wox.Plugin.dll" /y