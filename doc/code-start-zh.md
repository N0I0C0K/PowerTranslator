# Getting Start

## 结构

程序的入口是官方提供的 IPlugin 接口，通过 query 函数触发。下面讲解各个类的主要作用

### Translator

插件的各个基础功能，插件的属性，实现官方的各种基础接口（比如 设置、查询）

### Utils

工具空间，通用功能

### TranslateHelper

翻译功能的适配器，向上层（Transltor）提供可直接调用的翻译接口函数，向下负责调度各个翻译 API 的实现，确保翻译的稳定和快速

### SuggestHelper

建议功能的适配器，目前只有一个 suggest 接口故同时集成了建议功能的实现

### HistoryHelper

历史功能的适配器

### ITransltor

具体的翻译实现（通常是网络接口调用）

目前已经实现的翻译接口有：
- 有道翻译网页版本
- 有道翻译 api 试用版本（用于 backup）
- 有道翻译老版本网页

计划中的翻译接口实现：
- 微软翻译
- 谷歌翻译

### SettingHelper

提供设置接口，主要是在 Power toys 界面中显示的设置

## 如何添加一个翻译实现

假设我们要添加 Google 翻译

1. 新建一个文件夹 Google
2. 新建 GoogleTransltor.cs
3. 新建 class GoogleTransltor 继承自 ITranslater，实现接口。
4. 在 TranslateHelper 中的构造函数里的 translatorTypes 中添加 GoogleTransltor。
5. 测试是否成功调用

就这样你就添加了一个新的翻译接口了！


## 测试 and 打包

测试的一般步骤是：
1. 编写好代码后，使用 build 命令生成插件 dll
2. 放到插件目录下后重新运行 Power toys run

### build

运行 `dotnet build -p:Platform={x64|ARM64}` 会在 `/bin/output` 下生成完整的插件文件，可以直接拖到插件目录下完成安装

### 测试用例

目前还没有测试用例的编写，考虑在后期加上

### pack

自己手动测试没有问题后，运行 `dotnet pack` 打包命令，会生成 `/bin/Translator_{x64|ARM64}.zip` 就是完整的插件文件了

pack 和 build 命令可以通过修改 [Translater.csproj](../Translater.csproj) 文件修改