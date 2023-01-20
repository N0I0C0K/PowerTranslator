# Translater

## about

一个用于[PowerToysRun](https://github.com/microsoft/PowerToys)的插件.

现已更新到有道翻译, **支持有道翻译的全部语言翻译**

## Install

1. 找到`"{User}\AppData\Local\Microsoft\PowerToys\PowerToys Run\settings.json"`
2. 在`plugins`字段下添加

```json
{
  "Id": "HBB9510CD0D2481F853690A07E6DC426",
  "Name": "Translater",
  "Description": "a simple translate plugin",
  "Author": "N0I0C0K",
  "Disabled": false,
  "IsGlobal": false,
  "ActionKeyword": "^",
  "WeightBoost": 0,
  "IconPathDark": "Translater\\Images\\translater.dark.png",
  "IconPathLight": "Translater\\Images\\translater.light.png",
  "AdditionalOptions": []
}
```

3. 下载 Translater.zip
4. 解压到`{安装目录}\modules\launcher\Plugins\`

## Usage

- `alt+space`打开 PowerToysRun, 输入`^[你要翻译的地方]`

演示翻译 command
![en->zh](Images/command.gif)

## Issue

如果发现了无法翻译, 出现了未知错误, 欢迎提交 issue. 我会及时修复
