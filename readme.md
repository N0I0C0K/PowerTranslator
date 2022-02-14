# Translater
## about
一个用于[PowerToysRun](https://github.com/microsoft/PowerToys)的插件.使用百度翻译api(后期会改)
- 目前只支持**zh->en**

## Usage
1. 找到`"{User}\AppData\Local\Microsoft\PowerToys\PowerToys Run\settings.json"`
2. 在`plugins`字段下添加
```json
{
    "Id": "HBB9510CD0D2481F853690A07E6DC426",
    "Name": "Translater",
    "Description": "A simple translate plugin",
    "Author": "N0I0C0K",
    "Disabled": false,
    "IsGlobal": false,
    "ActionKeyword": "^",
    "IconPathDark": "Translater\\Images\\shell.dark.png",
    "IconPathLight": "Translater\\Images\\shell.light.png",
    "AdditionalOptions": []
}
```
3. 下载Translater.zip
4. 解压到`{安装目录}\modules\launcher\Plugins\`
