# PowerTrans

[中文](./readme.md)

## about

A plugin for [PowerToysRun](https://github.com/microsoft/PowerToys).

Based on Youdao Translation, it supports the mutual translation of multiple languages

## Install

1. Close PowerToys
2. Download [Translator.zip](https://github.com/N0I0C0K/PowerTrans/releases)
3. Extract to`{Installation directory}\modules\launcher\Plugins\`
   As shown in the picture
   ![file](Images/file.png)
4. Open PowerToys

## Usage

- The default trigger key is`^`.(The current default trigger key conflicts with the Everything plugin. You can change the trigger in the following way.)
- Designated translation text

  - `alt+space`open PowerToys Run, input `^[what you want to translate]`
  - `Enter` Copy the translation to the clipboard

  Demonstration translation command
  ![en->zh](Images/command.gif)

- Quick translation of clipboard

  - When there is text in the clipboard, type the trigger keyword `^` directly to quickly translate the contents of the clipboard

    ![clipboard](Images/clipboard.gif)

- Specify the translation target language
  - Use `^[words]->[Target language]`, for example: `^你好->ja` To translate 你好 into Japanese
    ![Specified language](Images/target%20lan.gif)

Common language code
|language|code|comment|
|---------|------|-|
|汉语(简体) | zh-CHS | 汉语简体
|汉语(繁体) | zh-CHT| 漢語翻譯
|日语 |ja| 日本語
|英语 |en| English
|韩语 |ko| 한국어
|法语 |fr| En français
|俄语 |ru| русск

## Setting

- Change the default trigger key (if you need to change it, it is recommended to change it to " ` ", and check whether there is any conflict)
  ![change active key](Images/change_active.png)
- It is recommended to check `clear the previous query on startup`, which can solve the problem that clipboard quick translation cannot be triggered
  ![auto clean](Images/auto_clean.png)

## Issue

If you find an issue that cannot be translated or an unknown error occurs, and you need a new function, please submit the issue.
