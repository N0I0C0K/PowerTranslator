# Changelog

All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [Unreleased]

### Added
- 支持了语言代码简写. 现在你可以使用 `zhs` 用来指代 `zh-CHS`
- 添加了 DeepL 翻译(因为对频率的限制较强，未将其设为默认翻译引擎)
- 支持翻译代码变量（在snake_case或camelCase）。例如，`hello_world` 将被当作 `hello world`

### Fixed
- 修复“是否使用代理设置”在软件启动时未正确设置

## [0.11]

### Added
- 新增打开词典快捷方式，自带 有道、牛津、剑桥 词典快捷方式
- 新增若干可选语言

## [0.10.1] - 2024-01-20

### Fixed
- 修复了复制时默认带上音标的错误
- 内地接口之后不再使用系统默认代理

## [0.10.0] - 2024-01-14

### Added
- 新增第二语言选项
- 为单个单词添加音标显示

## [0.9.0] - 2023-10-01

### Added
- 新增了默认目标语言选项

## [0.8.0] - 2023-09-21

### Added
- 改进了信息的展示
- 改进了初始化过程

### Fixed
- 修复了接口失效问题
- 修复了一个接口出现问题后的接口切换错误

## [0.7.1] - 2023-05-13

### Fixed
- 修复了历史记录功能会错误地将空查询记录
- 优化了初始化速度，此前的版本初始化速度为 **6020ms**，优化后为 **3ms**（其实总的初始化时间是不会改变的，只是将比较耗时的部分移动到了后台执行）

## [0.7.0] - 2023-04-10

### Added
- 新增历史记录功能，键入 `h` 查看历史翻译

### Fixed
- 修复了翻译被特殊字符截断的问题
- 优化了初始化速度

## [0.6.0] - 2023-03-09

### Added
- 新增朗读功能，支持中英文朗读，快捷键 `Ctrl+Enter`
- 新增自动朗读结果（默认关闭）

### Changed
- 项目名称更名为 `PowerTranslator`

## [0.5.1] - 2023-02-15

### Added
- 新增了备用接口，增加了稳定性

## [0.5.0] - 2023-02-14

### Added
- 新增了翻译 API，整体迁移到新 API
- 新增了出现错误寻求帮助的快捷方式

### Fixed
- 修改了初始化逻辑

## [0.4.0]

### Added
- 新增了搜索建议

### Fixed
- 修复了之前搜索的卡顿问题
- 修复了无法在中间输入的问题
