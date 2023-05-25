# 安装

1. 关闭 PowerToys
2. 下载 [Translator.zip](https://github.com/N0I0C0K/PowerTranslator/releases)
3. 解压到`{安装目录}\modules\launcher\Plugins\`.
   如图所示
   ![file](../Images/file.png)
   
   (如果出现了无法解压的问题, [可以使用命令行安装](#使用命令行安装))
4. 启动 PowerToys

## 使用命令行安装

在一些情况下（通常是因为需要管理员权限）可能出现无法解压的情况, 可以使用命令行安装, 确保你已经将`Translator.zip`放入 Plugins 目录下.

1. `win+s`搜索 PowerShell, 以管理员身份运行.
2. 执行`cd "{安装目录}\modules\launcher\Plugins\"`
3. 执行`Expand-Archive -Path ./Translator.zip -DestinationPath ./Translator -Force`

## 如何升级

1. 关闭 PowerToys
2. 下载新版本`Translator.zip`.
3. 解压覆盖掉原先的 Translator 文件夹即可.(同样如果出现无法解压的问题[可以使用命令行安装](#使用命令行安装))
4. 启动 PowerToys
