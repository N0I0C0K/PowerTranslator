# Getting Start

## 结构

程序的入口是官方提供的 IPlugin 接口，通过 query 函数触发。下面讲解各个类的主要作用

### Translater

插件的各个基础功能，插件的属性，实现官方的各种基础接口（比如 设置、查询）

### Utils

工具空间，通用功能

### TranslateHelper

翻译功能的适配器，向上层（Translter）提供可直接调用的翻译接口函数，向下负责调度各个翻译 API 的实现，确保翻译的稳定和快速

### SuggestHelper

建议功能的适配器，目前只有一个 suggest 接口故同时充当了建议功能的实现

### HistoryHelper

历史功能的适配器

### ITranslter

具体的翻译实现（通常是网络接口调用）

## 如何添加一个翻译实现

假设我们要添加 Google 翻译

1. 新建一个文件夹 Google
2. 新建 GoogleTransltor.cs
3. 新建 class GoogleTransltor 继承自 ITranslater，实现接口。
4. 在 TranslateHelper 中的构造函数里的 translatorTypes 中添加 GoogleTransltor。
5. 测试是否成功调用

就这样你就添加了一个新的翻译接口了！