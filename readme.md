<div align="center">
<h1>PowerTranslator</h1>
<p>一个用于<a href=https://github.com/microsoft/PowerToys>PowerToys Run</a>的翻译插件, 快速, 稳定.</p>

![GitHub release (latest by date)](https://img.shields.io/github/v/release/N0I0C0K/PowerTranslator?style=flat-square) ![GitHub Repo stars](https://img.shields.io/github/stars/N0I0C0K/PowerTranslator?color=ffb900&style=flat-square) ![GitHub all releases](https://img.shields.io/github/downloads/N0I0C0K/PowerTranslator/total?style=flat-square) ![GitHub](https://img.shields.io/github/license/N0I0C0K/PowerTranslator?style=flat-square)

[English](./readme_en.md)

</div>

## About

一个用于[PowerToysRun](https://github.com/microsoft/PowerToys)的插件.

现已更新到有道翻译, **支持有道翻译的全部语言翻译**.

- [如何使用](#usage)
- [安装](#install)
- [设置](#setting)
- [提交问题](#issue)
- [贡献]

## Usage

- 默认触发键为`|`.（下方GIF演示采用的自定义触发键`^`）
- 指定翻译文本

  - `alt+space`打开 PowerToysRun, 输入`^[你要翻译的地方]`
  - `Enter`复制翻译结果到剪贴板

  演示翻译 `command`
  ![command](Images/command.gif)

  演示翻译 `命令`
  ![chinese](Images/%E5%91%BD%E4%BB%A4.gif)

- 中文带有拼音
  参考上方演示翻译`命令`

- 快速翻译剪贴板

  - 当剪贴板内有文字, 直接键入触发关键字`^`, 即可快速翻译剪贴板内的内容

    ![clipboard](Images/clipboard.gif)

- 指定翻译目标语言

  - 使用 `^[words]->[Target language]`, 例如: `^你好->ja` 表示把你好翻译为日文
    ![Specified language](Images/target%20lan.gif)
    常用语言代码
    |语言|代码|备注|
    |---------|------|-|
    |汉语(简体) | zh-CHS | 汉语简体
    |汉语(繁体) | zh-CHT| 漢語翻譯
    |日语 |ja| 日本語
    |英语 |en| English
    |韩语 |ko| 한국어
    |法语 |fr| En français
    |俄语 |ru| русск

- 搜索建议

  - 根据当前的输入建议搜索内容
    ![suggest](Images/suggest.gif)
  - 可以在设置里选择是否启用建议, 对应`Enable search suggest`

- 朗读内容

  - 支持朗读中文, 英文(快捷键 ctrl+enter)
    ![read](Images/read.png)
  - 基于有道 TTS 接口
  - 支持自动朗读结果功能, **默认关闭**, 可以在设置开启.对应`Automatic reading result`

- 历史记录
  - 键入`h`或者剪贴板内无翻译目标会显示历史翻译记录.
    ![his](Images/his.png)
  - 默认 **15** 条记录
  - **为什么不支持自定义上限条目?**
    因为官方只开放了 bool 类型的自定义参数, 所以目前不支持自定义历史记录上限. 等待后续支持我会更新.或者可以下载源码自行修改编译.

- 第二语言
  - 开启第二翻译目标语言，默认关闭
  ![second option](Images/second_option.png)
  - 开启后会在每次翻译的时候自动展示第二语言结果
  ![second](Images/second.png)


## Install

1. 关闭 PowerToys
2. 下载 [Translator.zip](https://github.com/N0I0C0K/PowerTranslator/releases)
3. 解压到`%LOCALAPPDATA%\Microsoft\PowerToys\PowerToys Run\Plugins`
   如图所示
   ![file](Images/file.png)
4. 启动 PowerToys

(参考官方[第三方插件安装文档](https://github.com/microsoft/PowerToys/blob/main/doc/thirdPartyRunPlugins.md))

[安装-升级 详细教程](./doc/how%20to%20install.md)

## Setting

- 如果触发按键冲突, 修改默认触发键 (建议修改为" \` ", 同时建议检查一下有无冲突)。**注意在v0.8之后默认触发按键是`"|"`，无需再次修改**
  ![change active key](Images/change_active.png)
- 建议勾选`输入平滑`, 可以优化输入体验
  ![enable Smooth input](Images/enable%20Smooth%20input.png)
- `启动时清除上一查询`可以快速触发翻译剪切板, **按需求勾选.**
  ![auto clean](Images/auto_clean.png)
- 切换默认目标翻译语言，默认是`auto`
  ![languages](Images/languages.png)

## Issue

如果发现了无法翻译, 出现了未知错误, 需要新的功能, 欢迎提交 issue. 我会及时修复或改进

## Contribution

### 环境

- .net sdk

### 准备

1. fork 这个仓库
2. clone fork 后的仓库到本地
3. 执行 lib.bat
4. 使用 vscode（任意） 打开
5. 开始编码吧！

[帮助文档 - Getting Start!](doc/code-start-zh.md)